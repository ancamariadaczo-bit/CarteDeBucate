const recipeContainer =
    document.getElementById("recipe");

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

    recipeContainer.appendChild(title);

    // SURSA

    if (recipe.sourceUrl) {

        const source =
            document.createElement("p");

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

        recipeContainer.appendChild(source);
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

        recipeContainer.appendChild(
            imageSource
        );

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

                imageSource.insertAdjacentElement(
                    "afterend",
                    message
                );
            }
        );

        recipeContainer.appendChild(image);
    }

    // INGREDIENTE

    const ingredientsTitle =
        document.createElement("h2");

    ingredientsTitle.textContent =
        "Ingrediente";

    recipeContainer.appendChild(
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

    recipeContainer.appendChild(
        ingredientsList
    );

    // PAȘI

    const stepsTitle =
        document.createElement("h2");

    stepsTitle.textContent =
        "Preparare";

    recipeContainer.appendChild(
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

    recipeContainer.appendChild(
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

                image.addEventListener(
                    "load",
                    resolve,
                    { once: true }
                );

                image.addEventListener(
                    "error",
                    resolve,
                    { once: true }
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