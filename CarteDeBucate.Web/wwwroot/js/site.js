// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", () => {
    const editableForms = document.querySelectorAll(
        "form[data-track-unsaved-changes='true']");

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
        "Ai modificari nesalvate. Daca parasesti pagina, modificarile se vor pierde.");

    if (!shouldLeave) {
        event.preventDefault();
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
