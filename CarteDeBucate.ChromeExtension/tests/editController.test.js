import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import { initializeEditController } from "../controllers/editController.js";
import { createBrowserEffectsMock } from "./helpers/chromeMock.js";
import { createDomFromFile } from "./helpers/createDom.js";

const editHtmlPath = new URL("../edit.html", import.meta.url);
const editScriptPath = new URL("../edit.js", import.meta.url);

const validRecipe = {
    name: "Soup",
    sourceUrl: "https://recipes.example.test/soup",
    author: "Ana",
    imageUrl: "https://images.example.test/soup.jpg",
    ingredients: ["Water", "Salt"],
    steps: ["Boil water.", "Serve."]
};

async function setupEditor({
    recipePayload = JSON.stringify(validRecipe),
    payloadReadError = null,
    saveRecipe,
    navigateToPrint,
    closeWindow,
    reportError
} = {}) {
    const context = await createDomFromFile(editHtmlPath);
    const browser = createBrowserEffectsMock();
    const calls = { savedRecipes: [], errors: [] };

    initializeEditController({
        document: context.document,
        recipePayload,
        payloadReadError,
        saveRecipe: saveRecipe ?? (recipe => calls.savedRecipes.push(recipe)),
        navigateToPrint: navigateToPrint ?? (() => browser.effects.navigate("print.html")),
        closeWindow: closeWindow ?? browser.effects.closeWindow,
        reportError: reportError ?? (error => calls.errors.push(error))
    });

    return { ...context, browser, calls };
}

function submit(window, form) {
    form.dispatchEvent(new window.Event("submit", {
        bubbles: true,
        cancelable: true
    }));
}

test("initializes the real editor page and populates fields and rows", async () => {
    const context = await setupEditor();

    try {
        assert.equal(context.document.getElementById("recipeName").value, "Soup");
        assert.equal(
            context.document.getElementById("sourceUrl").value,
            "https://recipes.example.test/soup"
        );
        assert.equal(
            context.document.getElementById("imageUrl").value,
            "https://images.example.test/soup.jpg"
        );
        assert.deepEqual(
            [...context.document.querySelectorAll(".ingredient-input")].map(input => input.value),
            ["Water", "Salt"]
        );
        assert.deepEqual(
            [...context.document.querySelectorAll(".instruction-input")].map(input => input.value),
            ["Boil water.", "Serve."]
        );
        assert.equal(context.document.getElementById("errorMessage").hidden, true);
    } finally {
        context.cleanup();
    }
});

test("disables editing for absent, invalid, or unreadable payloads", async t => {
    const scenarios = [
        { name: "absent", recipePayload: null },
        { name: "invalid JSON", recipePayload: "{invalid" },
        { name: "primitive", recipePayload: "42" },
        { name: "array", recipePayload: "[]" },
        { name: "storage read error", recipePayload: null, payloadReadError: new Error("storage") }
    ];

    for (const scenario of scenarios) {
        await t.test(scenario.name, async () => {
            const context = await setupEditor(scenario);

            try {
                const form = context.document.getElementById("recipeForm");
                const cancelButton = context.document.getElementById("cancelButton");

                for (const control of form.elements) {
                    if (control !== cancelButton) {
                        assert.equal(control.disabled, true, control.id);
                    }
                }

                assert.equal(cancelButton.disabled, false);
                assert.equal(context.document.getElementById("errorMessage").hidden, false);
                assert.match(
                    context.document.getElementById("errorMessage").textContent,
                    scenario.payloadReadError ? /could not be initialized/ : /No recipe was found/
                );
            } finally {
                context.cleanup();
            }
        });
    }
});

