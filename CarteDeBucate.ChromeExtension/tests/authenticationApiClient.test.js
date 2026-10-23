import test from "node:test";
import assert from "node:assert/strict";

import {
    currentUserApiUrl,
    exchangeAuthenticationCodeApiUrl,
    exchangeAuthenticationCode,
    getCurrentUser
} from "../api/authenticationApiClient.js";
import { API_ENDPOINTS } from "../config/apiConfig.js";

test("authentication API URLs use the centralized endpoints", () => {
    assert.equal(currentUserApiUrl, API_ENDPOINTS.currentUser);
    assert.equal(
        exchangeAuthenticationCodeApiUrl,
        API_ENDPOINTS.exchangeCode
    );
});

test("getCurrentUser returns null without making a request when the token is missing", async () => {
    let requestCount = 0;
    const fetchRequest = async () => {
        requestCount += 1;
        throw new Error("Unexpected network request.");
    };

    const result = await getCurrentUser(null, fetchRequest);

    assert.equal(result, null);
    assert.equal(requestCount, 0);
});

test("getCurrentUser sends the Bearer token and returns the current user", async () => {
    const requests = [];
    const accessToken = "test-access-token";
    const currentUser = {
        userId: "user-42",
        username: "chef"
    };
    const fetchRequest = async (...request) => {
        requests.push(request);

        return {
            ok: true,
            status: 200,
            json: async () => currentUser
        };
    };

    const result = await getCurrentUser(accessToken, fetchRequest);

    assert.deepEqual(requests, [[
        API_ENDPOINTS.currentUser,
        {
            method: "GET",
            headers: {
                "Authorization": `Bearer ${accessToken}`
            }
        }
    ]]);
    assert.deepEqual(result, currentUser);
});

test("getCurrentUser returns null for an unauthorized response", async () => {
    const fetchRequest = async () => ({
        ok: false,
        status: 401
    });

    const result = await getCurrentUser("invalid-token", fetchRequest);

    assert.equal(result, null);
});

test("getCurrentUser throws for a non-401 error response", async () => {
    const fetchRequest = async () => ({
        ok: false,
        status: 503
    });

    await assert.rejects(
        () => getCurrentUser("test-access-token", fetchRequest),
        new Error("Authentication check failed with status 503.")
    );
});

test("exchangeAuthenticationCode rejects a missing code without making a request", async () => {
    let requestCount = 0;
    const fetchRequest = async () => {
        requestCount += 1;
        throw new Error("Unexpected network request.");
    };

    await assert.rejects(
        () => exchangeAuthenticationCode(null, fetchRequest),
        new Error("Authentication code is required.")
    );
    assert.equal(requestCount, 0);
});

test("exchangeAuthenticationCode sends the code as JSON and returns the access token", async () => {
    const requests = [];
    const code = "temporary-authentication-code";
    const accessToken = "issued-access-token";
    const fetchRequest = async (...request) => {
        requests.push(request);

        return {
            ok: true,
            status: 200,
            json: async () => ({ accessToken })
        };
    };

    const result = await exchangeAuthenticationCode(code, fetchRequest);

    assert.deepEqual(requests, [[
        API_ENDPOINTS.exchangeCode,
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({ code })
        }
    ]]);
    assert.equal(result, accessToken);
});

test("exchangeAuthenticationCode uses the error returned by the API", async () => {
    const fetchRequest = async () => ({
        ok: false,
        status: 400,
        json: async () => ({
            message: "The authentication code is invalid or expired."
        })
    });

    await assert.rejects(
        () => exchangeAuthenticationCode("invalid-code", fetchRequest),
        new Error("The authentication code is invalid or expired.")
    );
});

test("exchangeAuthenticationCode rejects successful responses without a valid access token", async t => {
    const scenarios = [
        { name: "missing token", responseBody: {} },
        { name: "null token", responseBody: { accessToken: null } },
        { name: "numeric token", responseBody: { accessToken: 42 } },
        { name: "empty token", responseBody: { accessToken: "" } },
        { name: "blank token", responseBody: { accessToken: "   " } }
    ];

    for (const scenario of scenarios) {
        await t.test(scenario.name, async () => {
            const fetchRequest = async () => ({
                ok: true,
                status: 200,
                json: async () => scenario.responseBody
            });

            await assert.rejects(
                () => exchangeAuthenticationCode(
                    "temporary-authentication-code",
                    fetchRequest
                ),
                new Error(
                    "The authentication response did not contain a valid access token."
                )
            );
        });
    }
});

test("exchangeAuthenticationCode rejects a successful non-JSON response", async () => {
    const fetchRequest = async () => ({
        ok: true,
        status: 200,
        json: async () => {
            throw new SyntaxError("Unexpected end of JSON input.");
        }
    });

    await assert.rejects(
        () => exchangeAuthenticationCode(
            "temporary-authentication-code",
            fetchRequest
        ),
        new Error("The authentication response was not valid JSON.")
    );
});

test("exchangeAuthenticationCode preserves the HTTP status for a non-JSON error response", async () => {
    const fetchRequest = async () => ({
        ok: false,
        status: 502,
        json: async () => {
            throw new SyntaxError("Unexpected token '<'.");
        }
    });

    await assert.rejects(
        () => exchangeAuthenticationCode(
            "temporary-authentication-code",
            fetchRequest
        ),
        new Error("Authentication code exchange failed with status 502.")
    );
});
