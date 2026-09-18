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

    const { getCurrentUser } = await import("./api/authenticationApiClient.js");

    const { getAccessToken, removeAccessToken } = await import("./auth/authStorage.js");

    const { resolveAuthenticationState } = await import("./auth/authenticationState.js");

    const reportError = error => {
        console.error(error);
    };

    const isAuthenticated = await resolveAuthenticationState({
        getAccessToken,
        getCurrentUser,
        removeAccessToken,
        reportError
    });

    async function login() {
        const response = await chrome.runtime.sendMessage({
            type: "LOGIN"
        });

        if (!response?.success) {
            throw new Error(
                response?.error ?? "Login failed."
            );
        }
    }

    initializePopupController({
        document,
        extractRecipe: () => extractRecipeFromActiveTab({
            chrome,
            extractorFiles
        }),
        saveRecipe: recipe => {
            localStorage.setItem("recipeToPrint", JSON.stringify(recipe));
        },
        saveRecipeToApi: async recipe => {
            const accessToken = await getAccessToken();

            return saveRecipeToApi(
                recipe,
                accessToken
            );
        },
        initialIsAuthenticated: isAuthenticated,
        login,
        openWindow: ({ page, type, width, height }) => {
            return chrome.windows.create({
                url: chrome.runtime.getURL(page),
                type,
                width,
                height
            });
        },
        reportError
    });
});
