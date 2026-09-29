import assert from "node:assert/strict";
import test from "node:test";
import {
    initializeSidePanelLauncher
} from "../sidepanel/sidePanelLauncher.js";

function setupLauncher({
    requestIds = ["request-1"],
    openSidePanelError,
    publishExtractionRequestError
} = {}) {
    const listeners = [];
    const events = [];
    const reportedErrors = [];
    let requestIdIndex = 0;

    initializeSidePanelLauncher({
        addActionClickListener: listener => listeners.push(listener),
        openSidePanel: async details => {
            events.push({ operation: "open", details });

            if (openSidePanelError) {
                throw openSidePanelError;
            }
        },
        publishExtractionRequest: async request => {
            events.push({ operation: "publish", request });

            if (publishExtractionRequestError) {
                throw publishExtractionRequestError;
            }
        },
        createRequestId: () => requestIds[requestIdIndex++],
        reportError: error => reportedErrors.push(error)
    });

    return { listeners, events, reportedErrors };
}

test("registers one action listener and opens the panel before publishing", async () => {
    const context = setupLauncher();

    assert.equal(context.listeners.length, 1);

    await context.listeners[0]({ id: 101, windowId: 201 });

    assert.deepEqual(context.events, [
        {
            operation: "open",
            details: { windowId: 201 }
        },
        {
            operation: "publish",
            request: {
                requestId: "request-1",
                tabId: 101,
                windowId: 201
            }
        }
    ]);
    assert.deepEqual(context.reportedErrors, []);
});

test("does not open or publish when the tab id or window id is invalid", async t => {
    for (const [name, tab] of [
        ["missing tab id", { windowId: 201 }],
        ["missing window id", { id: 101 }],
        ["invalid tab id", { id: -1, windowId: 201 }],
        ["invalid window id", { id: 101, windowId: -1 }]
    ]) {
        await t.test(name, async () => {
            const context = setupLauncher();

            await context.listeners[0](tab);

            assert.deepEqual(context.events, []);
            assert.equal(context.reportedErrors.length, 1);
            assert.equal(context.reportedErrors[0] instanceof TypeError, true);
        });
    }
});

test("creates and publishes a new request for every repeated click", async () => {
    const context = setupLauncher({
        requestIds: ["request-1", "request-2"]
    });
    const listener = context.listeners[0];

    await listener({ id: 101, windowId: 201 });
    await listener({ id: 102, windowId: 201 });

    assert.deepEqual(
        context.events.filter(event => event.operation === "publish"),
        [
            {
                operation: "publish",
                request: {
                    requestId: "request-1",
                    tabId: 101,
                    windowId: 201
                }
            },
            {
                operation: "publish",
                request: {
                    requestId: "request-2",
                    tabId: 102,
                    windowId: 201
                }
            }
        ]
    );
});

test("reports an open error without attempting to publish", async () => {
    const openError = new Error("Side Panel API failed.");
    const context = setupLauncher({ openSidePanelError: openError });

    await context.listeners[0]({ id: 101, windowId: 201 });

    assert.deepEqual(context.events, [
        {
            operation: "open",
            details: { windowId: 201 }
        }
    ]);
    assert.equal(context.reportedErrors.length, 1);
    assert.equal(
        context.reportedErrors[0].message,
        "The Side Panel could not be opened."
    );
    assert.equal(context.reportedErrors[0].cause, openError);
});

test("reports a publish error separately after the panel opens", async () => {
    const publishError = new Error("Session storage failed.");
    const context = setupLauncher({
        publishExtractionRequestError: publishError
    });

    await context.listeners[0]({ id: 101, windowId: 201 });

    assert.deepEqual(
        context.events.map(event => event.operation),
        ["open", "publish"]
    );
    assert.equal(context.reportedErrors.length, 1);
    assert.equal(
        context.reportedErrors[0].message,
        "The extraction request could not be published."
    );
    assert.equal(context.reportedErrors[0].cause, publishError);
});
