const extractorFiles = [
    "extractors/jsonLdImporter.js",
    "extractors/htmlImporter.js",
    "extractors/recipeExtractor.js"
];

import("./controllers/popupController.js").then(({ initializePopupController }) => {
    initializePopupController({
        document,
        extractRecipe: extractRecipeFromActiveTab,
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
        }
    });
});

async function extractRecipeFromActiveTab() {
    const [tab] = await chrome.tabs.query({
        active: true,
        currentWindow: true
    });

    await chrome.scripting.executeScript({
        target: {
            tabId: tab.id
        },
        files: extractorFiles
    });

    const executionResults = await chrome.scripting.executeScript({
        target: {
            tabId: tab.id
        },
        func: () => globalThis.RecipeClipper.extractRecipe()
    });

    const extractionResult = executionResults[0].result;

    console.log(extractionResult);

    return extractionResult;
}
