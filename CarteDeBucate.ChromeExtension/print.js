import { initializePrintController } from "./controllers/printController.js";

initializePrintController({
    document,
    recipePayload: localStorage.getItem("recipeToPrint"),
    requestPrint: () => {
        window.print();
    },
    setTimeout: (callback, delay) => window.setTimeout(callback, delay),
    clearTimeout: timeoutId => window.clearTimeout(timeoutId)
});
