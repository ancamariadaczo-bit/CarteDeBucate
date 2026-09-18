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
    API_ENDPOINTS
} from "./config/apiConfig.js";

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
        .then(() => {
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
