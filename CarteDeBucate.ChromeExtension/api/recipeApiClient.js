import { API_ENDPOINTS } from "../config/apiConfig.js";

export const recipeApiUrl = API_ENDPOINTS.recipes;
export const recipeExistsApiUrl = API_ENDPOINTS.recipeExists;

export async function recipeExistsBySourceUrl(
    sourceUrl,
    accessToken,
    fetchRequest = globalThis.fetch
) {
    if (!accessToken) {
        throw createRecipeApiError(
            "Authentication is required.",
            401
        );
    }

    const normalizedSourceUrl = String(sourceUrl ?? "").trim();
    const query = new URLSearchParams({
        sourceUrl: normalizedSourceUrl
    });
    const response = await fetchRequest(
        `${recipeExistsApiUrl}?${query.toString()}`,
        {
            method: "GET",
            headers: {
                "Authorization": `Bearer ${accessToken}`
            }
        }
    );

    if (response.status === 401) {
        throw createRecipeApiError(
            "Authentication has expired. Sign in again.",
            response.status
        );
    }

    const responseBody = await readResponseBody(response);

    if (!response.ok) {
        throw createRecipeApiError(
            responseBody?.message
            ?? responseBody?.title
            ?? `API request failed with status ${response.status}`,
            response.status
        );
    }

    if (typeof responseBody?.exists !== "boolean") {
        throw createRecipeApiError(
            "The API response did not contain a valid exists value.",
            response.status
        );
    }

    return responseBody.exists;
}

export async function saveRecipeToApi(
    recipe,
    accessToken,
    fetchRequest = globalThis.fetch
) {
    if (!accessToken) {
        throw createRecipeApiError(
            "Authentication is required.",
            401
        );
    }

    const response = await fetchRequest(recipeApiUrl, {
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
    });

    if (response.status === 401) {
        throw createRecipeApiError(
            "Authentication has expired. Sign in again.",
            response.status
        );
    }

    const responseBody = await readResponseBody(response);

    if (!response.ok) {
        throw createRecipeApiError(
            responseBody?.message
            ?? responseBody?.title
            ?? `API request failed with status ${response.status}`,
            response.status
        );
    }

    return responseBody;
}

async function readResponseBody(response) {
    try {
        return await response.json();
    } catch (error) {
        if (!response.ok) {
            return null;
        }

        throw createRecipeApiError(
            "The API response was not valid JSON.",
            response.status,
            error
        );
    }
}

function createRecipeApiError(message, status, cause) {
    const error = cause === undefined
        ? new Error(message)
        : new Error(message, { cause });

    error.status = status;
    return error;
}
