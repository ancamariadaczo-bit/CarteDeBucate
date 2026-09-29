import {
    parseRecipePayload,
    validateAndBuildEditedRecipe
} from "../editLogic.js";

export function initializeEditController({
    document,
    recipePayload,
    payloadReadError = null,
    saveRecipe,
    saveRecipeToApi,
    removeAccessToken = async () => {},
    login,
    initialIsAuthenticated = false,
    initialIsAuthenticationResolved = true,
    navigateToPrint,
    closeWindow,
    reportError = () => {}
}) {
    const recipeForm = document.getElementById("recipeForm");
    const recipeNameInput = document.getElementById("recipeName");
    const sourceUrlInput = document.getElementById("sourceUrl");
    const imageUrlInput = document.getElementById("imageUrl");
    const ingredientRows = document.getElementById("ingredientRows");
    const instructionRows = document.getElementById("instructionRows");
    const addIngredientButton = document.getElementById("addIngredientButton");
    const addInstructionButton = document.getElementById("addInstructionButton");
    const printButton = document.getElementById("printButton");
    const saveButton = document.getElementById("saveButton");
    const cancelButton = document.getElementById("cancelButton");
    const saveStatus = document.getElementById("saveStatus");
    const authSection = document.getElementById("authSection");
    const authMessage = document.getElementById("authMessage");
    const loginButton = document.getElementById("loginButton");
    const errorMessage = document.getElementById("errorMessage");
    const signInPrompt = authMessage.textContent.trim();

    let originalRecipe = null;
    let isAuthenticated = initialIsAuthenticated;
    let isAuthenticationResolved = initialIsAuthenticationResolved;
    let isAuthenticating = false;
    let isSaving = false;
    let isSaved = false;

    addIngredientButton.addEventListener("click", () => {
        const input = appendIngredientRow("");
        input.focus();
    });

    addInstructionButton.addEventListener("click", () => {
        const textarea = appendInstructionRow("");
        textarea.focus();
    });

    recipeForm.addEventListener("submit", event => {
        event.preventDefault();

        const editedRecipe = buildEditedRecipe();

        if (!editedRecipe) {
            return;
        }

        try {
            saveRecipe(editedRecipe);
            navigateToPrint();
        } catch (error) {
            reportError(error);
            showError("The edited recipe could not be prepared for printing.");
        }
    });

    saveButton.addEventListener("click", async () => {
        if (!originalRecipe || !isAuthenticated || isSaving || isSaved) {
            return;
        }

        const editedRecipe = buildEditedRecipe();

        if (!editedRecipe) {
            return;
        }

        isSaving = true;
        updateUi();
        showSaveStatus("Saving...");

        try {
            await saveRecipeToApi(editedRecipe);

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
            isAuthenticated = false;
            authMessage.textContent =
                "Sign in was not completed. Please try again.";
            reportError(error);
        } finally {
            isAuthenticating = false;
            updateUi();
        }
    });

    cancelButton.addEventListener("click", () => {
        closeWindow();
    });

    initializeEditor();

    return {
        setAuthenticationState
    };

    function buildEditedRecipe() {
        if (!originalRecipe) {
            showPayloadError();
            return null;
        }

        const formValues = {
            name: recipeNameInput.value,
            sourceUrl: sourceUrlInput.value,
            imageUrl: imageUrlInput.value,
            ingredients: collectValues(".ingredient-input"),
            steps: collectValues(".instruction-input")
        };

        const validationResult = validateAndBuildEditedRecipe(
            originalRecipe,
            formValues
        );

        if (!validationResult.success) {
            showValidationErrors(validationResult.errors);
            return null;
        }

        clearErrorMessage();

        return validationResult.recipe;
    }

    function initializeEditor() {
        if (payloadReadError) {
            disableEditor();
            showError("The recipe editor could not be initialized.");
            return;
        }

        const recipe = parseRecipePayload(recipePayload);

        if (!recipe) {
            showPayloadError();
            return;
        }

        originalRecipe = recipe;

        try {
            recipeNameInput.value = toEditableText(recipe.name);
            sourceUrlInput.value = toEditableText(recipe.sourceUrl);
            imageUrlInput.value = toEditableText(recipe.imageUrl);

            const ingredients = Array.isArray(recipe.ingredients)
                ? recipe.ingredients
                : [];

            for (const ingredient of ingredients) {
                appendIngredientRow(toEditableText(ingredient));
            }

            const steps = Array.isArray(recipe.steps)
                ? recipe.steps
                : [];

            for (const step of steps) {
                appendInstructionRow(toEditableText(step));
            }

            updateUi();
        } catch (error) {
            reportError(error);
            originalRecipe = null;
            disableEditor();
            showError("The recipe editor could not be initialized.");
        }
    }

    function appendIngredientRow(value) {
        const row = document.createElement("div");
        row.className = "dynamic-row ingredient-row";

        const input = document.createElement("input");
        input.className = "ingredient-input";
        input.type = "text";
        input.value = value;
        input.setAttribute("aria-label", "Ingredient");
        input.addEventListener("input", updateAddButtonStates);

        const removeButton = createRemoveButton(row);

        row.appendChild(input);
        row.appendChild(removeButton);
        ingredientRows.appendChild(row);
        updateAddButtonStates();

        return input;
    }

    function appendInstructionRow(value) {
        const row = document.createElement("div");
        row.className = "dynamic-row instruction-row";

        const textarea = document.createElement("textarea");
        textarea.className = "instruction-input";
        textarea.value = value;
        textarea.rows = 3;
        textarea.setAttribute("aria-label", "Instruction");
        textarea.addEventListener("input", updateAddButtonStates);

        const removeButton = createRemoveButton(row);

        row.appendChild(textarea);
        row.appendChild(removeButton);
        instructionRows.appendChild(row);
        updateAddButtonStates();

        return textarea;
    }

    function createRemoveButton(row) {
        const button = document.createElement("button");
        button.className = "remove-button";
        button.type = "button";
        button.textContent = "Remove";

        button.addEventListener("click", () => {
            row.remove();
            updateAddButtonStates();
        });

        return button;
    }

    function updateAddButtonStates() {
        addIngredientButton.disabled = hasEmptyField(".ingredient-input");
        addInstructionButton.disabled = hasEmptyField(".instruction-input");
    }

    function hasEmptyField(selector) {
        return Array.from(document.querySelectorAll(selector))
            .some(field => !field.value.trim());
    }

    function collectValues(selector) {
        return Array.from(document.querySelectorAll(selector))
            .map(field => field.value);
    }

    function toEditableText(value) {
        return typeof value === "string" ? value : "";
    }

    function showPayloadError() {
        disableEditor();
        showError("No recipe was found to edit.");
    }

    function disableEditor() {
        for (const element of recipeForm.elements) {
            if (element !== cancelButton) {
                element.disabled = true;
            }
        }

        printButton.disabled = true;
        authSection.hidden = true;
    }

    function setAuthenticationState(value) {
        isAuthenticated = Boolean(value);
        isAuthenticationResolved = true;
        updateUi();
    }

    function updateUi() {
        const hasEditableRecipe = originalRecipe !== null;

        authSection.hidden = !hasEditableRecipe
            || !isAuthenticationResolved
            || isAuthenticated;

        loginButton.disabled = isAuthenticating;

        if (hasEditableRecipe) {
            saveButton.disabled = !isAuthenticationResolved
                || !isAuthenticated
                || isSaving
                || isSaved;
        }
    }

    function showSaveStatus(message, state = null) {
        saveStatus.textContent = message;
        saveStatus.classList.toggle("success", state === "success");
        saveStatus.classList.toggle("error", state === "error");
    }

    function showValidationErrors(errors) {
        errorMessage.textContent = "";

        const summary = document.createElement("p");
        summary.textContent = "Please correct the following errors:";

        const errorList = document.createElement("ul");

        for (const error of errors) {
            const item = document.createElement("li");
            item.textContent = error;
            errorList.appendChild(item);
        }

        errorMessage.appendChild(summary);
        errorMessage.appendChild(errorList);
        revealErrorMessage();
    }

    function showError(message) {
        errorMessage.textContent = message;
        revealErrorMessage();
    }

    function clearErrorMessage() {
        errorMessage.textContent = "";
        errorMessage.hidden = true;
    }

    function revealErrorMessage() {
        errorMessage.hidden = false;
        errorMessage.focus();
    }
}

function isAuthenticationRequiredError(error) {
    return error instanceof Error && error.status === 401;
}
