const extractorFiles = [
    "extractors/jsonLdImporter.js",
    "extractors/htmlImporter.js",
    "extractors/recipeExtractor.js"
];

const API_URL = "https://localhost:7080/api/recipes";

async function saveRecipeToApi(recipe) {
    const response = await fetch(API_URL, {
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

import("./controllers/popupController.js").then(({
    extractRecipeFromActiveTab,
    initializePopupController
}) => {
    initializePopupController({
        document,
        extractRecipe: () => extractRecipeFromActiveTab({
            chrome,
            extractorFiles
        }),
        saveRecipe: recipe => {
            localStorage.setItem("recipeToPrint", JSON.stringify(recipe));
        },
        saveRecipeToApi,
        openWindow: ({ page, type, width, height }) => {
            return chrome.windows.create({
                url: chrome.runtime.getURL(page),
                type,
                width,
                height
            });
        },
        reportError: error => {
            console.error(error);
        }
    });
});