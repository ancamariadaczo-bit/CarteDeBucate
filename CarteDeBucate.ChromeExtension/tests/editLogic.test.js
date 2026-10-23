import assert from "node:assert/strict";
import test from "node:test";
import {
    isValidHttpUrl,
    normalizeInstruction,
    normalizeInstructions,
    normalizeNonEmptyValues,
    parseRecipePayload,
    validateAndBuildEditedRecipe
} from "../editLogic.js";

test("accepts absolute HTTP and HTTPS URLs, including exterior whitespace", () => {
    assert.equal(isValidHttpUrl("http://example.test/recipe"), true);
    assert.equal(isValidHttpUrl("https://example.test/recipe?q=1"), true);
    assert.equal(isValidHttpUrl("  https://example.test/recipe  "), true);
});

test("rejects empty, relative, protocol-less, host-less, and non-HTTP URLs", () => {
    const invalidUrls = [
        "",
        "/recipe",
        "example.test/recipe",
        "https://",
        "file:///tmp/recipe",
        "mailto:chef@example.test",
        null
    ];

    for (const value of invalidUrls) {
        assert.equal(isValidHttpUrl(value), false, String(value));
    }
});

test("normalizes ingredients without changing their order", () => {
    const values = ["  Flour ", "", "   ", 12, "Milk", "Salt  "];

    assert.deepEqual(normalizeNonEmptyValues(values), ["Flour", "Milk", "Salt"]);
    assert.deepEqual(values, ["  Flour ", "", "   ", 12, "Milk", "Salt  "]);
    assert.deepEqual(normalizeNonEmptyValues(null), []);
});

test("preserves meaningful Enters, removes empty lines, and normalizes CRLF", () => {
    assert.equal(
        normalizeInstruction("  First line\r\n   \r\nSecond line  \r\n"),
        "First line\nSecond line"
    );
    assert.equal(normalizeInstruction("   \n \r\n"), "");
    assert.equal(normalizeInstruction(null), "");
});

test("normalizes instruction arrays and removes empty instructions", () => {
    const values = [" First\n\nSecond ", "   ", 42, "Third"];

    assert.deepEqual(normalizeInstructions(values), ["First\nSecond", "Third"]);
    assert.deepEqual(values, [" First\n\nSecond ", "   ", 42, "Third"]);
    assert.deepEqual(normalizeInstructions(null), []);
});

test("parses only valid object payloads", () => {
    assert.equal(parseRecipePayload(), null);
    assert.equal(parseRecipePayload(""), null);
    assert.equal(parseRecipePayload("{invalid"), null);
    assert.equal(parseRecipePayload("null"), null);
    assert.equal(parseRecipePayload("42"), null);
    assert.equal(parseRecipePayload("[]"), null);
    assert.deepEqual(parseRecipePayload('{"name":"Soup"}'), { name: "Soup" });
});

test("requires Source URL and converts an empty optional Image URL to null", () => {
    const missingSource = validateAndBuildEditedRecipe({}, {
        name: "Soup",
        sourceUrl: " ",
        imageUrl: " ",
        ingredients: ["Water"],
        steps: ["Boil."]
    });

    assert.deepEqual(missingSource, {
        success: false,
        errors: ["Source URL is required."]
    });

    const valid = validateAndBuildEditedRecipe({}, {
        name: "Soup",
        sourceUrl: " https://example.test/soup ",
        imageUrl: " ",
        ingredients: ["Water"],
        steps: ["Boil."]
    });

    assert.equal(valid.recipe.imageUrl, null);
    assert.equal(valid.recipe.sourceUrl, "https://example.test/soup");
});

test("returns all existing validation messages together", () => {
    assert.deepEqual(validateAndBuildEditedRecipe({}, {
        name: " ",
        sourceUrl: "ftp://example.test/recipe",
        imageUrl: "data:image/png;base64,abc",
        ingredients: [" ", 10],
        steps: ["\n  "]
    }), {
        success: false,
        errors: [
            "Recipe name is required.",
            "Source URL must be a valid HTTP or HTTPS URL.",
            "Image URL must be a valid HTTP or HTTPS URL.",
            "At least one ingredient is required.",
            "At least one instruction is required."
        ]
    });
});

test("preserves author and extra properties without modifying input data", () => {
    const originalRecipe = {
        name: "Old name",
        sourceUrl: "https://example.test/old",
        author: "Ana",
        category: "Dinner",
        ingredients: ["Old ingredient"],
        steps: ["Old step"]
    };
    const formValues = {
        name: " New name ",
        sourceUrl: " https://example.test/new ",
        imageUrl: " https://images.example.test/new.jpg ",
        ingredients: [" First ", "Second"],
        steps: [" Line one\r\n\r\nLine two "]
    };
    const originalSnapshot = structuredClone(originalRecipe);
    const formSnapshot = structuredClone(formValues);

    const result = validateAndBuildEditedRecipe(originalRecipe, formValues);

    assert.deepEqual(result, {
        success: true,
        errors: [],
        recipe: {
            name: "New name",
            sourceUrl: "https://example.test/new",
            author: "Ana",
            category: "Dinner",
            imageUrl: "https://images.example.test/new.jpg",
            ingredients: ["First", "Second"],
            steps: ["Line one\nLine two"]
        }
    });
    assert.deepEqual(originalRecipe, originalSnapshot);
    assert.deepEqual(formValues, formSnapshot);
    assert.notEqual(result.recipe, originalRecipe);
});
