import assert from "node:assert/strict";
import test from "node:test";
import {
    createExtractionRequestStore,
    EXTRACTION_REQUEST_MAX_AGE_MS,
    getExtractionRequestStorageKey
} from "../sidepanel/extractionRequestStore.js";

function createSessionStorage(initialValues = {}) {
    const values = new Map(Object.entries(initialValues));
    const calls = {
        get: [],
        set: [],
        remove: []
    };

    return {
        storageSession: {
            async get(key) {
                calls.get.push(key);

                if (key === null) {
                    return Object.fromEntries(values);
                }

                return values.has(key) ? { [key]: values.get(key) } : {};
            },
            async set(items) {
                calls.set.push(items);

                for (const [key, value] of Object.entries(items)) {
                    values.set(key, value);
                }
            },
            async remove(key) {
                calls.remove.push(key);

                for (const item of Array.isArray(key) ? key : [key]) {
                    values.delete(item);
                }
            }
        },
        calls,
        values
    };
}

test("publishes only extraction metadata under a request-specific key", async () => {
    const session = createSessionStorage();
    const store = createExtractionRequestStore({
        storageSession: session.storageSession,
        now: () => 1_000
    });

    const request = await store.publishExtractionRequest({
        requestId: "request-1",
        tabId: 101,
        windowId: 201
    });

    const key = getExtractionRequestStorageKey(201, "request-1");
    const expectedRequest = {
        requestId: "request-1",
        tabId: 101,
        windowId: 201,
        timestamp: 1_000
    };

    assert.deepEqual(request, expectedRequest);
    assert.deepEqual(session.calls.set, [{ [key]: expectedRequest }]);
    assert.deepEqual(await store.readExtractionRequest(201), expectedRequest);
    assert.equal(session.values.has(key), true);
});

test("isolates requests by window and consumes each request only once", async () => {
    const session = createSessionStorage();
    const store = createExtractionRequestStore({
        storageSession: session.storageSession,
        now: () => 2_000
    });

    const firstRequest = await store.publishExtractionRequest({
        requestId: "request-1",
        tabId: 101,
        windowId: 201
    });
    const secondRequest = await store.publishExtractionRequest({
        requestId: "request-2",
        tabId: 102,
        windowId: 202
    });

    const [firstConsumption, duplicateConsumption] = await Promise.all([
        store.consumeExtractionRequest(201),
        store.consumeExtractionRequest(201)
    ]);

    assert.deepEqual(firstConsumption, firstRequest);
    assert.equal(duplicateConsumption, null);
    assert.equal(
        session.values.has(getExtractionRequestStorageKey(201, "request-1")),
        false
    );
    assert.deepEqual(await store.consumeExtractionRequest(202), secondRequest);
});

test("removes the mailbox entry before returning a consumed request", async () => {
    const events = [];
    const key = getExtractionRequestStorageKey(201, "request-1");
    const request = {
        requestId: "request-1",
        tabId: 101,
        windowId: 201,
        timestamp: 3_000
    };
    const storageSession = {
        async get() {
            events.push("get");
            return { [key]: request };
        },
        async set() {},
        async remove() {
            events.push("remove");
        }
    };
    const store = createExtractionRequestStore({
        storageSession,
        now: () => 3_000
    });

    const consumption = store.consumeExtractionRequest(201).then(value => {
        events.push("resolved");
        return value;
    });

    assert.deepEqual(await consumption, request);
    assert.deepEqual(events, ["get", "remove", "resolved"]);
});

