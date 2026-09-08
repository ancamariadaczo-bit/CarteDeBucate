(() => {

    globalThis.RecipeClipper =
        globalThis.RecipeClipper || {};

    globalThis.RecipeClipper.tryJsonLd = function () {

        const scripts = document.querySelectorAll(
            'script[type="application/ld+json"]'
        );

        for (const script of scripts) {

            try {

                const data = JSON.parse(script.textContent);

                const recipeObject = findRecipe(data);

                if (recipeObject !== null) {
                    return convertRecipe(recipeObject);
                }

            } catch (error) {

                console.log(
                    "Invalid JSON-LD:",
                    error
                );

            }
        }

        return null;
    };

    function findRecipe(value) {

        if (value === null || typeof value !== "object") {
            return null;
        }

        if (isRecipe(value)) {
            return value;
        }

        if (Array.isArray(value)) {

            for (const item of value) {

                const found = findRecipe(item);

                if (found !== null) {
                    return found;
                }
            }

            return null;
        }

        for (const property of Object.values(value)) {

            const found = findRecipe(property);

            if (found !== null) {
                return found;
            }
        }

        return null;
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
            return image;
        }

        if (Array.isArray(image)) {

            if (image.length === 0) {
                return null;
            }

            return getImageUrl(image[0]);
        }

        if (typeof image === "object") {
            return image.url ?? image.contentUrl ?? null;
        }

        return null;
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
