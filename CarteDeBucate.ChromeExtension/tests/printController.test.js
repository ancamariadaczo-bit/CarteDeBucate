import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import { initializePrintController } from "../controllers/printController.js";
import { createBrowserEffectsMock, createControlledTimers } from "./helpers/chromeMock.js";
import { createDomFromFile } from "./helpers/createDom.js";

const printHtmlPath = new URL("../print.html", import.meta.url);
const printScriptPath = new URL("../print.js", import.meta.url);

const recipe = {
    name: "Soup",
    sourceUrl: "https://recipes.example.test/soup",
    author: "Ana",
    imageUrl: "https://images.example.test/soup.jpg",
    ingredients: ["Water", "Salt"],
    steps: ["Boil.", "Serve."]
};

async function setupPrint({
    recipePayload = JSON.stringify(recipe),
    imageComplete
} = {}) {
    const context = await createDomFromFile(printHtmlPath);
    const browser = createBrowserEffectsMock();
    const timers = createControlledTimers();

    if (imageComplete !== undefined) {
        Object.defineProperty(
            context.window.HTMLImageElement.prototype,
            "complete",
            { configurable: true, get: () => imageComplete }
        );
    }

    initializePrintController({
        document: context.document,
        recipePayload,
        requestPrint: browser.effects.requestPrint,
        setTimeout: timers.setTimeout,
        clearTimeout: timers.clearTimeout
    });

    return { ...context, browser, timers };
}

async function flushPromises() {
    await Promise.resolve();
    await Promise.resolve();
}

test("shows the existing message and does not print when payload is absent", async () => {
    const context = await setupPrint({ recipePayload: null });

    try {
        assert.equal(
            context.document.getElementById("recipe").textContent,
            "No recipe was found to print."
        );
        assert.equal(context.browser.calls.print, 0);
        assert.equal(context.timers.pending().length, 0);
    } finally {
        context.cleanup();
    }
});

test("rejects corrupted or structurally invalid recipe payloads", async () => {
    const scenarios = [
        { name: "invalid JSON", recipePayload: "{invalid" },
        { name: "null", recipePayload: "null" },
        { name: "primitive", recipePayload: "42" },
        { name: "array", recipePayload: "[]" },
        {
            name: "missing ingredients",
            recipePayload: JSON.stringify({ name: "Soup", steps: ["Boil."] })
        },
        {
            name: "missing steps",
            recipePayload: JSON.stringify({ name: "Soup", ingredients: ["Water"] })
        }
    ];

    for (const scenario of scenarios) {
        const context = await setupPrint({ recipePayload: scenario.recipePayload });

        try {
            assert.equal(
                context.document.getElementById("recipe").textContent,
                "No recipe was found to print.",
                scenario.name
            );
            assert.equal(context.browser.calls.print, 0, scenario.name);
            assert.equal(context.timers.pending().length, 0, scenario.name);
            assert.equal(
                context.document.getElementById("recipeSummary"),
                null,
                scenario.name
            );
            assert.equal(
                context.document.getElementById("stepsSection"),
                null,
                scenario.name
            );
        } finally {
            context.cleanup();
        }
    }
});

test("renders the complete recipe using the real print page", async () => {
    const context = await setupPrint({ imageComplete: false });

    try {
        assert.equal(context.document.title, "Soup");
        assert.equal(context.document.querySelector("#recipeSummary h1").textContent, "Soup");
        assert.equal(
            context.document.querySelector(".recipe-source a").href,
            recipe.sourceUrl
        );
        assert.equal(context.document.querySelector("#imageSection img").src, recipe.imageUrl);
        assert.deepEqual(
            [...context.document.querySelectorAll("#recipeSummary ul li")].map(item => item.textContent),
            recipe.ingredients
        );
        assert.deepEqual(
            [...context.document.querySelectorAll("#stepsSection ol li")].map(item => item.textContent),
            recipe.steps
        );
    } finally {
        context.cleanup();
    }
});

test("renders defensively when source and image are absent", async () => {
    const context = await setupPrint({
        recipePayload: JSON.stringify({
            ...recipe,
            sourceUrl: null,
            imageUrl: null
        })
    });

    try {
        assert.equal(context.document.querySelector(".recipe-source"), null);
        assert.equal(context.document.querySelector("#imageSection img"), null);
        assert.equal(context.browser.calls.print, 1);
    } finally {
        context.cleanup();
    }
});

