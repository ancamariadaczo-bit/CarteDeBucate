(() => {

    globalThis.RecipeClipper =
        globalThis.RecipeClipper || {};

    globalThis.RecipeClipper.tryJsonLd = function () {

        const scripts = document.querySelectorAll(
            'script[type="application/ld+json"]'
        );
        let firstIncompleteRecipe = null;

        for (const script of scripts) {

            try {

                const data = JSON.parse(script.textContent);

                const recipeObjects = [];
                collectRecipes(data, recipeObjects);

                for (const recipeObject of recipeObjects) {
                    const recipe = convertRecipe(recipeObject);

                    if (isCompleteRecipe(recipe)) {
                        return recipe;
                    }

                    firstIncompleteRecipe ??= recipe;
                }

            } catch (error) {

                console.log(
                    "Invalid JSON-LD:",
                    error
                );

            }
        }

        return firstIncompleteRecipe;
    };

    function collectRecipes(value, recipes) {

        if (value === null || typeof value !== "object") {
            return;
        }

        if (isRecipe(value)) {
            recipes.push(value);
        }

        if (Array.isArray(value)) {

            for (const item of value) {
                collectRecipes(item, recipes);
            }

            return;
        }

        for (const property of Object.values(value)) {
            collectRecipes(property, recipes);
        }
    }

    function isRecipe(value) {

        const type = value["@type"];

        if (type === "Recipe") {
            return true;
        }

        if (Array.isArray(type)) {
            return type.includes("Recipe");
        }

        return false;
    }

    function convertRecipe(recipe) {

        return {
            name: recipe.name ?? "",
            sourceUrl: window.location.href,
            author: getAuthor(recipe.author),
            imageUrl: getImageUrl(recipe.image),
            ingredients: getIngredients(recipe.recipeIngredient),
            steps: getSteps(recipe.recipeInstructions)
        };
    }

    function isCompleteRecipe(recipe) {

        return isNonEmptyText(recipe.name) &&
            isNonEmptyTextArray(recipe.ingredients) &&
            isNonEmptyTextArray(recipe.steps);
    }

    function isNonEmptyText(value) {

        return typeof value === "string" &&
            value.trim().length > 0;
    }

    function isNonEmptyTextArray(values) {

        return Array.isArray(values) &&
            values.length > 0 &&
            values.every(isNonEmptyText);
    }

    function getIngredients(value) {

        if (!Array.isArray(value)) {
            return [];
        }

        return value
            .filter(item => typeof item === "string")
            .map(item => item.trim())
            .filter(item => item.length > 0);
    }

    function getAuthor(author) {

        if (!author) {
            return null;
        }

        if (typeof author === "string") {
            return author;
        }

        if (Array.isArray(author)) {

            const names = author
                .map(item => getAuthor(item))
                .filter(name => name !== null);

            return names.join(", ");
        }

        if (typeof author === "object") {
            return author.name ?? null;
        }

        return null;
    }

    function getImageUrl(image) {

        if (!image) {
            return null;
        }

        if (typeof image === "string") {
            return normalizeImageUrl(image);
        }

        if (Array.isArray(image)) {

            for (const item of image) {

                const imageUrl = getImageUrl(item);

                if (imageUrl) {
                    return imageUrl;
                }
            }

            return null;
        }

        if (typeof image === "object") {
            return getImageUrl(image.url) ??
                getImageUrl(image.contentUrl);
        }

        return null;
    }

    function normalizeImageUrl(value) {

        if (typeof value !== "string" || !value.trim()) {
            return null;
        }

        try {

            const url = new URL(value.trim(), document.baseURI);

            if (url.protocol !== "http:" && url.protocol !== "https:") {
                return null;
            }

            return url.href;

        } catch {
            return null;
        }
    }

    function getSteps(instructions) {

        const steps = [];

        collectSteps(instructions, steps);

        return steps;
    }

    function collectSteps(value, steps) {

        if (!value) {
            return;
        }

        if (typeof value === "string") {

            const text = value.trim();

            if (text.length > 0) {
                steps.push(text);
            }

            return;
        }

        if (Array.isArray(value)) {

            for (const item of value) {
                collectSteps(item, steps);
            }

            return;
        }

        if (typeof value === "object") {

            if (typeof value.text === "string") {

                const text = value.text.trim();

                if (text.length > 0) {
                    steps.push(text);
                }
            }

            if (value.itemListElement) {
                collectSteps(value.itemListElement, steps);
            }
        }
    }
})();
