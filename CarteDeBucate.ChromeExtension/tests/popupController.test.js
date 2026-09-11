import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import {
    extractRecipeFromActiveTab,
    initializePopupController
} from "../controllers/popupController.js";
import {
    createBrowserEffectsMock,
    createChromeMock,
    createStorageMock
} from "./helpers/chromeMock.js";
import { createDomFromFile } from "./helpers/createDom.js";

const popupHtmlPath = new URL("../popup.html", import.meta.url);
const popupScriptPath = new URL("../popup.js", import.meta.url);

const recipe = {
    name: "Soup",
    sourceUrl: "https://recipes.example.test/soup",
    author: "Ana",
    imageUrl: "https://images.example.test/soup.jpg",
    ingredients: ["Water", "Salt"],
    steps: ["Boil.", "Serve."]
};

async function setupPopup(extractRecipe, reportError = () => {}) {
    const context = await createDomFromFile(popupHtmlPath);
    const storage = createStorageMock();
    const browser = createBrowserEffectsMock();

    initializePopupController({
        document: context.document,
        extractRecipe,
        saveRecipe: value => storage.storage.setItem("recipeToPrint", JSON.stringify(value)),
        openWindow: browser.effects.openWindow,
        reportError
    });

    return { ...context, storage, browser };
}

async function clickAndFlush(element) {
    element.click();
    await Promise.resolve();
    await Promise.resolve();
}

test("starts with only the Extract action visible", async () => {
    const context = await setupPopup(async () => ({ success: false, error: "unused" }));

    try {
        assert.equal(context.document.getElementById("extractButton").hidden, false);
        assert.equal(context.document.getElementById("editButton").hidden, true);
        assert.equal(context.document.getElementById("printButton").hidden, true);
        assert.equal(context.document.getElementById("resultSeparator").hidden, true);
        assert.equal(context.document.getElementById("result").textContent, "");
    } finally {
        context.cleanup();
    }
});

test("renders a successful extraction with title, links, image, ingredients, and steps", async () => {
    const context = await setupPopup(async () => ({ success: true, recipe }));

    try {
        await clickAndFlush(context.document.getElementById("extractButton"));

        const result = context.document.getElementById("result");
        assert.equal(result.querySelector("h3").textContent, "Soup");
        assert.deepEqual(
            [...result.querySelectorAll("a")].map(link => link.href),
            [recipe.sourceUrl, recipe.imageUrl]
        );
        assert.equal(result.querySelector("img").src, recipe.imageUrl);
        assert.deepEqual(
            [...result.querySelectorAll("ul li")].map(item => item.textContent),
            recipe.ingredients
        );
        assert.deepEqual(
            [...result.querySelectorAll("ol li")].map(item => item.textContent),
            recipe.steps
        );
        assert.equal(context.document.getElementById("extractButton").hidden, true);
        assert.equal(context.document.getElementById("editButton").hidden, false);
        assert.equal(context.document.getElementById("printButton").hidden, false);
    } finally {
        context.cleanup();
    }
});

test("treats extracted content as text instead of executable HTML", async () => {
    const unsafeRecipe = {
        ...recipe,
        name: '<img id="unsafe-title" src=x>',
        ingredients: ['<script id="unsafe-ingredient">run()</script>'],
        steps: ['<b id="unsafe-step">bold</b>']
    };
    const context = await setupPopup(async () => ({ success: true, recipe: unsafeRecipe }));

    try {
        await clickAndFlush(context.document.getElementById("extractButton"));
        const result = context.document.getElementById("result");

        assert.equal(result.querySelector("#unsafe-title"), null);
        assert.equal(result.querySelector("#unsafe-ingredient"), null);
        assert.equal(result.querySelector("#unsafe-step"), null);
        assert.match(result.textContent, /<img id="unsafe-title"/);
    } finally {
        context.cleanup();
    }
});

test("removes an image that fails and displays the existing message", async () => {
    const context = await setupPopup(async () => ({ success: true, recipe }));

    try {
        await clickAndFlush(context.document.getElementById("extractButton"));
        const image = context.document.querySelector("#result img");
        image.dispatchEvent(new context.window.Event("error"));

        assert.equal(context.document.querySelector("#result img"), null);
        assert.match(
            context.document.getElementById("result").textContent,
            /The image cannot be displayed directly\./
        );
    } finally {
        context.cleanup();
    }
});

test("shows extraction failure and clears the previously extracted recipe", async () => {
    const results = [
        { success: true, recipe },
        { success: false, error: "No recipe here." }
    ];
    const context = await setupPopup(async () => results.shift());

    try {
        await clickAndFlush(context.document.getElementById("extractButton"));
        await clickAndFlush(context.document.getElementById("extractButton"));
        await clickAndFlush(context.document.getElementById("editButton"));

        assert.equal(context.document.getElementById("result").textContent, "No recipe here.");
        assert.equal(context.document.getElementById("extractButton").hidden, false);
        assert.equal(context.document.getElementById("editButton").hidden, true);
        assert.equal(context.document.getElementById("printButton").hidden, true);
        assert.equal(context.storage.calls.setItem.length, 0);
        assert.equal(context.browser.calls.openWindow.length, 0);
    } finally {
        context.cleanup();
    }
});

