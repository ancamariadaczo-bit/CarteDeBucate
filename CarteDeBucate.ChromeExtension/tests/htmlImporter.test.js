import assert from "node:assert/strict";
import test from "node:test";
import { createDom, createDomFromFile } from "./helpers/createDom.js";
import { loadClassicScript } from "./helpers/loadClassicScript.js";

const importerPath = new URL(
    "../extractors/htmlImporter.js",
    import.meta.url
);

async function extractFromHtml(html, url = "https://recipes.example.test/html") {
    const context = createDom({ html, url, runScripts: true });
    await loadClassicScript(context.window, importerPath);

    return {
        result: toPlain(context.window.RecipeClipper.tryHtml()),
        ...context
    };
}

async function extractFromFixture(name, url) {
    const context = await createDomFromFile(
        new URL(`./fixtures/html/${name}`, import.meta.url),
        { url, runScripts: true }
    );
    await loadClassicScript(context.window, importerPath);

    return {
        result: toPlain(context.window.RecipeClipper.tryHtml()),
        ...context
    };
}

function toPlain(value) {
    return value === null
        ? null
        : JSON.parse(JSON.stringify(value));
}

test("extracts structured fields, cleans whitespace, and removes duplicates", async () => {
    const context = await extractFromFixture(
        "structured.html",
        "https://recipes.example.test/structured"
    );

    try {
        assert.deepEqual(context.result, {
            name: "Structured Soup",
            sourceUrl: "https://recipes.example.test/structured",
            author: "Ana Example",
            imageUrl: "https://images.example.test/structured.jpg",
            ingredients: ["Water", "Salt"],
            steps: ["Boil water.", "Serve."]
        });
    } finally {
        context.cleanup();
    }
});

test("supports every existing family of structured ingredient and instruction selectors", async t => {
    const scenarios = [
        {
            name: "schema itemprop",
            ingredients: '<span itemprop="recipeIngredient">Flour</span>',
            steps: '<span itemprop="recipeInstructions">Mix.</span>'
        },
        {
            name: "EasyRecipe",
            ingredients: '<div class="ERSIngredients"><span class="ingredient">Flour</span></div>',
            steps: '<div class="ERSInstructions"><span class="instruction">Mix.</span></div>'
        },
        {
            name: "WP Recipe Maker",
            ingredients: '<span class="wprm-recipe-ingredient">Flour</span>',
            steps: '<span class="wprm-recipe-instruction-text">Mix.</span>'
        },
        {
            name: "Tasty Recipes",
            ingredients: '<div class="tasty-recipes-ingredients"><li>Flour</li></div>',
            steps: '<div class="tasty-recipes-instructions"><li>Mix.</li></div>'
        },
        {
            name: "Mediavine Create",
            ingredients: '<div class="mv-create-ingredients"><li>Flour</li></div>',
            steps: '<div class="mv-create-instructions"><li>Mix.</li></div>'
        },
        {
            name: "generic recipe classes",
            ingredients: '<div class="recipe-ingredients"><li>Flour</li></div>',
            steps: '<div class="recipe-instructions"><li>Mix.</li></div>'
        },
        {
            name: "generic ingredients and directions",
            ingredients: '<div class="ingredients"><li>Flour</li></div>',
            steps: '<div class="directions"><li>Mix.</li></div>'
        }
    ];

    for (const scenario of scenarios) {
        await t.test(scenario.name, async () => {
            const context = await extractFromHtml(`
                <!DOCTYPE html><html><body>
                    <h1>Recipe</h1>
                    ${scenario.ingredients}
                    ${scenario.steps}
                </body></html>
            `);

            try {
                assert.deepEqual(context.result.ingredients, ["Flour"]);
                assert.deepEqual(context.result.steps, ["Mix."]);
            } finally {
                context.cleanup();
            }
        });
    }
});

test("uses Romanian headings, list ingredients, paragraph ingredients, and numbered paragraphs", async () => {
    const context = await extractFromFixture("article-romanian.html");

    try {
        assert.deepEqual(context.result.ingredients, ["Ceapă", "Ardei", "Condimente:"]);
        assert.deepEqual(context.result.steps, ["1. Taie legumele.", "2) Fierbe încet."]);
    } finally {
        context.cleanup();
    }
});

test("uses English headings and ordered lists", async () => {
    const context = await extractFromFixture("article-english.html");

    try {
        assert.deepEqual(context.result.ingredients, ["Carrot", "Potato"]);
        assert.deepEqual(context.result.steps, ["Heat the oven.", "Roast."]);
    } finally {
        context.cleanup();
    }
});

