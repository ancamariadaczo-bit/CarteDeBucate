import test from "node:test";
import assert from "node:assert/strict";

import {
    runAuthenticationFlow
} from "../auth/authenticationFlow.js";

const loginUrl =
    "https://example.test/Account/Login?returnUrl=%2Fapi%2Fauthentication%2Fextension-complete";
const authenticationCode = "temporary-authentication-code";
const accessToken = "issued-access-token";
const callbackUrl =
    `https://extension.chromiumapp.org/authentication-callback?code=${authenticationCode}`;

test("runAuthenticationFlow launches one interactive flow, exchanges the code, then saves the token", async () => {
    const launchCalls = [];
    const exchangedCodes = [];
    const savedTokens = [];
    const operationOrder = [];

    await runAuthenticationFlow({
        launchWebAuthFlow: async options => {
            launchCalls.push(options);
            return callbackUrl;
        },
        exchangeAuthenticationCode: async code => {
            operationOrder.push("exchange");
            exchangedCodes.push(code);
            return accessToken;
        },
        saveAccessToken: async token => {
            operationOrder.push("save");
            savedTokens.push(token);
        },
        loginUrl
    });

    assert.deepEqual(launchCalls, [{
        url: loginUrl,
        interactive: true
    }]);
    assert.deepEqual(exchangedCodes, [authenticationCode]);
    assert.deepEqual(savedTokens, [accessToken]);
    assert.deepEqual(operationOrder, ["exchange", "save"]);
});

test("runAuthenticationFlow rejects a missing callback", async () => {
    let exchangeCount = 0;
    let saveCount = 0;

    await assert.rejects(
        () => runAuthenticationFlow({
            launchWebAuthFlow: async () => null,
            exchangeAuthenticationCode: async () => {
                exchangeCount += 1;
            },
            saveAccessToken: async () => {
                saveCount += 1;
            },
            loginUrl
        }),
        new Error("Authentication did not complete.")
    );
    assert.equal(exchangeCount, 0);
    assert.equal(saveCount, 0);
});

test("runAuthenticationFlow rejects an invalid callback URL", async () => {
    await assert.rejects(
        () => runAuthenticationFlow({
            launchWebAuthFlow: async () => "not-a-valid-url",
            exchangeAuthenticationCode: async () => accessToken,
            saveAccessToken: async () => {},
            loginUrl
        }),
        new Error("Authentication callback URL is invalid.")
    );
});

test("runAuthenticationFlow rejects a callback without a code", async () => {
    let exchangeCount = 0;

    await assert.rejects(
        () => runAuthenticationFlow({
            launchWebAuthFlow: async () =>
                "https://extension.chromiumapp.org/authentication-callback",
            exchangeAuthenticationCode: async () => {
                exchangeCount += 1;
            },
            saveAccessToken: async () => {},
            loginUrl
        }),
        new Error("Authentication code was not returned.")
    );
    assert.equal(exchangeCount, 0);
});

test("runAuthenticationFlow propagates a launch error", async () => {
    const launchError = new Error("The login window could not be opened.");

    await assert.rejects(
        () => runAuthenticationFlow({
            launchWebAuthFlow: async () => {
                throw launchError;
            },
            exchangeAuthenticationCode: async () => accessToken,
            saveAccessToken: async () => {},
            loginUrl
        }),
        launchError
    );
});

test("runAuthenticationFlow propagates an exchange error without saving", async () => {
    const exchangeError = new Error("The code exchange failed.");
    let saveCount = 0;

    await assert.rejects(
        () => runAuthenticationFlow({
            launchWebAuthFlow: async () => callbackUrl,
            exchangeAuthenticationCode: async () => {
                throw exchangeError;
            },
            saveAccessToken: async () => {
                saveCount += 1;
            },
            loginUrl
        }),
        exchangeError
    );
    assert.equal(saveCount, 0);
});

test("runAuthenticationFlow propagates a storage error", async () => {
    const storageError = new Error("The access token could not be saved.");

    await assert.rejects(
        () => runAuthenticationFlow({
            launchWebAuthFlow: async () => callbackUrl,
            exchangeAuthenticationCode: async () => accessToken,
            saveAccessToken: async () => {
                throw storageError;
            },
            loginUrl
        }),
        storageError
    );
});