test("reports a rejected extraction and restores the failure state", async () => {
    const extractionError = new Error("Cannot inject into this tab.");
    const reportedErrors = [];
    const context = await setupPopup(
        async () => { throw extractionError; },
        error => reportedErrors.push(error)
    );

    try {
        await clickAndFlush(context.document.getElementById("extractButton"));

        assert.deepEqual(reportedErrors, [extractionError]);
        assert.equal(
            context.document.getElementById("result").textContent,
            "The recipe could not be extracted from this page."
        );
        assert.equal(context.document.getElementById("extractButton").hidden, false);
        assert.equal(context.document.getElementById("editButton").hidden, true);
        assert.equal(context.document.getElementById("printButton").hidden, true);
        assert.equal(context.document.getElementById("resultSeparator").hidden, true);
        assert.equal(context.storage.calls.setItem.length, 0);
        assert.equal(context.browser.calls.openWindow.length, 0);
    } finally {
        context.cleanup();
    }
});

test("Edit and Print do nothing before a recipe is extracted", async () => {
    const context = await setupPopup(async () => ({ success: false, error: "unused" }));

    try {
        await clickAndFlush(context.document.getElementById("editButton"));
        await clickAndFlush(context.document.getElementById("printButton"));

        assert.equal(context.storage.calls.setItem.length, 0);
        assert.equal(context.browser.calls.openWindow.length, 0);
    } finally {
        context.cleanup();
    }
});

test("Edit and Print save the payload and open 900x700 popup windows", async () => {
    const context = await setupPopup(async () => ({ success: true, recipe }));

    try {
        await clickAndFlush(context.document.getElementById("extractButton"));
        await clickAndFlush(context.document.getElementById("editButton"));
        await clickAndFlush(context.document.getElementById("printButton"));

        assert.deepEqual(
            context.storage.calls.setItem,
            [
                ["recipeToPrint", JSON.stringify(recipe)],
                ["recipeToPrint", JSON.stringify(recipe)]
            ]
        );
        assert.deepEqual(context.browser.calls.openWindow, [
            { page: "edit.html", type: "popup", width: 900, height: 700 },
            { page: "print.html", type: "popup", width: 900, height: 700 }
        ]);
    } finally {
        context.cleanup();
    }
});

test("extracts from the active tab using the production Chrome API sequence", async () => {
    const extractionResult = { success: true, recipe };
    const extractorFiles = [
        "extractors/jsonLdImporter.js",
        "extractors/htmlImporter.js",
        "extractors/recipeExtractor.js"
    ];
    const chromeMock = createChromeMock({
        tabs: [{ id: 321 }],
        executeScriptResults: [[], [{ result: extractionResult }]]
    });

    const reportedExtractions = [];

    const result = await extractRecipeFromActiveTab({
        chrome: chromeMock.chrome,
        extractorFiles,
        reportExtraction: value => reportedExtractions.push(value)
    });

    assert.deepEqual(result, extractionResult);
    assert.deepEqual(reportedExtractions, [extractionResult]);
    assert.deepEqual(chromeMock.calls.tabsQuery, [{
        active: true,
        currentWindow: true
    }]);
    assert.equal(chromeMock.calls.executeScript.length, 2);
    assert.deepEqual(chromeMock.calls.executeScript[0], {
        target: { tabId: 321 },
        files: extractorFiles
    });
    assert.deepEqual(chromeMock.calls.executeScript[1].target, { tabId: 321 });
    assert.equal(typeof chromeMock.calls.executeScript[1].func, "function");
});

test("the real classic popup bootstrap preserves injection and dependency composition", async () => {
    const [html, script] = await Promise.all([
        readFile(popupHtmlPath, "utf8"),
        readFile(popupScriptPath, "utf8")
    ]);

    assert.match(html, /<script\s+src="popup\.js"><\/script>/);
    assert.doesNotMatch(html, /<script[^>]+type="module"[^>]+src="popup\.js"/);
    assert.match(html, /width:\s*468px/);
    assert.match(script, /import\("\.\/controllers\/popupController\.js"\)\.then/);
    assert.match(script, /extractRecipeFromActiveTab/);
    assert.match(script, /initializePopupController\s*\(/);
    assert.match(script, /localStorage\.setItem\("recipeToPrint"/);
    assert.match(script, /reportError:\s*error\s*=>/);
    assert.match(script, /url:\s*chrome\.runtime\.getURL\(page\)/);
    assert.match(script, /openWindow:\s*\(\{\s*page,\s*type,\s*width,\s*height\s*}\)\s*=>/);
    assert.match(script, /chrome\.windows\.create\(\{[\s\S]*?type,[\s\S]*?width,[\s\S]*?height/);

    const jsonLdIndex = script.indexOf('"extractors/jsonLdImporter.js"');
    const htmlIndex = script.indexOf('"extractors/htmlImporter.js"');
    const recipeIndex = script.indexOf('"extractors/recipeExtractor.js"');

    assert.ok(jsonLdIndex >= 0 && jsonLdIndex < htmlIndex);
    assert.ok(htmlIndex < recipeIndex);
});
