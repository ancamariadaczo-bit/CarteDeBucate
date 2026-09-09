const recipeContainer =
    document.getElementById("recipe");

const recipeSummary =
    document.getElementById("recipeSummary");

const imageSection =
    document.getElementById("imageSection");

const stepsSection =
    document.getElementById("stepsSection");

const imageLoadTimeoutMilliseconds =
    10000;

const recipeJson =
    localStorage.getItem("recipeToPrint");

if (!recipeJson) {

    recipeContainer.textContent =
        "Nu am găsit rețeta pentru print.";

} else {

    const recipe = JSON.parse(recipeJson);

    displayRecipe(recipe);

    document.title = recipe.name;

    printWhenReady();
}

function displayRecipe(recipe) {

    // TITLU

    const title =
        document.createElement("h1");

    title.textContent = recipe.name;

    recipeSummary.appendChild(title);

    // SURSA

    if (recipe.sourceUrl) {

        const source =
            document.createElement("p");

        source.className =
            "recipe-source";

        const sourceLabel =
            document.createElement("strong");

        sourceLabel.textContent =
            "Sursă: ";

        const sourceLink =
            document.createElement("a");

        sourceLink.href =
            recipe.sourceUrl;

        sourceLink.textContent =
            recipe.sourceUrl;

        source.appendChild(sourceLabel);
        source.appendChild(sourceLink);

        recipeSummary.appendChild(source);
    }

    // IMAGINE URL

    if (recipe.imageUrl) {

        const imageSource =
            document.createElement("p");

        const imageLabel =
            document.createElement("strong");

        imageLabel.textContent =
            "Imagine: ";

        const imageLink =
            document.createElement("a");

        imageLink.href =
            recipe.imageUrl;

        imageLink.textContent =
            recipe.imageUrl;

        imageSource.appendChild(imageLabel);
        imageSource.appendChild(imageLink);

        // IMAGINEA

        const image =
            document.createElement("img");

        image.src = recipe.imageUrl;

        image.alt = recipe.name;

        image.addEventListener(
            "error",
            () => {
                image.remove();

                const message =
                    document.createElement("p");

                message.textContent =
                    "Imaginea nu poate fi afișată direct.";

                message.className =
                    "image-error";

                imageSource.insertAdjacentElement(
                    "beforebegin",
                    message
                );
            }
        );

        imageSection.appendChild(image);

        imageSource.className =
            "image-source";

        imageSection.appendChild(
            imageSource
        );
    }

    // INGREDIENTE

    const ingredientsTitle =
        document.createElement("h2");

    ingredientsTitle.textContent =
        "Ingrediente";

    recipeSummary.appendChild(
        ingredientsTitle
    );

    const ingredientsList =
        document.createElement("ul");

    for (const ingredient of recipe.ingredients) {

        const item =
            document.createElement("li");

        item.textContent =
            ingredient;

        ingredientsList.appendChild(item);
    }

    recipeSummary.appendChild(
        ingredientsList
    );

    // PAȘI

    const stepsTitle =
        document.createElement("h2");

    stepsTitle.textContent =
        "Preparare";

    stepsSection.appendChild(
        stepsTitle
    );

    const stepsList =
        document.createElement("ol");

    for (const step of recipe.steps) {

        const item =
            document.createElement("li");

        item.textContent =
            step;

        stepsList.appendChild(item);
    }

    stepsSection.appendChild(
        stepsList
    );
}

function printWhenReady() {

    const images =
        Array.from(document.images);

    if (images.length === 0) {

        window.print();

        return;
    }

    const imagePromises =
        images.map(image => {

            if (image.complete) {
                return Promise.resolve();
            }

            return new Promise(resolve => {

                let timeoutId;

                const finishWaiting = () => {

                    clearTimeout(timeoutId);

                    image.removeEventListener(
                        "load",
                        finishWaiting
                    );

                    image.removeEventListener(
                        "error",
                        finishWaiting
                    );

                    resolve();
                };

                image.addEventListener(
                    "load",
                    finishWaiting,
                    { once: true }
                );

                image.addEventListener(
                    "error",
                    finishWaiting,
                    { once: true }
                );

                timeoutId = setTimeout(
                    finishWaiting,
                    imageLoadTimeoutMilliseconds
                );
            });
        });

    Promise.all(imagePromises)
        .then(() => {

            setTimeout(() => {
                window.print();
            }, 200);

        });
}
