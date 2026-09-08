const extractButton = document.getElementById("extractButton");
const result = document.getElementById("result");

extractButton.addEventListener("click", async () => {

    const [tab] = await chrome.tabs.query({
        active: true,
        currentWindow: true
    });

    const executionResults = await chrome.scripting.executeScript({
        target: {
            tabId: tab.id
        },
        func: extractRecipeFromPage
    });

    const extractionResult = executionResults[0].result;

    console.log(extractionResult);

    if (!extractionResult.success) {
        result.textContent = extractionResult.error;
        return;
    }

    displayRecipe(extractionResult.recipe);
});

function extractRecipeFromPage() {

    const jsonLdRecipe = tryJsonLd();

    if (jsonLdRecipe !== null) {
        return {
            success: true,
            method: "json-ld",
            recipe: jsonLdRecipe
        };
    }

    const htmlRecipe = tryHtml();

    if (htmlRecipe !== null) {
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

    function tryJsonLd() {

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
    }

	function tryHtml() {

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
	}

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

		const ogImage = document.querySelector(
			'meta[property="og:image"]'
		);

		if (ogImage?.content) {
			return ogImage.content;
		}

		const twitterImage = document.querySelector(
			'meta[name="twitter:image"]'
		);

		if (twitterImage?.content) {
			return twitterImage.content;
		}

		const recipeImage = document.querySelector(
			"[itemprop='image']"
		);

		if (recipeImage) {

			return recipeImage.src
				?? recipeImage.content
				?? null;
		}

		const articleImage = document.querySelector(
			"article img"
		);

		if (articleImage?.src) {
			return articleImage.src;
		}

		return null;
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
}

function displayRecipe(recipe) {

    result.textContent = "";

    const title = document.createElement("h3");
    title.textContent = recipe.name;

    result.appendChild(title);

	const source = document.createElement("p");
    source.textContent = recipe.sourceUrl;

    result.appendChild(source);

	if (recipe.imageUrl) {

		const imageLink = document.createElement("p");
		imageLink.textContent = `Imagine: ${recipe.imageUrl}`;
		result.appendChild(imageLink);

		const image = document.createElement("img");
		image.src = recipe.imageUrl;
		image.style.maxWidth = "300px";

		image.addEventListener("error", () => {
			image.remove();

			const message = document.createElement("p");
			message.textContent = "Imaginea nu poate fi afișată direct.";

			imageLink.insertAdjacentElement("afterend", message);
		});

		result.appendChild(image);
	}

    const ingredientsTitle = document.createElement("h4");
    ingredientsTitle.textContent = "Ingrediente";

    result.appendChild(ingredientsTitle);


    const ingredientList = document.createElement("ul");

    for (const ingredient of recipe.ingredients) {

        const item = document.createElement("li");
        item.textContent = ingredient;

        ingredientList.appendChild(item);
    }

    result.appendChild(ingredientList);

    const stepsTitle = document.createElement("h4");
    stepsTitle.textContent = "Pași";

    result.appendChild(stepsTitle);

    const stepsList = document.createElement("ol");

    for (const step of recipe.steps) {

        const item = document.createElement("li");
        item.textContent = step;

        stepsList.appendChild(item);
    }

    result.appendChild(stepsList);
}