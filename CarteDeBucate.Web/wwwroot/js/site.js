// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", () => {
    const editableForms = document.querySelectorAll(
        "form[data-track-unsaved-changes='true']");
    const importRecipeForm = document.querySelector(
        "form[data-import-recipe-form='true']");

    editableForms.forEach((form) => {
        form.dataset.hasUnsavedChanges = "false";

        form.addEventListener("input", markFormAsChanged);
        form.addEventListener("change", markFormAsChanged);
        form.addEventListener("submit", clearChangesBeforeValidSubmit);

        const navigationLinks = form.querySelectorAll(
            "a[data-confirm-unsaved-changes='true']");

        navigationLinks.forEach((link) => {
            link.addEventListener("click", confirmNavigationWithUnsavedChanges);
        });
    });

    if (importRecipeForm !== null) {
        importRecipeForm.addEventListener("submit", showImportLoadingState);
    }
});

window.addEventListener("beforeunload", (event) => {
    if (!hasUnsavedChanges()) {
        return;
    }

    event.preventDefault();
    event.returnValue = "";
});

function markFormAsChanged(event) {
    if (!isEditableFormField(event.target)) {
        return;
    }

    event.currentTarget.dataset.hasUnsavedChanges = "true";
}

function clearChangesBeforeValidSubmit(event) {
    if (!event.currentTarget.checkValidity()) {
        return;
    }

    event.currentTarget.dataset.hasUnsavedChanges = "false";
}

function confirmNavigationWithUnsavedChanges(event) {
    const form = event.currentTarget.closest(
        "form[data-track-unsaved-changes='true']");

    if (form === null || form.dataset.hasUnsavedChanges !== "true") {
        return;
    }

    const shouldLeave = window.confirm(
        "Ai modificări nesalvate. Dacă părăsești pagina, modificările se vor pierde.");

    if (!shouldLeave) {
        event.preventDefault();
    }
}

function showImportLoadingState(event) {
    const form = event.currentTarget;

    if (!form.checkValidity()) {
        return;
    }

    const submitButton = form.querySelector(
        "button[data-import-submit-button='true']");

    if (submitButton === null) {
        return;
    }

    const submitText = submitButton.querySelector(
        "[data-import-submit-text='true']");
    const spinner = submitButton.querySelector(
        "[data-import-submit-spinner='true']");

    submitButton.disabled = true;

    if (submitText !== null) {
        submitText.textContent = "Se importă...";
    }

    if (spinner !== null) {
        spinner.classList.remove("d-none");
    }
}

function isEditableFormField(element) {
    if (!(element instanceof HTMLElement)) {
        return false;
    }

    const tagName = element.tagName.toLowerCase();

    return tagName === "input"
        || tagName === "textarea"
        || tagName === "select";
}

function hasUnsavedChanges() {
    return document.querySelector(
        "form[data-track-unsaved-changes='true'][data-has-unsaved-changes='true']")
        !== null;
}
