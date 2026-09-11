export function initializePopupController({
    document,
    extractRecipe,
    saveRecipe,
    openWindow,
    reportError = () => {}
}) {
    const extractButton = document.getElementById("extractButton");
    const editButton = document.getElementById("editButton");
    const printButton = document.getElementById("printButton");
    const resultSeparator = document.getElementById("resultSeparator");
    const result = document.getElementById("result");

    let currentRecipe = null;

    extractButton.addEventListener("click", async () => {
        let extractionResult;

        try {
            extractionResult = await extractRecipe();
        } catch (error) {
            reportError(error);
            showExtractionFailure("The recipe could not be extracted from this page.");
            return;
        }

        if (!extractionResult?.success) {
            showExtractionFailure(
                extractionResult?.error
                ?? "The recipe could not be extracted from this page."
            );
            return;
        }

        currentRecipe = extractionResult.recipe;
        displayRecipe(currentRecipe);
        extractButton.hidden = true;
        editButton.hidden = false;
        printButton.hidden = false;
        resultSeparator.hidden = false;
    });

    editButton.addEventListener("click", async () => {
        if (!currentRecipe) {
            return;
        }

        saveRecipe(currentRecipe);

        await openWindow({
            page: "edit.html",
            type: "popup",
            width: 900,
            height: 700
        });
    });

    printButton.addEventListener("click", async () => {
        if (!currentRecipe) {
            return;
        }

        saveRecipe(currentRecipe);

        await openWindow({
            page: "print.html",
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

    function showExtractionFailure(message) {
        currentRecipe = null;
        result.textContent = message;
        extractButton.hidden = false;
        editButton.hidden = true;
        printButton.hidden = true;
        resultSeparator.hidden = true;
    }
}

export async function extractRecipeFromActiveTab({
    chrome,
    extractorFiles,
    reportExtraction = extractionResult => console.log(extractionResult)
}) {
    const [tab] = await chrome.tabs.query({
        active: true,
        currentWindow: true
    });

    await chrome.scripting.executeScript({
        target: {
            tabId: tab.id
        },
        files: extractorFiles
    });

    const executionResults = await chrome.scripting.executeScript({
        target: {
            tabId: tab.id
        },
        func: () => globalThis.RecipeClipper.extractRecipe()
    });

    const extractionResult = executionResults[0].result;

    reportExtraction(extractionResult);

    return extractionResult;
}
