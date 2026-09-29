export function initializeSidePanelLauncher({
    addActionClickListener,
    openSidePanel,
    publishExtractionRequest,
    createRequestId,
    reportError
}) {
    assertFunction(addActionClickListener, "addActionClickListener");
    assertFunction(openSidePanel, "openSidePanel");
    assertFunction(publishExtractionRequest, "publishExtractionRequest");
    assertFunction(createRequestId, "createRequestId");
    assertFunction(reportError, "reportError");

    async function handleActionClick(tab) {
        const tabId = tab?.id;
        const windowId = tab?.windowId;

        if (!isChromeId(tabId) || !isChromeId(windowId)) {
            reportError(new TypeError(
                "The action click did not provide a valid tab id and window id."
            ));
            return;
        }

        try {
            await openSidePanel({ windowId });
        } catch (error) {
            reportError(new Error("The Side Panel could not be opened.", {
                cause: error
            }));
            return;
        }

        try {
            const requestId = createRequestId();

            if (typeof requestId !== "string" || requestId.trim().length === 0) {
                throw new TypeError("createRequestId must return a non-empty string.");
            }

            await publishExtractionRequest({ requestId, tabId, windowId });
        } catch (error) {
            reportError(new Error("The extraction request could not be published.", {
                cause: error
            }));
        }
    }

    addActionClickListener(handleActionClick);

    return handleActionClick;
}

function assertFunction(value, name) {
    if (typeof value !== "function") {
        throw new TypeError(`${name} must be a function.`);
    }
}

function isChromeId(value) {
    return Number.isInteger(value) && value >= 0;
}
