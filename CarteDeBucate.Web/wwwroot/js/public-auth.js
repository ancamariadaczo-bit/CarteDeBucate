document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("[data-password-toggle]").forEach(button => {
        button.addEventListener("click", () => {
            const input = button.closest(".password-input").querySelector("input");
            const showing = input.type === "text";
            input.type = showing ? "password" : "text";
            button.textContent = showing ? button.dataset.showLabel : button.dataset.hideLabel;
            button.setAttribute("aria-label", button.textContent);
        });
    });
    document.querySelectorAll("form[data-auth-form]").forEach(form => {
        form.addEventListener("submit", () => {
            if (!form.checkValidity()) return;
            const submit = form.querySelector("button[type=submit]");
            submit.disabled = true;
            submit.setAttribute("aria-busy", "true");
            submit.textContent = submit.dataset.loadingLabel;
        });
    });
});
