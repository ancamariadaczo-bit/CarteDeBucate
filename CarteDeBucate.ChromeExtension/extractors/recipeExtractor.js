(() => {

    globalThis.RecipeClipper =
        globalThis.RecipeClipper || {};

    globalThis.RecipeClipper.extractRecipe = function () {

        const jsonLdRecipe =
            globalThis.RecipeClipper.tryJsonLd();

        if (isValidRecipe(jsonLdRecipe)) {
            return {
                success: true,
                method: "json-ld",
                recipe: jsonLdRecipe
            };
        }

        const htmlRecipe =
            globalThis.RecipeClipper.tryHtml();

        if (isValidRecipe(htmlRecipe)) {
            return {
                success: true,
                method: "html",
                recipe: htmlRecipe
            };
        }

        return {
            success: false,
            error: "Nu pare să fie o pagină de rețetă."
        };
    };

    function isValidRecipe(recipe) {

        return recipe !== null &&
            typeof recipe === "object";
    }
})();
