import test from "node:test";
import assert from "node:assert/strict";

import {
    refreshWebLoginTabs
} from "../auth/webLoginTabs.js";

const loginPageUrl = "https://localhost:7080/Account/Login";

test("refreshWebLoginTabs reloads matching login tabs regardless of query string", async () => {
    const queryCalls = [];
    const reloadedTabIds = [];

    await refreshWebLoginTabs({
        queryTabs: async queryInfo => {
            queryCalls.push(queryInfo);

            return [
                {
                    id: 11,
                    url: "https://localhost:7080/Account/Login"
                },
                {
                    id: 12,
                    url: "https://localhost:7080/Account/Login?returnUrl=%2FRecipes"
                },
                {
                    id: 13,
                    url: "https://localhost:7080/Recipes"
                }
            ];
        },
        reloadTab: async tabId => {
            reloadedTabIds.push(tabId);
        },
        loginPageUrl
    });

    assert.deepEqual(queryCalls, [{}]);
    assert.deepEqual(reloadedTabIds, [11, 12]);
});

test("refreshWebLoginTabs ignores tabs from other origins and invalid tabs", async () => {
    const reloadedTabIds = [];

    await refreshWebLoginTabs({
        queryTabs: async () => [
            {
                id: 21,
                url: "https://example.test/Account/Login"
            },
            {
                id: 22,
                url: "not-a-valid-url"
            },
            {
                url: "https://localhost:7080/Account/Login"
            }
        ],
        reloadTab: async tabId => {
            reloadedTabIds.push(tabId);
        },
        loginPageUrl
    });

    assert.deepEqual(reloadedTabIds, []);
});

test("refreshWebLoginTabs matches the login path case-insensitively and with a trailing slash", async () => {
    const reloadedTabIds = [];

    await refreshWebLoginTabs({
        queryTabs: async () => [
            {
                id: 31,
                url: "https://localhost:7080/account/login/"
            }
        ],
        reloadTab: async tabId => {
            reloadedTabIds.push(tabId);
        },
        loginPageUrl
    });

    assert.deepEqual(reloadedTabIds, [31]);
});
