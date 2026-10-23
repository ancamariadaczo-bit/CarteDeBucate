const extractorFiles = [
    "extractors/jsonLdImporter.js",
    "extractors/htmlImporter.js",
    "extractors/recipeExtractor.js"
];

import("./controllers/sidePanelController.js").then(async ({
    extractRecipeFromTab,
    initializeSidePanelController
}) => {
    const {
        recipeExistsBySourceUrl,
        saveRecipeToApi
    } = await import("./api/recipeApiClient.js");

    const { getCurrentUser } = await import("./api/authenticationApiClient.js");

    const {
        getAccessToken,
        getAuthenticationStateFromStorageChange,
        removeAccessToken
    } = await import("./auth/authStorage.js");

    const { resolveAuthenticationState } = await import("./auth/authenticationState.js");

    const {
        createExtractionRequestStore,
        getExtractionRequestStorageKeyPrefix
    } = await import("./sidepanel/extractionRequestStore.js");

    const reportError = error => {
        console.error(error);
    };

    async function login() {
        const response = await chrome.runtime.sendMessage({
            type: "LOGIN"
        });

        if (!response?.success) {
            throw new Error(
                response?.error ?? "Login failed."
            );
        }
    }

    const controller = initializeSidePanelController({
        document,
        extractRecipe: tabId => extractRecipeFromTab({
            chrome,
            extractorFiles,
            tabId
        }),
        saveRecipe: recipe => {
            localStorage.setItem("recipeToPrint", JSON.stringify(recipe));
        },
        saveRecipeToApi: async recipe => {
            const accessToken = await getAccessToken();

            return saveRecipeToApi(
                recipe,
                accessToken
            );
        },
        recipeExistsBySourceUrl: async sourceUrl => {
            const accessToken = await getAccessToken();

            return recipeExistsBySourceUrl(
                sourceUrl,
                accessToken
            );
        },
        removeAccessToken,
        initialIsAuthenticated: false,
        initialIsAuthenticationResolved: false,
        login,
        openWindow: ({ page, type, width, height }) => {
            return chrome.windows.create({
                url: chrome.runtime.getURL(page),
                type,
                width,
                height
            });
        },
        reportError
    });

    const currentWindow = await chrome.windows.getCurrent();
    const windowId = currentWindow?.id;

    if (!Number.isInteger(windowId) || windowId < 0) {
        throw new Error("The Side Panel window could not be identified.");
    }

    const requestStore = createExtractionRequestStore({
        storageSession: chrome.storage.session
    });
    const requestStorageKeyPrefix = getExtractionRequestStorageKeyPrefix(windowId);
    const forwardedRequestIds = new Set();
    let authenticationStateRevision = 0;
    let pendingConsumption = Promise.resolve();

    function consumePendingRequests() {
        const currentConsumption = pendingConsumption
            .catch(() => {})
            .then(async () => {
                while (true) {
                    const request = await requestStore.consumeExtractionRequest(windowId);

                    if (!request) {
                        return;
                    }

                    if (forwardedRequestIds.has(request.requestId)) {
                        continue;
                    }

                    forwardedRequestIds.add(request.requestId);
                    await controller.handleExtractionRequest(request);
                }
            })
            .catch(reportError);

        pendingConsumption = currentConsumption;
        return currentConsumption;
    }

    function handleStorageChange(changes, areaName) {
        const isAuthenticated =
            getAuthenticationStateFromStorageChange(changes, areaName);

        if (isAuthenticated !== null) {
            authenticationStateRevision += 1;
            controller.setAuthenticationState(isAuthenticated);
            return;
        }

        if (
            areaName !== "session"
            || !Object.entries(changes ?? {}).some(([key, change]) =>
                key.startsWith(requestStorageKeyPrefix)
                && change?.newValue !== undefined
            )
        ) {
            return;
        }

        void consumePendingRequests();
    }

    chrome.storage.onChanged.addListener(handleStorageChange);

    window.addEventListener("unload", () => {
        chrome.storage.onChanged.removeListener(handleStorageChange);
    }, { once: true });

    const initialConsumption = consumePendingRequests();
    const initialAuthenticationRevision = authenticationStateRevision;

    void resolveAuthenticationState({
        getAccessToken,
        getCurrentUser,
        removeAccessToken,
        reportError
    })
        .then(isAuthenticated => {
            if (authenticationStateRevision === initialAuthenticationRevision) {
                controller.setAuthenticationState(isAuthenticated);
            }
        })
        .catch(reportError);

    await initialConsumption;
}).catch(error => {
    console.error(error);
});
