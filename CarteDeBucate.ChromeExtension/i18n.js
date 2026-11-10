function localizeDocument(document) {
    document.querySelectorAll("[data-i18n]").forEach(element => {
        const message = chrome.i18n.getMessage(element.dataset.i18n);

        if (message) {
            element.textContent = message;
        }
    });

    document.querySelectorAll("[data-i18n-title]").forEach(element => {
        const message = chrome.i18n.getMessage(element.dataset.i18nTitle);

        if (message) {
            element.textContent = message;
        }
    });
}

document.addEventListener("DOMContentLoaded", () => localizeDocument(document));
