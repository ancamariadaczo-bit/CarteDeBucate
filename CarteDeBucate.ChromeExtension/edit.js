import { initializeEditController } from "./controllers/editController.js";

let recipePayload = null;
let payloadReadError = null;

try {
    recipePayload = localStorage.getItem("recipeToPrint");
} catch (error) {
    console.error(error);
    payloadReadError = error;
}

initializeEditController({
    document,
    recipePayload,
    payloadReadError,
    saveRecipe: recipe => {
        localStorage.setItem("recipeToPrint", JSON.stringify(recipe));
    },
    navigateToPrint: () => {
        window.location.href = chrome.runtime.getURL("print.html");
    },
    closeWindow: () => {
        window.close();
    },
    reportError: error => {
        console.error(error);
    }
});
