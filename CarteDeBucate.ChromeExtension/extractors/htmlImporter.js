(() => {

    globalThis.RecipeClipper =
        globalThis.RecipeClipper || {};

    globalThis.RecipeClipper.tryHtml = function () {

        const name = getHtmlRecipeName();

        const ingredients = getHtmlIngredients();

        const steps = getHtmlSteps();

        if (
            !name ||
            ingredients.length === 0 ||
            steps.length === 0
        ) {
            return null;
        }

        return {
            name: name,
            sourceUrl: window.location.href,
            author: getHtmlAuthor(),
            imageUrl: getHtmlImage(),
            ingredients: ingredients,
            steps: steps
        };
    };

    function getHtmlRecipeName() {

        const selectors = [
            ".wprm-recipe-name",
            ".tasty-recipes-title",
            ".mv-create-title",

            ".ERSName",

            "[itemprop='name']",
            "article h1",
            "h1"
        ];

        return getFirstText(selectors);
    }

    function getFirstText(selectors) {

        for (const selector of selectors) {

            const element = document.querySelector(selector);

            if (!element) {
                continue;
            }

            const text = cleanText(element.textContent);

            if (text.length > 0) {
                return text;
            }
        }

        return null;
    }

    function cleanText(text) {

        return text
            .replace(/\s+/g, " ")
            .trim();
    }

    function getHtmlIngredients() {

        const selectors = [
            "[itemprop='recipeIngredient']",

            "[itemprop='ingredients']",
            ".ERSIngredients .ingredient",
            ".ERSIngredients li",

            ".wprm-recipe-ingredient",
            ".tasty-recipes-ingredients li",
            ".mv-create-ingredients li",
            ".recipe-ingredients li",
            ".ingredients li"
        ];

        const structuredIngredients = getTexts(selectors);

        if (structuredIngredients.length > 0) {
            return structuredIngredients;
        }

        return getIngredientsFromArticleStructure();
    }

    function getIngredientsFromArticleStructure() {

        const children = getArticleElements();

        if (children.length === 0) {
            return [];
        }

        const ingredientsStartIndex =
            children.findIndex(element =>
                isIngredientsHeading(element)
            );

        if (ingredientsStartIndex === -1) {
            return [];
        }

        const preparationStartIndex = children.findIndex(
            (element, index) => {

                if (index <= ingredientsStartIndex) {
                    return false;
                }

                return isPreparationHeading(element);
            }
        );

        if (preparationStartIndex === -1) {
            return [];
        }

        const ingredients = [];

        for (
            let i = ingredientsStartIndex + 1;
            i < preparationStartIndex;
            i++
        ) {

            const element = children[i];

            if (element.matches("ul, ol")) {

                const items = element.querySelectorAll("li");

                for (const item of items) {

                    const text = cleanText(item.textContent);

                    if (text.length > 0) {
                        ingredients.push(text);
                    }
                }

                continue;
            }

            if (element.matches("p")) {

                const text = cleanText(element.textContent);

                if (text.endsWith(":")) {
                    ingredients.push(text);
                }
            }
        }

        return ingredients;
    }

    function getTexts(selectors) {

        const values = [];

        for (const selector of selectors) {

            const elements = document.querySelectorAll(selector);

            for (const element of elements) {

                const text = cleanText(element.textContent);

                if (text.length > 0) {
                    values.push(text);
                }
            }
        }

        return [...new Set(values)];
    }

    function getHtmlSteps() {

        const selectors = [
            "[itemprop='recipeInstructions']",

            ".ERSInstructions .instruction",
            ".ERSInstructions li",

            ".wprm-recipe-instruction-text",
            ".tasty-recipes-instructions li",
            ".mv-create-instructions li",
            ".recipe-instructions li",
            ".instructions li",
            ".directions li"
        ];

        const structuredSteps = getTexts(selectors);

        if (structuredSteps.length > 0) {
            return structuredSteps;
        }

        return getStepsFromArticleStructure();
    }

    function getStepsFromArticleStructure() {

        const children = getArticleElements();

        if (children.length === 0) {
            return [];
        }

        const preparationStartIndex =
            children.findIndex(element =>
                isPreparationHeading(element)
            );

        if (preparationStartIndex === -1) {
            return [];
        }

        const steps = [];

        for (
            let i = preparationStartIndex + 1;
            i < children.length;
            i++
        ) {

            const element = children[i];

            if (element.matches("ol, ul")) {

                const items = element.querySelectorAll("li");

                for (const item of items) {

                    const text = cleanText(item.textContent);

                    if (text.length > 0) {
                        steps.push(text);
                    }
                }

                continue;
            }

            if (element.matches("p")) {

                const text = cleanText(element.textContent);

                if (/^\d+\s*[.)]\s+/.test(text)) {
                    steps.push(text);
                }
            }
        }

        return steps;
    }

    function getHtmlAuthor() {

        const authorMeta = document.querySelector(
            'meta[name="author"]'
        );

        if (authorMeta?.content) {
            return cleanText(authorMeta.content);
        }


        const selectors = [
            "[itemprop='author'] [itemprop='name']",
            "[itemprop='author']",
            "[rel='author']",
            ".author"
        ];

        return getFirstText(selectors);
    }

    function getHtmlImage() {

        const sources = [
            {
                selector: 'meta[property="og:image"]',
                attributes: ["content"]
            },
            {
                selector: 'meta[name="twitter:image"]',
                attributes: ["content"]
            },
            {
                selector: "[itemprop='image']",
                attributes: ["src", "content", "href"]
            },
            {
                selector: "article img",
                attributes: ["src"]
            }
        ];

        for (const source of sources) {
            const imageUrl = getFirstValidImageUrl(
                source.selector,
                source.attributes
            );

            if (imageUrl) {
                return imageUrl;
            }
        }

        return null;
    }

    function getFirstValidImageUrl(selector, attributes) {

        const elements = document.querySelectorAll(selector);

        for (const element of elements) {
            for (const attribute of attributes) {
                const imageUrl = normalizeImageUrl(
                    element.getAttribute(attribute)
                );

                if (imageUrl) {
                    return imageUrl;
                }
            }
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

    function isIngredientsHeading(element) {

        const text = cleanText(element.textContent);

        if (!text) {
            return false;
        }

        const isHeading = element.matches("h1, h2, h3, h4, h5, h6");

        if (isHeading && /\bingrediente\b/i.test(text)) {
            return true;
        }

        return /^(ingrediente|ingredients)\b/i.test(text);
    }

    function isPreparationHeading(element) {

        const text = cleanText(element.textContent);

        if (!text) {
            return false;
        }

        return /^(pregătire|pregatire|preparare|mod de preparare|instrucțiuni|instructiuni|instructions|directions|steps)\b/i
            .test(text);
    }

    function getArticleElements() {

        const container =
            document.querySelector(".entry-content")
            ?? document.querySelector(".post-content")
            ?? document.querySelector(".article-content")
            ?? document.querySelector("article");

        if (!container) {
            return [];
        }

        return Array.from(
            container.querySelectorAll(
                "h1, h2, h3, h4, h5, h6, p, ul, ol"
            )
        );
    }
})();
