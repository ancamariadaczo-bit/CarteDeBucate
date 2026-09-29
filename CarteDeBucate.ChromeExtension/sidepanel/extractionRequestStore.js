const EXTRACTION_REQUEST_KEY_PREFIX = "sidePanelExtractionRequest:";

export const EXTRACTION_REQUEST_MAX_AGE_MS = 60_000;

export function getExtractionRequestStorageKeyPrefix(windowId) {
    assertChromeId(windowId, "windowId");
    return `${EXTRACTION_REQUEST_KEY_PREFIX}${windowId}:`;
}

export function getExtractionRequestStorageKey(windowId, requestId) {
    assertRequestId(requestId);
    return `${getExtractionRequestStorageKeyPrefix(windowId)}${encodeURIComponent(requestId)}`;
}

export function createExtractionRequestStore({
    storageSession,
    now = () => Date.now()
}) {
    assertStorageSession(storageSession);

    const consumedRequestIds = new Set();
    const consumptionQueues = new Map();

    async function publishExtractionRequest({ requestId, tabId, windowId }) {
        const timestamp = now();
        const request = { requestId, tabId, windowId, timestamp };

        assertValidRequest(request);

        await storageSession.set({
            [getExtractionRequestStorageKey(windowId, requestId)]: request
        });

        return request;
    }

    async function readExtractionRequest(windowId) {
        const entries = await readWindowRequestEntries(windowId);
        const candidate = findOldestConsumableEntry(entries, windowId);

        return candidate?.request ?? null;
    }

    async function removeExtractionRequest(windowId) {
        const entries = await readWindowRequestEntries(windowId);
        const keys = entries.map(([key]) => key);

        if (keys.length > 0) {
            await storageSession.remove(keys);
        }
    }

    function consumeExtractionRequest(windowId) {
        const queueKey = getExtractionRequestStorageKeyPrefix(windowId);
        const previousConsumption = consumptionQueues.get(queueKey) ?? Promise.resolve();
        const currentConsumption = previousConsumption
            .catch(() => {})
            .then(async () => {
                const entries = await readWindowRequestEntries(windowId);
                const candidate = findOldestConsumableEntry(entries, windowId);
                const keysToRemove = entries
                    .filter(([key, request]) =>
                        !isConsumableEntry(key, request, windowId)
                    )
                    .map(([key]) => key);

                if (candidate) {
                    keysToRemove.push(candidate.key);
                }

                if (keysToRemove.length > 0) {
                    // Every request has its own key, so removing this snapshot cannot
                    // delete a newer action click published by another context.
                    await storageSession.remove([...new Set(keysToRemove)]);
                }

                if (!candidate) {
                    return null;
                }

                consumedRequestIds.add(candidate.request.requestId);
                return candidate.request;
            });

        consumptionQueues.set(queueKey, currentConsumption);

        return currentConsumption.finally(() => {
            if (consumptionQueues.get(queueKey) === currentConsumption) {
                consumptionQueues.delete(queueKey);
            }
        });
    }

    async function readWindowRequestEntries(windowId) {
        const keyPrefix = getExtractionRequestStorageKeyPrefix(windowId);
        const storedValues = await storageSession.get(null);

        return Object.entries(storedValues ?? {})
            .filter(([key]) => key.startsWith(keyPrefix));
    }

    function findOldestConsumableEntry(entries, windowId) {
        return entries
            .filter(([key, request]) =>
                isConsumableEntry(key, request, windowId)
            )
            .map(([key, request]) => ({ key, request }))
            .sort((left, right) =>
                left.request.timestamp - right.request.timestamp
                || left.request.requestId.localeCompare(right.request.requestId)
            )[0] ?? null;
    }

    function isConsumableEntry(key, request, expectedWindowId) {
        return isConsumableRequest(request, expectedWindowId)
            && key === getExtractionRequestStorageKey(
                request.windowId,
                request.requestId
            );
    }

    function isConsumableRequest(request, expectedWindowId) {
        if (!isValidRequest(request) || request.windowId !== expectedWindowId) {
            return false;
        }

        const currentTimestamp = now();
        const age = currentTimestamp - request.timestamp;

        return Number.isFinite(currentTimestamp)
            && age >= 0
            && age <= EXTRACTION_REQUEST_MAX_AGE_MS
            && !consumedRequestIds.has(request.requestId);
    }

    return {
        publishExtractionRequest,
        readExtractionRequest,
        consumeExtractionRequest,
        removeExtractionRequest
    };
}

function assertStorageSession(storageSession) {
    if (
        !storageSession
        || typeof storageSession.get !== "function"
        || typeof storageSession.set !== "function"
        || typeof storageSession.remove !== "function"
    ) {
        throw new TypeError("storageSession must provide get, set, and remove functions.");
    }
}

function assertChromeId(value, name) {
    if (!Number.isInteger(value) || value < 0) {
        throw new TypeError(`${name} must be a non-negative integer.`);
    }
}

function assertRequestId(requestId) {
    if (typeof requestId !== "string" || requestId.trim().length === 0) {
        throw new TypeError("requestId must be a non-empty string.");
    }
}

function assertValidRequest(request) {
    if (!isValidRequest(request)) {
        throw new TypeError("The extraction request is invalid.");
    }
}

function isValidRequest(request) {
    if (!request || typeof request !== "object" || Array.isArray(request)) {
        return false;
    }

    const keys = Object.keys(request).sort();
    const expectedKeys = ["requestId", "tabId", "timestamp", "windowId"];

    return keys.length === expectedKeys.length
        && keys.every((key, index) => key === expectedKeys[index])
        && typeof request.requestId === "string"
        && request.requestId.trim().length > 0
        && Number.isInteger(request.tabId)
        && request.tabId >= 0
        && Number.isInteger(request.windowId)
        && request.windowId >= 0
        && Number.isFinite(request.timestamp);
}
