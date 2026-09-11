const extractorFiles = [
    "extractors/jsonLdImporter.js",
    "extractors/htmlImporter.js",
    "extractors/recipeExtractor.js"
];

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
