import {
    exchangeAuthenticationCode
} from "./api/authenticationApiClient.js";

import {
    saveAccessToken
} from "./auth/authStorage.js";

import {
    runAuthenticationFlow
} from "./auth/authenticationFlow.js";

import {
    refreshWebLoginTabs
} from "./auth/webLoginTabs.js";

import {
    API_ENDPOINTS
} from "./config/apiConfig.js";

import {
    createExtractionRequestStore
} from "./sidepanel/extractionRequestStore.js";

import {
    initializeSidePanelLauncher
} from "./sidepanel/sidePanelLauncher.js";

const extractionRequestStore = createExtractionRequestStore({
    storageSession: chrome.storage.session
});

initializeSidePanelLauncher({
    addActionClickListener: listener =>
        chrome.action.onClicked.addListener(listener),
    openSidePanel: options =>
        chrome.sidePanel.open(options),
    publishExtractionRequest: request =>
        extractionRequestStore.publishExtractionRequest(request),
    createRequestId: () => crypto.randomUUID(),
    reportError: error => console.error(error)
});

const returnUrl =
    "/api/authentication/extension-complete";

const loginUrl =
    API_ENDPOINTS.mvcLogin +
    `?returnUrl=${encodeURIComponent(returnUrl)}`;

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.type !== "LOGIN") {
        return;
    }

    runAuthenticationFlow({
        launchWebAuthFlow: options =>
            chrome.identity.launchWebAuthFlow(options),
        exchangeAuthenticationCode,
        saveAccessToken,
        loginUrl
    })
        .then(async () => {
            try {
                await refreshWebLoginTabs({
                    queryTabs: queryInfo =>
                        chrome.tabs.query(queryInfo),
                    reloadTab: tabId =>
                        chrome.tabs.reload(tabId),
                    loginPageUrl: API_ENDPOINTS.mvcLogin
                });
            } catch (error) {
                console.error(
                    "Web login tabs could not be refreshed:",
                    error
                );
            }

            sendResponse({
                success: true
            });
        })
        .catch(error => {
            console.error("Login failed:", error);

            sendResponse({
                success: false,
                error: error.message
            });
        });

    return true;
});
