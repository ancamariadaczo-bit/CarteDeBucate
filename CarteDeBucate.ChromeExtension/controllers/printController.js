const imageLoadTimeoutMilliseconds = 10000;
const printDelayMilliseconds = 200;

export function initializePrintController({
    document,
    recipePayload,
    requestPrint,
    setTimeout: scheduleTimeout,
    clearTimeout: cancelTimeout
}) {
    const recipeContainer = document.getElementById("recipe");
    const recipeSummary = document.getElementById("recipeSummary");
    const imageSection = document.getElementById("imageSection");
    const stepsSection = document.getElementById("stepsSection");

    const recipe = parsePrintableRecipe(recipePayload);

    if (!recipe) {
        recipeContainer.textContent = "No recipe was found to print.";
        return;
    }

    displayRecipe(recipe);
    document.title = recipe.name;
    printWhenReady();

    function displayRecipe(recipeToDisplay) {
        const title = document.createElement("h1");
        title.textContent = recipeToDisplay.name;
        recipeSummary.appendChild(title);

        if (recipeToDisplay.sourceUrl) {
            const source = document.createElement("p");
            source.className = "recipe-source";

            const sourceLabel = document.createElement("strong");
            sourceLabel.textContent = "Source: ";

            const sourceLink = document.createElement("a");
            sourceLink.href = recipeToDisplay.sourceUrl;
            sourceLink.textContent = recipeToDisplay.sourceUrl;

            source.appendChild(sourceLabel);
            source.appendChild(sourceLink);
            recipeSummary.appendChild(source);
        }

        if (recipeToDisplay.imageUrl) {
            const imageSource = document.createElement("p");
            const imageLabel = document.createElement("strong");
            imageLabel.textContent = "Image: ";

            const imageLink = document.createElement("a");
            imageLink.href = recipeToDisplay.imageUrl;
            imageLink.textContent = recipeToDisplay.imageUrl;

            imageSource.appendChild(imageLabel);
            imageSource.appendChild(imageLink);

            const image = document.createElement("img");
            image.src = recipeToDisplay.imageUrl;
            image.alt = recipeToDisplay.name;

            image.addEventListener("error", () => {
                image.remove();

                const message = document.createElement("p");
                message.textContent = "The image cannot be displayed directly.";
                message.className = "image-error";
                imageSource.insertAdjacentElement("beforebegin", message);
            });

            imageSection.appendChild(image);
            imageSource.className = "image-source";
            imageSection.appendChild(imageSource);
        }

        const ingredientsTitle = document.createElement("h2");
        ingredientsTitle.textContent = "Ingredients";
        recipeSummary.appendChild(ingredientsTitle);

        const ingredientsList = document.createElement("ul");

        for (const ingredient of recipeToDisplay.ingredients) {
            const item = document.createElement("li");
            item.textContent = ingredient;
            ingredientsList.appendChild(item);
        }

        recipeSummary.appendChild(ingredientsList);

        const stepsTitle = document.createElement("h2");
        stepsTitle.textContent = "Instructions";
        stepsSection.appendChild(stepsTitle);

        const stepsList = document.createElement("ol");

        for (const step of recipeToDisplay.steps) {
            const item = document.createElement("li");
            item.textContent = step;
            stepsList.appendChild(item);
        }

        stepsSection.appendChild(stepsList);
    }

    function printWhenReady() {
        const images = Array.from(document.images);

        if (images.length === 0) {
            requestPrint();
            return;
        }

        const imagePromises = images.map(image => {
            if (image.complete) {
                return Promise.resolve();
            }

            return new Promise(resolve => {
                let timeoutId;

                const finishWaiting = () => {
                    cancelTimeout(timeoutId);
                    image.removeEventListener("load", finishWaiting);
                    image.removeEventListener("error", finishWaiting);
                    resolve();
                };

                image.addEventListener("load", finishWaiting, { once: true });
                image.addEventListener("error", finishWaiting, { once: true });

                timeoutId = scheduleTimeout(
                    finishWaiting,
                    imageLoadTimeoutMilliseconds
                );
            });
        });

        Promise.all(imagePromises).then(() => {
            scheduleTimeout(requestPrint, printDelayMilliseconds);
        });
    }
}

function parsePrintableRecipe(recipePayload) {
    if (typeof recipePayload !== "string" || !recipePayload) {
        return null;
    }

    let recipe;

    try {
        recipe = JSON.parse(recipePayload);
    } catch {
        return null;
    }

    if (
        !recipe ||
        typeof recipe !== "object" ||
        Array.isArray(recipe) ||
        !Array.isArray(recipe.ingredients) ||
        !Array.isArray(recipe.steps)
    ) {
        return null;
    }

    return recipe;
}