test("falls back through entry, post, article-content, and article containers", async t => {
    const containers = [
        '<div class="entry-content">CONTENT</div>',
        '<div class="post-content">CONTENT</div>',
        '<div class="article-content">CONTENT</div>',
        "<article>CONTENT</article>"
    ];
    const content = `
        <h1>Fallback Recipe</h1>
        <h2>Ingredients</h2><ul><li>Water</li></ul>
        <h2>Instructions</h2><ol><li>Boil.</li></ol>
    `;

    for (const template of containers) {
        await t.test(template.match(/entry|post|article-content|article/)[0], async () => {
            const context = await extractFromHtml(template.replace("CONTENT", content));

            try {
                assert.equal(context.result.name, "Fallback Recipe");
                assert.deepEqual(context.result.ingredients, ["Water"]);
                assert.deepEqual(context.result.steps, ["Boil."]);
            } finally {
                context.cleanup();
            }
        });
    }
});

test("prefers author metadata before visible author selectors", async () => {
    const context = await extractFromHtml(`
        <meta name="author" content="Meta Author">
        <h1>Recipe</h1>
        <span class="author">Visible Author</span>
        <span itemprop="recipeIngredient">Water</span>
        <span itemprop="recipeInstructions">Boil.</span>
    `);

    try {
        assert.equal(context.result.author, "Meta Author");
    } finally {
        context.cleanup();
    }
});

test("supports each visible author selector", async t => {
    const authors = [
        '<span itemprop="author"><span itemprop="name">Schema Author</span></span>',
        '<span itemprop="author">Itemprop Author</span>',
        '<a rel="author">Rel Author</a>',
        '<span class="author">Class Author</span>'
    ];

    for (const markup of authors) {
        await t.test(markup, async () => {
            const context = await extractFromHtml(`
                <h1>Recipe</h1>${markup}
                <span itemprop="recipeIngredient">Water</span>
                <span itemprop="recipeInstructions">Boil.</span>
            `);

            try {
                assert.match(context.result.author, /Author/);
            } finally {
                context.cleanup();
            }
        });
    }
});

test("uses OG, Twitter, itemprop, then article image priority", async t => {
    const scenarios = [
        {
            name: "OG",
            images: '<meta property="og:image" content="https://img.test/og.jpg"><meta name="twitter:image" content="https://img.test/twitter.jpg"><img itemprop="image" src="https://img.test/item.jpg"><article><img src="https://img.test/article.jpg"></article>',
            expected: "https://img.test/og.jpg"
        },
        {
            name: "Twitter",
            images: '<meta name="twitter:image" content="https://img.test/twitter.jpg"><img itemprop="image" src="https://img.test/item.jpg"><article><img src="https://img.test/article.jpg"></article>',
            expected: "https://img.test/twitter.jpg"
        },
        {
            name: "itemprop",
            images: '<img itemprop="image" src="https://img.test/item.jpg"><article><img src="https://img.test/article.jpg"></article>',
            expected: "https://img.test/item.jpg"
        },
        {
            name: "article",
            images: '<article><img src="https://img.test/article.jpg"></article>',
            expected: "https://img.test/article.jpg"
        }
    ];

    for (const scenario of scenarios) {
        await t.test(scenario.name, async () => {
            const context = await extractFromHtml(`
                <h1>Recipe</h1>${scenario.images}
                <span itemprop="recipeIngredient">Water</span>
                <span itemprop="recipeInstructions">Boil.</span>
            `);

            try {
                assert.equal(context.result.imageUrl, scenario.expected);
            } finally {
                context.cleanup();
            }
        });
    }
});

test("returns null when any required recipe data is missing", async t => {
    const scenarios = [
        '<span itemprop="recipeIngredient">Water</span><span itemprop="recipeInstructions">Boil.</span>',
        '<h1>Recipe</h1><span itemprop="recipeInstructions">Boil.</span>',
        '<h1>Recipe</h1><span itemprop="recipeIngredient">Water</span>'
    ];

    for (const html of scenarios) {
        await t.test(html, async () => {
            const context = await extractFromHtml(html);

            try {
                assert.equal(context.result, null);
            } finally {
                context.cleanup();
            }
        });
    }
});

test("uses the document URL as sourceUrl", async () => {
    const context = await extractFromHtml(`
        <h1>Recipe</h1>
        <span itemprop="recipeIngredient">Water</span>
        <span itemprop="recipeInstructions">Boil.</span>
    `, "https://recipes.example.test/original?portion=2");

    try {
        assert.equal(
            context.result.sourceUrl,
            "https://recipes.example.test/original?portion=2"
        );
    } finally {
        context.cleanup();
    }
});