test("discards and removes expired or structurally invalid requests", async t => {
    const currentTimestamp = 100_000;
    const expiredRequest = {
        requestId: "expired",
        tabId: 101,
        windowId: 201,
        timestamp: currentTimestamp - EXTRACTION_REQUEST_MAX_AGE_MS - 1
    };
    const invalidRequest = {
        requestId: "invalid",
        tabId: 101,
        windowId: 201,
        timestamp: currentTimestamp,
        recipe: { name: "Must not be stored" }
    };

    for (const [name, request] of [
        ["expired request", expiredRequest],
        ["invalid request", invalidRequest]
    ]) {
        await t.test(name, async () => {
            const key = getExtractionRequestStorageKey(201, request.requestId);
            const session = createSessionStorage({ [key]: request });
            const store = createExtractionRequestStore({
                storageSession: session.storageSession,
                now: () => currentTimestamp
            });

            assert.equal(await store.consumeExtractionRequest(201), null);
            assert.equal(session.values.has(key), false);
        });
    }
});

test("deduplicates a requestId that is published again", async () => {
    const session = createSessionStorage();
    const store = createExtractionRequestStore({
        storageSession: session.storageSession,
        now: () => 4_000
    });
    const request = {
        requestId: "request-1",
        tabId: 101,
        windowId: 201
    };

    await store.publishExtractionRequest(request);
    assert.notEqual(await store.consumeExtractionRequest(201), null);

    await store.publishExtractionRequest(request);
    assert.equal(await store.consumeExtractionRequest(201), null);
    assert.equal(
        session.values.has(getExtractionRequestStorageKey(201, "request-1")),
        false
    );
});

test("does not delete a newer request published between reading and removing", async () => {
    const oldRequest = {
        requestId: "request-1",
        tabId: 101,
        windowId: 201,
        timestamp: 4_000
    };
    const newRequest = {
        requestId: "request-2",
        tabId: 102,
        windowId: 201,
        timestamp: 4_001
    };
    const oldKey = getExtractionRequestStorageKey(201, oldRequest.requestId);
    const newKey = getExtractionRequestStorageKey(201, newRequest.requestId);
    const values = new Map([[oldKey, oldRequest]]);
    let hasPublishedNewRequest = false;
    const storageSession = {
        async get() {
            const snapshot = Object.fromEntries(values);

            if (!hasPublishedNewRequest) {
                hasPublishedNewRequest = true;
                values.set(newKey, newRequest);
            }

            return snapshot;
        },
        async set(items) {
            for (const [key, value] of Object.entries(items)) {
                values.set(key, value);
            }
        },
        async remove(keys) {
            for (const key of Array.isArray(keys) ? keys : [keys]) {
                values.delete(key);
            }
        }
    };
    const store = createExtractionRequestStore({
        storageSession,
        now: () => 4_001
    });

    assert.deepEqual(await store.consumeExtractionRequest(201), oldRequest);
    assert.equal(values.has(oldKey), false);
    assert.equal(values.has(newKey), true);
    assert.deepEqual(await store.consumeExtractionRequest(201), newRequest);
});

test("propagates storage errors from publish, read, consume, and remove", async t => {
    const storageError = new Error("Session storage is unavailable.");
    const validRequest = {
        requestId: "request-1",
        tabId: 101,
        windowId: 201,
        timestamp: 5_000
    };

    await t.test("publish", async () => {
        const store = createExtractionRequestStore({
            storageSession: {
                async get() { return {}; },
                async set() { throw storageError; },
                async remove() {}
            },
            now: () => 5_000
        });

        await assert.rejects(
            store.publishExtractionRequest(validRequest),
            storageError
        );
    });

    await t.test("read and consume get", async () => {
        const store = createExtractionRequestStore({
            storageSession: {
                async get() { throw storageError; },
                async set() {},
                async remove() {}
            },
            now: () => 5_000
        });

        await assert.rejects(store.readExtractionRequest(201), storageError);
        await assert.rejects(store.consumeExtractionRequest(201), storageError);
    });

    await t.test("consume remove", async () => {
        const key = getExtractionRequestStorageKey(201, validRequest.requestId);
        const store = createExtractionRequestStore({
            storageSession: {
                async get() { return { [key]: validRequest }; },
                async set() {},
                async remove() { throw storageError; }
            },
            now: () => 5_000
        });

        await assert.rejects(store.consumeExtractionRequest(201), storageError);
        await assert.rejects(store.removeExtractionRequest(201), storageError);
    });
});
