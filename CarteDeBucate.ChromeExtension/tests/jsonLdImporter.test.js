import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import { createDom } from "./helpers/createDom.js";
import { loadClassicScript } from "./helpers/loadClassicScript.js";

const importerPath = new URL(
    "../extractors/jsonLdImporter.js",
    import.meta.url
);

async function loadFixture(name) {
    return JSON.parse(await readFile(
        new URL(`./fixtures/json-ld/${name}`, import.meta.url),
        "utf8"
    ));
}

async function extractFromValues(values, url = "https://recipes.example.test/json-ld") {
    const scripts = values.map(value => {
        const content = typeof value === "string"
            ? value
            : JSON.stringify(value);

        return `<script type="application/ld+json">${content}</script>`;
    }).join("");

    const context = createDom({
        html: `<!DOCTYPE html><html><body>${scripts}</body></html>`,
        url,
        runScripts: true
    });

    context.window.console.log = () => {};
    await loadClassicScript(context.window, importerPath);

    return {
        result: toPlain(context.window.RecipeClipper.tryJsonLd()),
        cleanup: context.cleanup
    };
}

function toPlain(value) {
    return value === null || value === undefined
        ? value
        : JSON.parse(JSON.stringify(value));
}

test("returns null when the page has no JSON-LD scripts", async () => {
    const { result, cleanup } = await extractFromValues([]);

    try {
        assert.equal(result, null);
    } finally {
        cleanup();
    }
});

test("skips invalid JSON-LD and uses the next valid recipe", async () => {
    const recipe = await loadFixture("root-recipe.json");
    const { result, cleanup } = await extractFromValues(["{invalid", recipe]);

    try {
        assert.equal(result.name, "Simple Soup");
        assert.deepEqual(result.ingredients, ["Water", "Salt"]);
    } finally {
        cleanup();
    }
});

test("extracts a root recipe with a string @type", async () => {
    const recipe = await loadFixture("root-recipe.json");
    const { result, cleanup } = await extractFromValues([recipe]);

    try {
        assert.deepEqual(result, {
            name: "Simple Soup",
            sourceUrl: "https://recipes.example.test/json-ld",
            author: "Ana",
            imageUrl: "https://images.example.test/soup.jpg",
            ingredients: ["Water", "Salt"],
            steps: ["Boil water.", "Add salt."]
        });
    } finally {
        cleanup();
    }
});

test("finds recipes in arrays, nested objects, and @graph with array @type", async t => {
    const baseRecipe = {
        "@type": "Recipe",
        name: "Found",
        recipeIngredient: ["Water"],
        recipeInstructions: ["Mix."]
    };

    const nestedFixture = await loadFixture("nested-recipe.json");
    const scenarios = [
        { name: "array", value: [{ "@type": "Thing" }, baseRecipe] },
        { name: "nested object", value: { data: { recipe: baseRecipe } } },
        { name: "@graph", value: nestedFixture }
    ];

    for (const scenario of scenarios) {
        await t.test(scenario.name, async () => {
            const { result, cleanup } = await extractFromValues([scenario.value]);

            try {
                assert.ok(result);
                assert.match(result.name, /Found|Nested Cake/);
            } finally {
                cleanup();
            }
        });
    }
});

test("normalizes string, object, and array author values", async t => {
    const scenarios = [
        { author: "Ana", expected: "Ana" },
        { author: { name: "Ana" }, expected: "Ana" },
        { author: [{ name: "Ana" }, "Bob"], expected: "Ana, Bob" }
    ];

    for (const scenario of scenarios) {
        await t.test(JSON.stringify(scenario.author), async () => {
            const { result, cleanup } = await extractFromValues([{
                "@type": "Recipe",
                name: "Recipe",
                author: scenario.author
            }]);

            try {
                assert.equal(result.author, scenario.expected);
            } finally {
                cleanup();
            }
        });
    }
});

test("normalizes string, object, and array image values", async t => {
    const expected = "https://images.example.test/recipe.jpg";
    const scenarios = [
        expected,
        { url: expected },
        { contentUrl: expected },
        [{ url: expected }]
    ];

    for (const image of scenarios) {
        await t.test(JSON.stringify(image), async () => {
            const { result, cleanup } = await extractFromValues([{
                "@type": "Recipe",
                name: "Recipe",
                image
            }]);

            try {
                assert.equal(result.imageUrl, expected);
            } finally {
                cleanup();
            }
        });
    }
});

test("trims ingredients, removes empty values, and flattens instruction structures", async () => {
    const recipe = await loadFixture("variant-fields.json");
    const { result, cleanup } = await extractFromValues(
        [recipe],
        "https://recipes.example.test/source?value=1"
    );

    try {
        assert.deepEqual(result.ingredients, ["Flour", "Milk"]);
        assert.deepEqual(result.steps, ["Mix.", "Bake.", "Cool.", "Serve."]);
        assert.equal(result.sourceUrl, "https://recipes.example.test/source?value=1");
    } finally {
        cleanup();
    }
});
