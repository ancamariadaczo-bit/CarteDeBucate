export function initializeSidePanelController({
    document,
    extractRecipe,
    saveRecipe,
    saveRecipeToApi,
    removeAccessToken = async () => { },
    login,
    openWindow,
    initialIsAuthenticated = false,
    initialIsAuthenticationResolved = true,
    reportError = () => { }
}) {
    const editButton = document.getElementById("editButton");
    const printButton = document.getElementById("printButton");
    const resultSeparator = document.getElementById("resultSeparator");
    const result = document.getElementById("result");
    const saveButton = document.getElementById("saveButton");
    const extractionStatus = document.getElementById("extractionStatus");
    const saveStatus = document.getElementById("saveStatus");
    const authSection = document.getElementById("authSection");
    const authMessage = document.getElementById("authMessage");
    const loginButton = document.getElementById("loginButton");
    const signInPrompt = authMessage.textContent.trim();

    let currentRecipe = null;
    let isExtracting = false;
    let isSaving = false;
    let isSaved = false;
    let isAuthenticated = initialIsAuthenticated;
    let isAuthenticationResolved = initialIsAuthenticationResolved;
    let isAuthenticating = false;
    const processedRequestIds = new Set();

    async function handleExtractionRequest({ requestId, tabId } = {}) {
        if (
            typeof requestId !== "string"
            || requestId.trim().length === 0
            || processedRequestIds.has(requestId)
        ) {
            return;
        }

        processedRequestIds.add(requestId);

        if (isSaving) {
            showExtractionStatus(
                "Wait for the current save to finish, then click the extension icon again.",
                "error"
            );
            return;
        }

        if (isExtracting) {
            showExtractionStatus(
                "Recipe extraction is already in progress. Please wait, then click the extension icon again.",
                "error"
            );
            return;
        }

        isExtracting = true;
        showExtractionStatus("Extracting recipe...");
        updateUi();

        try {
            const extractionResult = await extractRecipe(tabId);

            if (!extractionResult?.success) {
                const reason = extractionResult?.error
                    ?? "No recipe was found on this page.";

                showExtractionFailure(
                    `${reason} Click the extension icon to try again.`
                );
                return;
            }

            currentRecipe = extractionResult.recipe;
            isSaved = false;
            displayRecipe(currentRecipe);

            showExtractionStatus("");
            showSaveStatus("");
        } catch (error) {
            reportError(error);
            showExtractionFailure(
                "This page cannot be accessed. Open a regular web page and click the extension icon."
            );
        } finally {
            isExtracting = false;
            updateUi();
        }
    }

    editButton.addEventListener("click", async () => {
        if (!currentRecipe || isExtracting || isSaving) {
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
        if (!currentRecipe || isExtracting || isSaving) {
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

    saveButton.addEventListener("click", async () => {
        if (
            !currentRecipe
            || !isAuthenticated
            || isExtracting
            || isSaving
            || isSaved
        ) {
            return;
        }

        isSaving = true;
        updateUi();
        showSaveStatus("Saving...");

        try {
            await saveRecipeToApi(currentRecipe);
            isSaved = true;
            showSaveStatus("Recipe saved successfully.", "success");
        } catch (error) {
            if (isAuthenticationRequiredError(error)) {
                setAuthenticationState(false);
                showSaveStatus(
                    "Your session expired. Sign in again.",
                    "error"
                );

                try {
                    await removeAccessToken();
                } catch (storageError) {
                    reportError(storageError);
                }

                reportError(error);
                isSaved = false;
                return;
            }

            const reason = error instanceof Error
                ? error.message
                : "Unknown error.";

            showSaveStatus(
                `The recipe could not be saved. Reason: ${reason}`,
                "error"
            );

            reportError(error);
            isSaved = false;
        } finally {
            isSaving = false;
            updateUi();
        }
    });

    loginButton.addEventListener("click", async () => {
        if (!isAuthenticationResolved || isAuthenticating) {
            return;
        }

        isAuthenticating = true;
        authMessage.textContent = signInPrompt;
        updateUi();

        try {
            await login();
            setAuthenticationState(true);
        } catch (error) {
            authMessage.textContent =
                "Sign in was not completed. Please try again.";

            reportError(error);
        } finally {
            isAuthenticating = false;
            updateUi();
        }
    });

    // Prepares the UI regarding to the state and permissions.
    // currentRecipe == null → Edit/Print/Save hidden
    // currentRecipe != null → Edit/Print/Save visible
    // hasRecipe && authentication resolved && unauthenticated → Save disabled && Login visible
    // !hasRecipe || authentication pending/authenticated → Login hidden
    function updateUi() {
        const hasRecipe = currentRecipe !== null;

        editButton.hidden = !hasRecipe;
        printButton.hidden = !hasRecipe;
        saveButton.hidden = !hasRecipe;

        editButton.disabled = isExtracting || isSaving;
        printButton.disabled = isExtracting || isSaving;
        saveButton.disabled = !isAuthenticated || isSaving || isSaved || isExtracting;

        authSection.hidden = !hasRecipe
            || !isAuthenticationResolved
            || isAuthenticated;
        loginButton.disabled = isAuthenticating;

        resultSeparator.hidden = !hasRecipe;
    }

    function showSaveStatus(message, state = null) {
        saveStatus.textContent = message;
        saveStatus.classList.toggle("success", state === "success");
        saveStatus.classList.toggle("error", state === "error");
    }

    function showExtractionStatus(message, state = null) {
        extractionStatus.textContent = message;
        extractionStatus.classList.toggle("success", state === "success");
        extractionStatus.classList.toggle("error", state === "error");
    }

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
        showExtractionStatus(message, "error");
    }

    function setAuthenticationState(value) {
        isAuthenticated = Boolean(value);
        isAuthenticationResolved = true;
        updateUi();
    }

    updateUi();

    return {
        handleExtractionRequest,
        setAuthenticationState
    };
}

function isAuthenticationRequiredError(error) {
    return error instanceof Error && error.status === 401;
}

export async function extractRecipeFromTab({
    chrome,
    extractorFiles,
    tabId,
    reportExtraction = extractionResult => console.log(extractionResult)
}) {
    if (!Number.isInteger(tabId) || tabId < 0) {
        throw new TypeError("tabId must be a non-negative integer.");
    }

    await chrome.scripting.executeScript({
        target: {
            tabId
        },
        files: extractorFiles
    });

    const executionResults = await chrome.scripting.executeScript({
        target: {
            tabId
        },
        func: () => globalThis.RecipeClipper.extractRecipe()
    });

    const extractionResult = executionResults?.[0]?.result;

    if (extractionResult === undefined) {
        throw new Error("The extraction script did not return a result.");
    }

    reportExtraction(extractionResult);

    return extractionResult;
}
