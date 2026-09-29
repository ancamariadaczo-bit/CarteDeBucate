import test from "node:test";
import assert from "node:assert/strict";

import {
    getAccessToken,
    getAuthenticationStateFromStorageChange,
    removeAccessToken,
    saveAccessToken
} from "../auth/authStorage.js";

function installChromeStorageMock(initialValues = {}) {
    const hadChrome = Object.hasOwn(globalThis, "chrome");
    const originalChrome = globalThis.chrome;
    const storedValues = { ...initialValues };
    const calls = {
        get: [],
        set: [],
        remove: []
    };

    globalThis.chrome = {
        storage: {
            local: {
                get: async key => {
                    calls.get.push(key);

                    if (!Object.hasOwn(storedValues, key)) {
                        return {};
                    }

                    return {
                        [key]: storedValues[key]
                    };
                },
                set: async values => {
                    calls.set.push(values);
                    Object.assign(storedValues, values);
                },
                remove: async key => {
                    calls.remove.push(key);
                    delete storedValues[key];
                }
            }
        }
    };

    return {
        calls,
        storedValues,
        restore: () => {
            if (hadChrome) {
                globalThis.chrome = originalChrome;
                return;
            }

            delete globalThis.chrome;
        }
    };
}

test("getAccessToken returns null when the access token is absent", async () => {
    const storage = installChromeStorageMock();

    try {
        const result = await getAccessToken();

        assert.equal(result, null);
        assert.deepEqual(storage.calls.get, ["accessToken"]);
    } finally {
        storage.restore();
    }
});

test("getAccessToken reads the stored access token", async () => {
    const storage = installChromeStorageMock({
        accessToken: "stored-access-token"
    });

    try {
        const result = await getAccessToken();

        assert.equal(result, "stored-access-token");
        assert.deepEqual(storage.calls.get, ["accessToken"]);
    } finally {
        storage.restore();
    }
});

test("saveAccessToken stores the token under the accessToken key", async () => {
    const storage = installChromeStorageMock();

    try {
        await saveAccessToken("new-access-token");

        assert.deepEqual(storage.calls.set, [{
            accessToken: "new-access-token"
        }]);
        assert.equal(storage.storedValues.accessToken, "new-access-token");
    } finally {
        storage.restore();
    }
});

test("removeAccessToken removes the accessToken key", async () => {
    const storage = installChromeStorageMock({
        accessToken: "stored-access-token"
    });

    try {
        await removeAccessToken();

        assert.deepEqual(storage.calls.remove, ["accessToken"]);
        assert.equal(
            Object.hasOwn(storage.storedValues, "accessToken"),
            false
        );
    } finally {
        storage.restore();
    }
});

test("storage changes expose authentication updates from other extension contexts", async t => {
    const scenarios = [
        {
            name: "token added",
            changes: {
                accessToken: {
                    oldValue: undefined,
                    newValue: "new-access-token"
                }
            },
            areaName: "local",
            expected: true
        },
        {
            name: "token removed",
            changes: {
                accessToken: {
                    oldValue: "old-access-token",
                    newValue: undefined
                }
            },
            areaName: "local",
            expected: false
        },
        {
            name: "blank token",
            changes: {
                accessToken: {
                    newValue: "   "
                }
            },
            areaName: "local",
            expected: false
        },
        {
            name: "unrelated local change",
            changes: {
                preference: {
                    newValue: true
                }
            },
            areaName: "local",
            expected: null
        },
        {
            name: "session storage change",
            changes: {
                accessToken: {
                    newValue: "new-access-token"
                }
            },
            areaName: "session",
            expected: null
        }
    ];

    for (const scenario of scenarios) {
        await t.test(scenario.name, () => {
            assert.equal(
                getAuthenticationStateFromStorageChange(
                    scenario.changes,
                    scenario.areaName
                ),
                scenario.expected
            );
        });
    }
});
