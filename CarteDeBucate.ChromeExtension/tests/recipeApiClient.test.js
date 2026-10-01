import test from "node:test";
import assert from "node:assert/strict";

import {
    recipeExistsApiUrl,
    recipeExistsBySourceUrl,
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

test("recipeExistsBySourceUrl sends an encoded GET request and returns true", async () => {
    const requests = [];
    const accessToken = "test-access-token";
    const sourceUrl = "  https://example.com/recipe?first=one&second=two  ";
    const fetchRequest = async (...request) => {
        requests.push(request);

        return {
            ok: true,
            status: 200,
            json: async () => ({ exists: true })
        };
    };

    const result = await recipeExistsBySourceUrl(
        sourceUrl,
        accessToken,
        fetchRequest
    );

    assert.deepEqual(requests, [[
        `${recipeExistsApiUrl}?sourceUrl=https%3A%2F%2Fexample.com%2Frecipe%3Ffirst%3Done%26second%3Dtwo`,
        {
            method: "GET",
            headers: {
                "Authorization": `Bearer ${accessToken}`
            }
        }
    ]]);
    assert.equal(result, true);
});

test("recipeExistsBySourceUrl returns false when the recipe does not exist", async () => {
    const fetchRequest = async () => ({
        ok: true,
        status: 200,
        json: async () => ({ exists: false })
    });

    const result = await recipeExistsBySourceUrl(
        recipe.sourceUrl,
        "test-access-token",
        fetchRequest
    );

    assert.equal(result, false);
});

test("recipeExistsBySourceUrl rejects the request when the access token is missing", async () => {
    await assert.rejects(
        () => recipeExistsBySourceUrl(recipe.sourceUrl, null),
        error => error instanceof Error
            && error.status === 401
            && error.message === "Authentication is required."
    );
});

test("recipeExistsBySourceUrl preserves an unauthorized status without parsing the body", async () => {
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
        () => recipeExistsBySourceUrl(
            recipe.sourceUrl,
            "expired-access-token",
            fetchRequest
        ),
        error => error instanceof Error
            && error.status === 401
            && error.message === "Authentication has expired. Sign in again."
    );
    assert.equal(jsonReadCount, 0);
});

test("recipeExistsBySourceUrl uses the API message when the request fails", async () => {
    const fetchRequest = async () => ({
        ok: false,
        status: 400,
        json: async () => ({
            message: "The source URL is invalid."
        })
    });

    await assert.rejects(
        () => recipeExistsBySourceUrl(
            recipe.sourceUrl,
            "test-access-token",
            fetchRequest
        ),
        error => error instanceof Error
            && error.status === 400
            && error.message === "The source URL is invalid."
    );
});

test("recipeExistsBySourceUrl uses the HTTP status when an error body is not JSON", async () => {
    const fetchRequest = async () => ({
        ok: false,
        status: 503,
        json: async () => {
            throw new SyntaxError("Unexpected token '<'.");
        }
    });

    await assert.rejects(
        () => recipeExistsBySourceUrl(
            recipe.sourceUrl,
            "test-access-token",
            fetchRequest
        ),
        error => error instanceof Error
            && error.status === 503
            && error.message === "API request failed with status 503"
    );
});

test("recipeExistsBySourceUrl rejects a successful response without exists", async () => {
    const fetchRequest = async () => ({
        ok: true,
        status: 200,
        json: async () => ({})
    });

    await assert.rejects(
        () => recipeExistsBySourceUrl(
            recipe.sourceUrl,
            "test-access-token",
            fetchRequest
        ),
        error => error instanceof Error
            && error.status === 200
            && error.message === "The API response did not contain a valid exists value."
    );
});

test("recipeExistsBySourceUrl rejects a non-boolean exists value", async () => {
    const fetchRequest = async () => ({
        ok: true,
        status: 200,
        json: async () => ({ exists: "true" })
    });

    await assert.rejects(
        () => recipeExistsBySourceUrl(
            recipe.sourceUrl,
            "test-access-token",
            fetchRequest
        ),
        error => error instanceof Error
            && error.status === 200
            && error.message === "The API response did not contain a valid exists value."
    );
});

test("recipeExistsBySourceUrl rejects a successful response that is not JSON", async () => {
    const fetchRequest = async () => ({
        ok: true,
        status: 200,
        json: async () => {
            throw new SyntaxError("Unexpected token '<'.");
        }
    });

    await assert.rejects(
        () => recipeExistsBySourceUrl(
            recipe.sourceUrl,
            "test-access-token",
            fetchRequest
        ),
        error => error instanceof Error
            && error.status === 200
            && error.message === "The API response was not valid JSON."
    );
});

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