test("treats recipe content as text instead of executable HTML", async () => {
    const unsafeRecipe = {
        ...recipe,
        name: '<img id="unsafe-title" src=x>',
        imageUrl: null,
        ingredients: ['<script id="unsafe-ingredient">run()</script>'],
        steps: ['<b id="unsafe-step">bold</b>']
    };
    const context = await setupPrint({ recipePayload: JSON.stringify(unsafeRecipe) });

    try {
        assert.equal(context.document.querySelector("#unsafe-title"), null);
        assert.equal(context.document.querySelector("#unsafe-ingredient"), null);
        assert.equal(context.document.querySelector("#unsafe-step"), null);
        assert.match(context.document.getElementById("recipe").textContent, /<img id="unsafe-title"/);
    } finally {
        context.cleanup();
    }
});

test("prints immediately when there are no images", async () => {
    const context = await setupPrint({
        recipePayload: JSON.stringify({ ...recipe, imageUrl: null })
    });

    try {
        assert.equal(context.browser.calls.print, 1);
        assert.deepEqual(context.timers.calls.scheduled, []);
    } finally {
        context.cleanup();
    }
});

test("waits for image load and then applies the 200 ms print delay", async () => {
    const context = await setupPrint({ imageComplete: false });

    try {
        assert.equal(context.browser.calls.print, 0);
        assert.deepEqual(context.timers.pending().map(task => task.delay), [10000]);

        context.document.querySelector("#imageSection img")
            .dispatchEvent(new context.window.Event("load"));
        await flushPromises();

        assert.deepEqual(context.timers.pending().map(task => task.delay), [200]);
        context.timers.runByDelay(200);
        assert.equal(context.browser.calls.print, 1);
    } finally {
        context.cleanup();
    }
});

test("continues after image error and displays the existing failure message", async () => {
    const context = await setupPrint({ imageComplete: false });

    try {
        context.document.querySelector("#imageSection img")
            .dispatchEvent(new context.window.Event("error"));
        await flushPromises();

        assert.equal(context.document.querySelector("#imageSection img"), null);
        assert.match(
            context.document.getElementById("imageSection").textContent,
            /The image cannot be displayed directly\./
        );
        context.timers.runByDelay(200);
        assert.equal(context.browser.calls.print, 1);
    } finally {
        context.cleanup();
    }
});

test("continues after the controlled 10 second timeout without waiting in real time", async () => {
    const context = await setupPrint({ imageComplete: false });

    try {
        assert.equal(context.timers.runByDelay(10000), 1);
        await flushPromises();

        assert.deepEqual(context.timers.pending().map(task => task.delay), [200]);
        context.timers.runByDelay(200);
        assert.equal(context.browser.calls.print, 1);
    } finally {
        context.cleanup();
    }
});

test("prints only once when later image events and cleared timeouts occur", async () => {
    const context = await setupPrint({ imageComplete: false });
    const image = context.document.querySelector("#imageSection img");

    try {
        image.dispatchEvent(new context.window.Event("load"));
        image.dispatchEvent(new context.window.Event("error"));
        await flushPromises();
        context.timers.runAll();

        assert.equal(context.browser.calls.print, 1);
        assert.equal(context.timers.runByDelay(10000), 0);
    } finally {
        context.cleanup();
    }
});

test("the real print entrypoint composes storage, print, and timer effects", async () => {
    const [html, script] = await Promise.all([
        readFile(printHtmlPath, "utf8"),
        readFile(printScriptPath, "utf8")
    ]);

    assert.match(html, /<script\s+type="module"\s+src="print\.js"><\/script>/);
    assert.match(script, /import\s*{\s*initializePrintController\s*}/);
    assert.match(script, /localStorage\.getItem\("recipeToPrint"\)/);
    assert.match(script, /window\.print\(\)/);
    assert.match(script, /window\.setTimeout\(callback, delay\)/);
    assert.match(script, /window\.clearTimeout\(timeoutId\)/);
});
