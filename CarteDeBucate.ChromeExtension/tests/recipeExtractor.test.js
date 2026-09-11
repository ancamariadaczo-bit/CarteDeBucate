import assert from "node:assert/strict";
import test from "node:test";
import { createDom } from "./helpers/createDom.js";
import { loadClassicScript } from "./helpers/loadClassicScript.js";

const extractorPath = new URL(
    "../extractors/recipeExtractor.js",
    import.meta.url
);

async function createExtractor({ jsonLdResult, htmlResult }) {
    const context = createDom({ runScripts: true });
    const calls = { jsonLd: 0, html: 0 };

    context.window.RecipeClipper = {
        tryJsonLd() {
            calls.jsonLd += 1;
            return jsonLdResult;
        },
        tryHtml() {
            calls.html += 1;
            return htmlResult;
        }
    };

    await loadClassicScript(context.window, extractorPath);

    return { ...context, calls };
}

function toPlain(value) {
    return JSON.parse(JSON.stringify(value));
}

test("prefers JSON-LD, reports its method, and does not call HTML", async () => {
    const recipe = { name: "JSON-LD Recipe" };
    const context = await createExtractor({
        jsonLdResult: recipe,
        htmlResult: { name: "HTML Recipe" }
    });

    try {
        assert.deepEqual(toPlain(context.window.RecipeClipper.extractRecipe()), {
            success: true,
            method: "json-ld",
            recipe
        });
        assert.deepEqual(context.calls, { jsonLd: 1, html: 0 });
    } finally {
        context.cleanup();
    }
});

test("falls back to HTML and reports its method", async () => {
    const recipe = { name: "HTML Recipe" };
    const context = await createExtractor({
        jsonLdResult: null,
        htmlResult: recipe
    });

    try {
        assert.deepEqual(toPlain(context.window.RecipeClipper.extractRecipe()), {
            success: true,
            method: "html",
            recipe
        });
        assert.deepEqual(context.calls, { jsonLd: 1, html: 1 });
    } finally {
        context.cleanup();
    }
});

test("rejects null and primitive extractor results", async t => {
    const invalidValues = [null, undefined, "recipe", 42, true];

    for (const value of invalidValues) {
        await t.test(String(value), async () => {
            const context = await createExtractor({
                jsonLdResult: value,
                htmlResult: value
            });

            try {
                assert.deepEqual(toPlain(context.window.RecipeClipper.extractRecipe()), {
                    success: false,
                    error: "This page does not appear to contain a recipe."
                });
            } finally {
                context.cleanup();
            }
        });
    }
});
