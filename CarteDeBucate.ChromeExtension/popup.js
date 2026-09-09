const extractButton = document.getElementById("extractButton");
const printButton = document.getElementById("printButton");
const resultSeparator = document.getElementById("resultSeparator");
const result = document.getElementById("result");

let currentRecipe = null;

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
        extractButton.hidden = false;
        printButton.hidden = true;
        resultSeparator.hidden = true;
        return;
    }

    currentRecipe = extractionResult.recipe;
    displayRecipe(currentRecipe);
    extractButton.hidden = true;
    printButton.hidden = false;
    resultSeparator.hidden = false;
});

printButton.addEventListener("click", async () => {

    if (!currentRecipe) {
        return;
    }

    localStorage.setItem(
        "recipeToPrint",
        JSON.stringify(currentRecipe)
    );

    await chrome.windows.create({
        url: chrome.runtime.getURL("print.html"),
        type: "popup",
        width: 900,
        height: 700
    });
});

function displayRecipe(recipe) {

    result.textContent = "";

    const title = document.createElement("h3");
    title.textContent = recipe.name;

    result.appendChild(title);

    const source = document.createElement("p");

    const sourceLabel = document.createElement("strong");
    sourceLabel.textContent = "Source: ";

    const sourceLink = document.createElement("a");
    sourceLink.href = recipe.sourceUrl;
    sourceLink.textContent = recipe.sourceUrl;
    sourceLink.target = "_blank";

    source.appendChild(sourceLabel);
    source.appendChild(sourceLink);

    result.appendChild(source);

    if (recipe.imageUrl) {

        const imageUrl = document.createElement("p");

        const imageLabel = document.createElement("strong");
        imageLabel.textContent = "Image: ";

        const imageLink = document.createElement("a");
        imageLink.href = recipe.imageUrl;
        imageLink.textContent = recipe.imageUrl;
        imageLink.target = "_blank";

        imageUrl.appendChild(imageLabel);
        imageUrl.appendChild(imageLink);

        result.appendChild(imageUrl);

        const image = document.createElement("img");
        image.src = recipe.imageUrl;
        image.style.maxWidth = "300px";

        image.addEventListener("error", () => {
            image.remove();

            const message = document.createElement("p");
            message.textContent = "The image cannot be displayed directly.";

            imageLink.insertAdjacentElement("afterend", message);
        });

        result.appendChild(image);
    }

    const ingredientsTitle = document.createElement("h4");
    ingredientsTitle.textContent = "Ingredients";

    result.appendChild(ingredientsTitle);

    const ingredientList = document.createElement("ul");

    for (const ingredient of recipe.ingredients) {

        const item = document.createElement("li");
        item.textContent = ingredient;

        ingredientList.appendChild(item);
    }

    result.appendChild(ingredientList);

    const stepsTitle = document.createElement("h4");
    stepsTitle.textContent = "Instructions";

    result.appendChild(stepsTitle);

    const stepsList = document.createElement("ol");

    for (const step of recipe.steps) {

        const item = document.createElement("li");
        item.textContent = step;

        stepsList.appendChild(item);
    }

    result.appendChild(stepsList);
}
