const extractButton = document.getElementById("extractButton");
const result = document.getElementById("result");

extractButton.addEventListener("click", async () => {

    const [tab] = await chrome.tabs.query({
        active: true,
        currentWindow: true
    });

    await chrome.scripting.executeScript({
        target: {
            tabId: tab.id
        },
        files: [
            "extractors/jsonLdImporter.js",
            "extractors/htmlImporter.js",
            "extractors/recipeExtractor.js"
        ]
    });

    const executionResults = await chrome.scripting.executeScript({
        target: {
            tabId: tab.id
        },
        func: () =>
            globalThis.RecipeClipper.extractRecipe()
    });

    const extractionResult = executionResults[0].result;

    console.log(extractionResult);

    if (!extractionResult.success) {
        result.textContent = extractionResult.error;
        return;
    }

    displayRecipe(extractionResult.recipe);
});

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
