const extractorFiles = [
    "extractors/jsonLdImporter.js",
    "extractors/htmlImporter.js",
    "extractors/recipeExtractor.js"
];

import("./controllers/popupController.js").then(async ({
    extractRecipeFromActiveTab,
    initializePopupController
}) => {
    const { saveRecipeToApi } = await import("./api/recipeApiClient.js");

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
