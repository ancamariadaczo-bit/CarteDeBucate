import test from "node:test";
import assert from "node:assert/strict";

import {
    resolveAuthenticationState
} from "../auth/authenticationState.js";

test("resolveAuthenticationState returns false without checking the user when the token is absent", async () => {
    let currentUserRequestCount = 0;
    let removeCount = 0;
    const reportedErrors = [];

    const result = await resolveAuthenticationState({
        getAccessToken: async () => null,
        getCurrentUser: async () => {
            currentUserRequestCount += 1;
        },
        removeAccessToken: async () => {
            removeCount += 1;
        },
        reportError: error => reportedErrors.push(error)
    });

    assert.equal(result, false);
    assert.equal(currentUserRequestCount, 0);
    assert.equal(removeCount, 0);
    assert.deepEqual(reportedErrors, []);
});

test("resolveAuthenticationState returns true and keeps a valid token", async () => {
    const accessToken = "valid-access-token";
    const requestedTokens = [];
    let removeCount = 0;
    const reportedErrors = [];

    const result = await resolveAuthenticationState({
        getAccessToken: async () => accessToken,
        getCurrentUser: async token => {
            requestedTokens.push(token);

            return {
                userId: "user-42",
                username: "chef"
            };
        },
        removeAccessToken: async () => {
            removeCount += 1;
        },
        reportError: error => reportedErrors.push(error)
    });

    assert.equal(result, true);
    assert.deepEqual(requestedTokens, [accessToken]);
    assert.equal(removeCount, 0);
    assert.deepEqual(reportedErrors, []);
});

test("resolveAuthenticationState removes an invalid token when the current user is null", async () => {
    let removeCount = 0;
    const reportedErrors = [];

    const result = await resolveAuthenticationState({
        getAccessToken: async () => "invalid-access-token",
        getCurrentUser: async () => null,
        removeAccessToken: async () => {
            removeCount += 1;
        },
        reportError: error => reportedErrors.push(error)
    });

    assert.equal(result, false);
    assert.equal(removeCount, 1);
    assert.deepEqual(reportedErrors, []);
});

test("resolveAuthenticationState reports a transient error without removing the token", async () => {
    const transientError = new Error("The authentication server is unavailable.");
    let removeCount = 0;
    const reportedErrors = [];

    const result = await resolveAuthenticationState({
        getAccessToken: async () => "stored-access-token",
        getCurrentUser: async () => {
            throw transientError;
        },
        removeAccessToken: async () => {
            removeCount += 1;
        },
        reportError: error => reportedErrors.push(error)
    });

    assert.equal(result, false);
    assert.equal(removeCount, 0);
    assert.deepEqual(reportedErrors, [transientError]);
});