test("adds, focuses, and removes rows while updating Add button states", async () => {
    const context = await setupEditor();

    try {
        const addIngredient = context.document.getElementById("addIngredientButton");
        const addInstruction = context.document.getElementById("addInstructionButton");

        assert.equal(addIngredient.disabled, false);
        assert.equal(addInstruction.disabled, false);

        addIngredient.click();
        const newIngredient = [...context.document.querySelectorAll(".ingredient-input")].at(-1);
        assert.equal(context.document.activeElement, newIngredient);
        assert.equal(addIngredient.disabled, true);

        newIngredient.value = "Pepper";
        newIngredient.dispatchEvent(new context.window.Event("input", { bubbles: true }));
        assert.equal(addIngredient.disabled, false);

        newIngredient.closest(".ingredient-row").querySelector(".remove-button").click();
        assert.equal(context.document.querySelectorAll(".ingredient-input").length, 2);
        assert.equal(addIngredient.disabled, false);

        addInstruction.click();
        const newInstruction = [...context.document.querySelectorAll(".instruction-input")].at(-1);
        assert.equal(context.document.activeElement, newInstruction);
        assert.equal(addInstruction.disabled, true);

        newInstruction.closest(".instruction-row").querySelector(".remove-button").click();
        assert.equal(context.document.querySelectorAll(".instruction-input").length, 2);
        assert.equal(addInstruction.disabled, false);
    } finally {
        context.cleanup();
    }
});

test("shows every validation error and focuses the error summary", async () => {
    const context = await setupEditor();

    try {
        context.document.getElementById("recipeName").value = "";
        context.document.getElementById("sourceUrl").value = "invalid";
        context.document.getElementById("imageUrl").value = "ftp://image.test/a.jpg";

        for (const input of context.document.querySelectorAll(
            ".ingredient-input, .instruction-input"
        )) {
            input.value = "";
        }

        submit(context.window, context.document.getElementById("recipeForm"));

        const errorMessage = context.document.getElementById("errorMessage");
        assert.equal(errorMessage.hidden, false);
        assert.equal(context.document.activeElement, errorMessage);
        assert.deepEqual(
            [...errorMessage.querySelectorAll("li")].map(item => item.textContent),
            [
                "Recipe name is required.",
                "Source URL must be a valid HTTP or HTTPS URL.",
                "Image URL must be a valid HTTP or HTTPS URL.",
                "At least one ingredient is required.",
                "At least one instruction is required."
            ]
        );
        assert.equal(context.calls.savedRecipes.length, 0);
    } finally {
        context.cleanup();
    }
});

test("saves a valid edited payload and requests navigation to print.html", async () => {
    const context = await setupEditor();

    try {
        context.document.getElementById("recipeName").value = " Edited Soup ";
        context.document.querySelector(".ingredient-input").value = " Fresh water ";

        submit(context.window, context.document.getElementById("recipeForm"));

        assert.equal(context.calls.savedRecipes.length, 1);
        assert.equal(context.calls.savedRecipes[0].name, "Edited Soup");
        assert.equal(context.calls.savedRecipes[0].author, "Ana");
        assert.equal(context.calls.savedRecipes[0].ingredients[0], "Fresh water");
        assert.deepEqual(context.browser.calls.navigate, ["print.html"]);
    } finally {
        context.cleanup();
    }
});

test("cancel requests window closing", async () => {
    const context = await setupEditor();

    try {
        context.document.getElementById("cancelButton").click();
        assert.equal(context.browser.calls.close, 1);
    } finally {
        context.cleanup();
    }
});

test("reports save and navigation callback errors without leaving the editor", async t => {
    const failures = [
        {
            name: "save",
            overrides: { saveRecipe: () => { throw new Error("save failed"); } }
        },
        {
            name: "navigation",
            overrides: { navigateToPrint: () => { throw new Error("navigation failed"); } }
        }
    ];

    for (const failure of failures) {
        await t.test(failure.name, async () => {
            const context = await setupEditor(failure.overrides);

            try {
                submit(context.window, context.document.getElementById("recipeForm"));

                assert.equal(context.calls.errors.length, 1);
                assert.match(context.calls.errors[0].message, /failed/);
                assert.match(
                    context.document.getElementById("errorMessage").textContent,
                    /could not be prepared for printing/
                );
            } finally {
                context.cleanup();
            }
        });
    }
});

test("the real edit entrypoint composes storage, navigation, close, and the controller", async () => {
    const [html, script] = await Promise.all([
        readFile(editHtmlPath, "utf8"),
        readFile(editScriptPath, "utf8")
    ]);

    assert.match(html, /<script\s+type="module"\s+src="edit\.js"><\/script>/);
    assert.match(script, /import\s*{\s*initializeEditController\s*}/);
    assert.match(script, /localStorage\.getItem\("recipeToPrint"\)/);
    assert.match(script, /localStorage\.setItem\("recipeToPrint"/);
    assert.match(script, /chrome\.runtime\.getURL\("print\.html"\)/);
    assert.match(script, /window\.close\(\)/);
});
