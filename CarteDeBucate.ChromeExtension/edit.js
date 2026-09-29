import { saveRecipeToApi } from "./api/recipeApiClient.js";
import { getCurrentUser } from "./api/authenticationApiClient.js";
import { getAccessToken, removeAccessToken } from "./auth/authStorage.js";
import { resolveAuthenticationState } from "./auth/authenticationState.js";
import { initializeEditController } from "./controllers/editController.js";

let recipePayload = null;
let payloadReadError = null;

const reportError = error => {
    console.error(error);
};

try {
    recipePayload = localStorage.getItem("recipeToPrint");
} catch (error) {
    console.error(error);
    payloadReadError = error;
}

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

const editorController = initializeEditController({
    document,
    recipePayload,
    payloadReadError,
    saveRecipe: recipe => {
        localStorage.setItem("recipeToPrint", JSON.stringify(recipe));
    },
    navigateToPrint: () => {
        window.location.href = chrome.runtime.getURL("print.html");
    },
    saveRecipeToApi: async recipe => {
        const accessToken = await getAccessToken();

        return saveRecipeToApi(
            recipe,
            accessToken
        );
    },
    removeAccessToken,
    login,
    initialIsAuthenticationResolved: false,
    closeWindow: () => {
        window.close();
    },
    reportError
});

resolveAuthenticationState({
    getAccessToken,
    getCurrentUser,
    removeAccessToken,
    reportError
}).then(isAuthenticated => {
    editorController.setAuthenticationState(isAuthenticated);
});
