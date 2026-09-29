import test from "node:test";
import assert from "node:assert/strict";

import {
    recipeApiUrl,
    saveRecipeToApi
} from "../api/recipeApiClient.js";

const recipe = {
    name: "Tomato soup",
    sourceUrl: "https://example.com/tomato-soup",
    ingredients: ["Tomatoes", "Salt"],
    steps: ["Cook the tomatoes", "Add salt"],
    imageUrl: "https://example.com/tomato-soup.jpg"
};

test("saveRecipeToApi sends the recipe to the recipes API and returns its response", async () => {
    const requests = [];
    const accessToken = "test-access-token";
    const apiResponse = {
        message: "Recipe saved successfully.",
        id: 42
    };
    const fetchRequest = async (...request) => {
        requests.push(request);

        return {
            ok: true,
            status: 201,
            json: async () => apiResponse
        };
    };

    const result = await saveRecipeToApi(recipe, accessToken, fetchRequest);

    assert.deepEqual(requests, [[
        recipeApiUrl,
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": `Bearer ${accessToken}`
            },
            body: JSON.stringify({
                name: recipe.name,
                sourceUrl: recipe.sourceUrl,
                ingredients: recipe.ingredients,
                steps: recipe.steps
            })
        }
    ]]);
    assert.deepEqual(result, apiResponse);
});

test("saveRecipeToApi uses the API message when the request fails", async () => {
    const accessToken = "test-access-token";
    const fetchRequest = async () => ({
        ok: false,
        status: 400,
        json: async () => ({
            message: "The recipe could not be saved."
        })
    });

    await assert.rejects(
        () => saveRecipeToApi(recipe, accessToken, fetchRequest),
        new Error("The recipe could not be saved.")
    );
});

test("saveRecipeToApi uses the problem title when the API does not return a message", async () => {
    const accessToken = "test-access-token";
    const fetchRequest = async () => ({
        ok: false,
        status: 400,
        json: async () => ({
            title: "Validation failed."
        })
    });

    await assert.rejects(
        () => saveRecipeToApi(recipe, accessToken, fetchRequest),
        new Error("Validation failed.")
    );
});

test("saveRecipeToApi includes the HTTP status when the API returns no error details", async () => {
    const accessToken = "test-access-token";
    const fetchRequest = async () => ({
        ok: false,
        status: 500,
        json: async () => ({})
    });

    await assert.rejects(
        () => saveRecipeToApi(recipe, accessToken, fetchRequest),
        new Error("API request failed with status 500")
    );
});

test("saveRecipeToApi preserves an unauthorized status without parsing an empty body", async () => {
    let jsonReadCount = 0;
    const fetchRequest = async () => ({
        ok: false,
        status: 401,
        json: async () => {
            jsonReadCount += 1;
            throw new Error("The empty response body cannot be parsed.");
        }
    });

    await assert.rejects(
        () => saveRecipeToApi(recipe, "expired-access-token", fetchRequest),
        error => error instanceof Error
            && error.status === 401
            && error.message === "Authentication has expired. Sign in again."
    );
    assert.equal(jsonReadCount, 0);
});

test("saveRecipeToApi uses the HTTP status when an error body is not JSON", async () => {
    const fetchRequest = async () => ({
        ok: false,
        status: 503,
        json: async () => {
            throw new SyntaxError("Unexpected token '<'.");
        }
    });

    await assert.rejects(
        () => saveRecipeToApi(recipe, "test-access-token", fetchRequest),
        error => error instanceof Error
            && error.status === 503
            && error.message === "API request failed with status 503"
    );
});

test("saveRecipeToApi rejects the request when the access token is missing", async () => {
    await assert.rejects(
        () => saveRecipeToApi(recipe, null),
        error => error instanceof Error
            && error.status === 401
            && error.message === "Authentication is required."
    );
});
