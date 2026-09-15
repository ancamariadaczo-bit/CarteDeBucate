//export const recipeApiUrl = "https://localhost:7080/api/recipes";

import { API_ENDPOINTS } from "../config/apiConfig.js";

export const recipeApiUrl = API_ENDPOINTS.recipes;

export async function saveRecipeToApi(
    recipe,
    fetchRequest = globalThis.fetch
) {
    const response = await fetchRequest(recipeApiUrl, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            name: recipe.name,
            sourceUrl: recipe.sourceUrl,
            ingredients: recipe.ingredients,
            steps: recipe.steps
        })
    });

    const responseBody = await response.json();

    if (!response.ok) {
        throw new Error(
            responseBody.message
            ?? responseBody.title
            ?? `API request failed with status ${response.status}`
        );
    }

    return responseBody;
}
