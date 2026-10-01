import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import { initializeEditController } from "../controllers/editController.js";
import { createBrowserEffectsMock } from "./helpers/chromeMock.js";
import { createDomFromFile } from "./helpers/createDom.js";

const editHtmlPath = new URL("../edit.html", import.meta.url);
const editScriptPath = new URL("../edit.js", import.meta.url);

const validRecipe = {
    name: "Soup",
    sourceUrl: "https://recipes.example.test/soup",
    author: "Ana",
    imageUrl: "https://images.example.test/soup.jpg",
    ingredients: ["Water", "Salt"],
    steps: ["Boil water.", "Serve."]
};

async function setupEditor({
    recipePayload = JSON.stringify(validRecipe),
    payloadReadError = null,
    saveRecipe,
    saveRecipeToApi,
    recipeExistsBySourceUrl,
    removeAccessToken,
    login,
    initialIsAuthenticated = false,
    initialIsAuthenticationResolved = true,
    navigateToPrint,
    closeWindow,
    reportError
} = {}) {
    const context = await createDomFromFile(editHtmlPath);
    const browser = createBrowserEffectsMock();
    const calls = {
        savedRecipes: [],
        cookbookRequests: [],
        duplicateChecks: [],
        loginRequests: [],
        errors: []
    };
    const saveRecipeToApiEffect = saveRecipeToApi
        ?? (async () => ({ recipeId: 1 }));
    const recipeExistsBySourceUrlEffect = recipeExistsBySourceUrl
        ?? (async () => false);
    const loginEffect = login ?? (async () => {});

    const controller = initializeEditController({
        document: context.document,
        recipePayload,
        payloadReadError,
        saveRecipe: saveRecipe ?? (recipe => calls.savedRecipes.push(recipe)),
        saveRecipeToApi: async recipe => {
            calls.cookbookRequests.push(recipe);
            return saveRecipeToApiEffect(recipe);
        },
        recipeExistsBySourceUrl: async sourceUrl => {
            calls.duplicateChecks.push(sourceUrl);
            return recipeExistsBySourceUrlEffect(sourceUrl);
        },
        removeAccessToken: removeAccessToken ?? (async () => {}),
        login: async () => {
            calls.loginRequests.push(true);
            return loginEffect();
        },
        initialIsAuthenticated,
        initialIsAuthenticationResolved,
        navigateToPrint: navigateToPrint ?? (() => browser.effects.navigate("print.html")),
        closeWindow: closeWindow ?? browser.effects.closeWindow,
        reportError: reportError ?? (error => calls.errors.push(error))
    });

    return { ...context, browser, calls, controller };
}

function submit(window, form) {
    form.dispatchEvent(new window.Event("submit", {
        bubbles: true,
        cancelable: true
    }));
}

async function flushAsyncWork() {
    for (let index = 0; index < 6; index += 1) {
        await Promise.resolve();
    }
}

async function clickAndFlush(element) {
    element.click();
    await flushAsyncWork();
}

test("initializes the real editor page and populates fields and rows", async () => {
    const context = await setupEditor();

    try {
        assert.equal(context.document.getElementById("recipeName").value, "Soup");
        assert.equal(
            context.document.getElementById("sourceUrl").value,
            "https://recipes.example.test/soup"
        );
        assert.equal(
            context.document.getElementById("imageUrl").value,
            "https://images.example.test/soup.jpg"
        );
        assert.deepEqual(
            [...context.document.querySelectorAll(".ingredient-input")].map(input => input.value),
            ["Water", "Salt"]
        );
        assert.deepEqual(
            [...context.document.querySelectorAll(".instruction-input")].map(input => input.value),
            ["Boil water.", "Serve."]
        );
        assert.equal(context.document.getElementById("errorMessage").hidden, true);
    } finally {
        context.cleanup();
    }
});

test("editor auth state keeps Save disabled while unauthenticated and leaves Print active", async () => {
    const context = await setupEditor();

    try {
        assert.equal(context.document.getElementById("saveButton").disabled, true);
        assert.equal(context.document.getElementById("authSection").hidden, false);
        assert.equal(context.document.getElementById("printButton").disabled, false);
    } finally {
        context.cleanup();
    }
});

test("editor auth state enables Save and hides login for an authenticated user", async () => {
    const context = await setupEditor({ initialIsAuthenticated: true });

    try {
        assert.equal(context.document.getElementById("saveButton").disabled, false);
        assert.equal(context.document.getElementById("authSection").hidden, true);
        assert.equal(context.document.getElementById("printButton").disabled, false);
    } finally {
        context.cleanup();
    }
});

test("editor auth state can be updated after initialization", async () => {
    const context = await setupEditor();

    try {
        context.controller.setAuthenticationState(true);

        assert.equal(context.document.getElementById("saveButton").disabled, false);
        assert.equal(context.document.getElementById("authSection").hidden, true);
    } finally {
        context.cleanup();
    }
});

test("initial authentication resolution blocks login without blocking editing or printing", async () => {
    const context = await setupEditor({
        initialIsAuthenticationResolved: false
    });

    try {
        const loginButton = context.document.getElementById("loginButton");

        loginButton.click();
        await flushAsyncWork();

        assert.equal(context.calls.loginRequests.length, 0);
        assert.equal(context.document.getElementById("authSection").hidden, true);
        assert.equal(context.document.getElementById("saveButton").disabled, true);
        assert.equal(context.document.getElementById("printButton").disabled, false);
        assert.equal(context.document.getElementById("recipeName").disabled, false);

        context.controller.setAuthenticationState(false);

        assert.equal(context.document.getElementById("authSection").hidden, false);
        assert.equal(loginButton.disabled, false);
    } finally {
        context.cleanup();
    }
});

test("ignores a second login click while authentication is in progress", async () => {
    let completeLogin;
    const context = await setupEditor({
        login: () => new Promise(resolve => {
            completeLogin = resolve;
        })
    });

    try {
        const loginButton = context.document.getElementById("loginButton");

        loginButton.click();
        loginButton.dispatchEvent(new context.window.Event("click", {
            bubbles: true,
            cancelable: true
        }));

        assert.equal(context.calls.loginRequests.length, 1);
        assert.equal(loginButton.disabled, true);

        completeLogin();
        await flushAsyncWork();

        assert.equal(context.document.getElementById("authSection").hidden, true);
        assert.equal(context.document.getElementById("saveButton").disabled, false);
    } finally {
        context.cleanup();
    }
});

test("login enables Save without replacing edited form values", async () => {
    const context = await setupEditor();

    try {
        const recipeName = context.document.getElementById("recipeName");
        const ingredient = context.document.querySelector(".ingredient-input");

        recipeName.value = "Soup edited before login";
        ingredient.value = "Fresh water";

        await clickAndFlush(context.document.getElementById("loginButton"));

        assert.equal(context.calls.loginRequests.length, 1);
        assert.equal(context.document.getElementById("authSection").hidden, true);
        assert.equal(context.document.getElementById("saveButton").disabled, false);
        assert.equal(recipeName.value, "Soup edited before login");
        assert.equal(ingredient.value, "Fresh water");
    } finally {
        context.cleanup();
    }
});

test("login failure or cancellation keeps Save disabled and reports the error", async t => {
    const scenarios = [
        new Error("Login failed."),
        new Error("Login canceled.")
    ];

    for (const loginError of scenarios) {
        await t.test(loginError.message, async () => {
            const context = await setupEditor({
                login: async () => {
                    throw loginError;
                }
            });

            try {
                const recipeName = context.document.getElementById("recipeName");
                recipeName.value = "Unsaved edited soup";

                await clickAndFlush(context.document.getElementById("loginButton"));

                assert.equal(context.document.getElementById("saveButton").disabled, true);
                assert.equal(context.document.getElementById("authSection").hidden, false);
                assert.equal(
                    context.document.getElementById("authMessage").textContent.trim(),
                    "Sign in was not completed. Please try again."
                );
                assert.equal(recipeName.value, "Unsaved edited soup");
                assert.deepEqual(context.calls.errors, [loginError]);
            } finally {
                context.cleanup();
            }
        });
    }
});

test("disables editing for absent, invalid, or unreadable payloads", async t => {
    const scenarios = [
        { name: "absent", recipePayload: null },
        { name: "invalid JSON", recipePayload: "{invalid" },
        { name: "primitive", recipePayload: "42" },
        { name: "array", recipePayload: "[]" },
        { name: "storage read error", recipePayload: null, payloadReadError: new Error("storage") }
    ];

    for (const scenario of scenarios) {
        await t.test(scenario.name, async () => {
            const context = await setupEditor(scenario);

            try {
                const form = context.document.getElementById("recipeForm");
                const cancelButton = context.document.getElementById("cancelButton");

                for (const control of form.elements) {
                    if (control !== cancelButton) {
                        assert.equal(control.disabled, true, control.id);
                    }
                }

                assert.equal(cancelButton.disabled, false);
                assert.equal(context.document.getElementById("authSection").hidden, true);
                assert.equal(context.document.getElementById("errorMessage").hidden, false);
                assert.match(
                    context.document.getElementById("errorMessage").textContent,
                    scenario.payloadReadError ? /could not be initialized/ : /No recipe was found/
                );
            } finally {
                context.cleanup();
            }
        });
    }
});

test("adds, focuses, and removes rows while updating Add button states", async () => {
    const context = await setupEditor();

    try {
        const addIngredient = context.document.getElementById("addIngredientButton");
        const addInstruction = context.document.getElementById("addInstructionButton");

        assert.equal(addIngredient.disabled, false);
        assert.equal(addInstruction.disabled, false);

        addIngredient.click();
        const newIngredient = [...context.document.querySelectorAll(".ingredient-input")].at(-1);
        assert.equal(context.document.activeElement, newIngredient);
        assert.equal(addIngredient.disabled, true);

        newIngredient.value = "Pepper";
        newIngredient.dispatchEvent(new context.window.Event("input", { bubbles: true }));
        assert.equal(addIngredient.disabled, false);

        newIngredient.closest(".ingredient-row").querySelector(".remove-button").click();
        assert.equal(context.document.querySelectorAll(".ingredient-input").length, 2);
        assert.equal(addIngredient.disabled, false);

        addInstruction.click();
        const newInstruction = [...context.document.querySelectorAll(".instruction-input")].at(-1);
        assert.equal(context.document.activeElement, newInstruction);
        assert.equal(addInstruction.disabled, true);

        newInstruction.closest(".instruction-row").querySelector(".remove-button").click();
        assert.equal(context.document.querySelectorAll(".instruction-input").length, 2);
        assert.equal(addInstruction.disabled, false);
    } finally {
        context.cleanup();
    }
});

test("editor validation blocks print effects and shows every error", async () => {
    const context = await setupEditor();

    try {
        context.document.getElementById("recipeName").value = "";
        context.document.getElementById("sourceUrl").value = "invalid";
        context.document.getElementById("imageUrl").value = "ftp://image.test/a.jpg";

        for (const input of context.document.querySelectorAll(
            ".ingredient-input, .instruction-input"
        )) {
            input.value = "";
        }

        submit(context.window, context.document.getElementById("recipeForm"));

        const errorMessage = context.document.getElementById("errorMessage");
        assert.equal(errorMessage.hidden, false);
        assert.equal(context.document.activeElement, errorMessage);
        assert.deepEqual(
            [...errorMessage.querySelectorAll("li")].map(item => item.textContent),
            [
                "Recipe name is required.",
                "Source URL must be a valid HTTP or HTTPS URL.",
                "Image URL must be a valid HTTP or HTTPS URL.",
                "At least one ingredient is required.",
                "At least one instruction is required."
            ]
        );
        assert.equal(context.calls.savedRecipes.length, 0);
        assert.equal(context.calls.cookbookRequests.length, 0);
        assert.deepEqual(context.browser.calls.navigate, []);
    } finally {
        context.cleanup();
    }
});

test("printing saves normalized edits, preserves original properties, and skips Cookbook", async () => {
    const context = await setupEditor();

    try {
        context.document.getElementById("recipeName").value = " Edited Soup ";
        context.document.getElementById("sourceUrl").value =
            " https://recipes.example.test/edited-soup ";
        context.document.getElementById("imageUrl").value =
            " https://images.example.test/edited-soup.jpg ";
        context.document.querySelector(".ingredient-input").value = " Fresh water ";
        context.document.querySelector(".instruction-input").value =
            "\nBoil water.\n\nServe.\n";

        submit(context.window, context.document.getElementById("recipeForm"));

        assert.equal(context.calls.savedRecipes.length, 1);
        assert.deepEqual(context.calls.savedRecipes[0], {
            name: "Edited Soup",
            sourceUrl: "https://recipes.example.test/edited-soup",
            author: "Ana",
            imageUrl: "https://images.example.test/edited-soup.jpg",
            ingredients: ["Fresh water", "Salt"],
            steps: ["Boil water.\nServe.", "Serve."]
        });
        assert.deepEqual(context.browser.calls.navigate, ["print.html"]);
        assert.equal(context.calls.cookbookRequests.length, 0);
    } finally {
        context.cleanup();
    }
});

test("printing after corrections clears the old validation summary", async () => {
    const context = await setupEditor();

    try {
        const form = context.document.getElementById("recipeForm");
        const recipeName = context.document.getElementById("recipeName");
        const errorMessage = context.document.getElementById("errorMessage");

        recipeName.value = "";
        submit(context.window, form);

        assert.equal(errorMessage.hidden, false);
        assert.match(errorMessage.textContent, /Recipe name is required/);

        recipeName.value = "Corrected Soup";
        submit(context.window, form);

        assert.equal(errorMessage.hidden, true);
        assert.equal(errorMessage.textContent, "");
        assert.equal(context.calls.savedRecipes.length, 1);
        assert.deepEqual(context.browser.calls.navigate, ["print.html"]);
        assert.equal(context.calls.cookbookRequests.length, 0);
    } finally {
        context.cleanup();
    }
});

test("Save to Cookbook ignores unauthenticated clicks", async () => {
    const context = await setupEditor();

    try {
        const saveButton = context.document.getElementById("saveButton");

        saveButton.dispatchEvent(new context.window.Event("click", {
            bubbles: true,
            cancelable: true
        }));
        await flushAsyncWork();

        assert.equal(context.calls.cookbookRequests.length, 0);
        assert.equal(saveButton.disabled, true);
        assert.equal(context.document.getElementById("saveStatus").textContent, "");
    } finally {
        context.cleanup();
    }
});

test("Save to Cookbook sends the normalized edited recipe instead of the original payload", async () => {
    const context = await setupEditor({ initialIsAuthenticated: true });

    try {
        context.document.getElementById("recipeName").value = " Edited Soup ";
        context.document.getElementById("sourceUrl").value =
            " https://recipes.example.test/edited-soup ";
        context.document.querySelector(".ingredient-input").value = " Fresh water ";
        context.document.querySelector(".instruction-input").value =
            "\nBoil.\n\nServe.\n";

        await clickAndFlush(context.document.getElementById("saveButton"));

        assert.deepEqual(
            context.calls.duplicateChecks,
            ["https://recipes.example.test/edited-soup"]
        );
        assert.equal(context.calls.cookbookRequests.length, 1);
        assert.notDeepEqual(context.calls.cookbookRequests[0], validRecipe);
        assert.deepEqual(context.calls.cookbookRequests[0], {
            name: "Edited Soup",
            sourceUrl: "https://recipes.example.test/edited-soup",
            author: "Ana",
            imageUrl: "https://images.example.test/soup.jpg",
            ingredients: ["Fresh water", "Salt"],
            steps: ["Boil.\nServe.", "Serve."]
        });
    } finally {
        context.cleanup();
    }
});

test("Save to Cookbook does not request the API when edited values are invalid", async () => {
    const context = await setupEditor({ initialIsAuthenticated: true });

    try {
        context.document.getElementById("recipeName").value = "";

        await clickAndFlush(context.document.getElementById("saveButton"));

        assert.equal(context.calls.duplicateChecks.length, 0);
        assert.equal(context.calls.cookbookRequests.length, 0);
        assert.equal(context.document.getElementById("errorMessage").hidden, false);
        assert.equal(context.document.getElementById("saveButton").disabled, false);
    } finally {
        context.cleanup();
    }
});

test("Save to Cookbook keeps the edited form and skips saving when the URL is a duplicate", async () => {
    const context = await setupEditor({
        initialIsAuthenticated: true,
        recipeExistsBySourceUrl: async () => true
    });

    try {
        const recipeName = context.document.getElementById("recipeName");
        const sourceUrl = context.document.getElementById("sourceUrl");
        const saveButton = context.document.getElementById("saveButton");
        const saveStatus = context.document.getElementById("saveStatus");

        recipeName.value = "Edited duplicate soup";
        sourceUrl.value = " https://recipes.example.test/edited-duplicate ";

        await clickAndFlush(saveButton);

        assert.deepEqual(
            context.calls.duplicateChecks,
            ["https://recipes.example.test/edited-duplicate"]
        );
        assert.equal(context.calls.cookbookRequests.length, 0);
        assert.equal(
            saveStatus.textContent,
            "This recipe is already saved in your cookbook."
        );
        assert.equal(saveStatus.classList.contains("success"), false);
        assert.equal(saveStatus.classList.contains("error"), false);
        assert.equal(recipeName.value, "Edited duplicate soup");
        assert.equal(
            sourceUrl.value,
            "https://recipes.example.test/edited-duplicate"
        );
        assert.equal(saveButton.disabled, false);
        assert.equal(context.document.getElementById("printButton").disabled, false);
        assert.equal(context.document.getElementById("cancelButton").disabled, false);
    } finally {
        context.cleanup();
    }
});

test("Save to Cookbook ignores a second click while the duplicate check is pending", async () => {
    let completeCheck;
    const context = await setupEditor({
        initialIsAuthenticated: true,
        recipeExistsBySourceUrl: () => new Promise(resolve => {
            completeCheck = resolve;
        })
    });

    try {
        const saveButton = context.document.getElementById("saveButton");

        saveButton.click();

        assert.equal(saveButton.disabled, true);
        assert.equal(context.calls.duplicateChecks.length, 1);
        assert.equal(context.calls.cookbookRequests.length, 0);

        saveButton.dispatchEvent(new context.window.Event("click", {
            bubbles: true,
            cancelable: true
        }));

        assert.equal(context.calls.duplicateChecks.length, 1);
        assert.equal(context.calls.cookbookRequests.length, 0);

        completeCheck(false);
        await flushAsyncWork();

        assert.equal(context.calls.cookbookRequests.length, 1);
    } finally {
        context.cleanup();
    }
});

test("an unauthorized duplicate check removes the token without saving", async () => {
    const authenticationError = Object.assign(
        new Error("Authentication has expired. Sign in again."),
        { status: 401 }
    );
    let removeAccessTokenCount = 0;
    const context = await setupEditor({
        initialIsAuthenticated: true,
        recipeExistsBySourceUrl: async () => {
            throw authenticationError;
        },
        removeAccessToken: async () => {
            removeAccessTokenCount += 1;
        }
    });

    try {
        await clickAndFlush(context.document.getElementById("saveButton"));

        assert.equal(removeAccessTokenCount, 1);
        assert.equal(context.calls.cookbookRequests.length, 0);
        assert.equal(context.document.getElementById("authSection").hidden, false);
        assert.equal(context.document.getElementById("saveButton").disabled, true);
        assert.equal(
            context.document.getElementById("saveStatus").textContent,
            "Your session expired. Sign in again."
        );
        assert.deepEqual(context.calls.errors, [authenticationError]);
    } finally {
        context.cleanup();
    }
});

test("a technical duplicate-check failure is reported and saving continues", async () => {
    const duplicateCheckError = new Error("Duplicate endpoint unavailable.");
    const context = await setupEditor({
        initialIsAuthenticated: true,
        recipeExistsBySourceUrl: async () => {
            throw duplicateCheckError;
        }
    });

    try {
        await clickAndFlush(context.document.getElementById("saveButton"));

        assert.deepEqual(context.calls.errors, [duplicateCheckError]);
        assert.equal(context.calls.duplicateChecks.length, 1);
        assert.equal(context.calls.cookbookRequests.length, 1);
        assert.equal(
            context.document.getElementById("saveStatus").textContent,
            "Recipe saved successfully."
        );
    } finally {
        context.cleanup();
    }
});

test("a new Save attempt clears the duplicate message and uses one current snapshot", async () => {
    let duplicateCheckCount = 0;
    let completeSecondCheck;
    const context = await setupEditor({
        initialIsAuthenticated: true,
        recipeExistsBySourceUrl: async () => {
            duplicateCheckCount += 1;

            if (duplicateCheckCount === 1) {
                return true;
            }

            return new Promise(resolve => {
                completeSecondCheck = resolve;
            });
        }
    });

    try {
        const sourceUrl = context.document.getElementById("sourceUrl");
        const saveButton = context.document.getElementById("saveButton");
        const saveStatus = context.document.getElementById("saveStatus");

        await clickAndFlush(saveButton);

        assert.equal(
            saveStatus.textContent,
            "This recipe is already saved in your cookbook."
        );

        sourceUrl.value = " https://recipes.example.test/second-attempt ";
        saveButton.click();

        assert.equal(
            saveStatus.textContent,
            "Checking whether this recipe is already saved..."
        );
        assert.deepEqual(context.calls.duplicateChecks, [
            validRecipe.sourceUrl,
            "https://recipes.example.test/second-attempt"
        ]);

        sourceUrl.value = "https://recipes.example.test/changed-during-check";
        completeSecondCheck(false);
        await flushAsyncWork();

        assert.equal(context.calls.cookbookRequests.length, 1);
        assert.equal(
            context.calls.cookbookRequests[0].sourceUrl,
            "https://recipes.example.test/second-attempt"
        );
        assert.equal(
            sourceUrl.value,
            "https://recipes.example.test/changed-during-check"
        );
        assert.equal(saveStatus.textContent, "Recipe saved successfully.");
    } finally {
        context.cleanup();
    }
});

test("Save to Cookbook disables immediately, prevents parallel requests, and stays disabled after success", async () => {
    let completeSave;
    const pendingSave = new Promise(resolve => {
        completeSave = resolve;
    });
    const context = await setupEditor({
        initialIsAuthenticated: true,
        saveRecipeToApi: () => pendingSave
    });

    try {
        const saveButton = context.document.getElementById("saveButton");
        const saveStatus = context.document.getElementById("saveStatus");

        saveButton.click();

        assert.equal(saveButton.disabled, true);
        assert.equal(
            saveStatus.textContent,
            "Checking whether this recipe is already saved..."
        );
        assert.equal(context.calls.duplicateChecks.length, 1);
        assert.equal(context.calls.cookbookRequests.length, 0);

        await flushAsyncWork();

        assert.equal(saveStatus.textContent, "Saving...");
        assert.equal(context.calls.cookbookRequests.length, 1);

        saveButton.dispatchEvent(new context.window.Event("click", {
            bubbles: true,
            cancelable: true
        }));
        assert.equal(context.calls.cookbookRequests.length, 1);

        completeSave({ recipeId: 42 });
        await flushAsyncWork();

        assert.equal(saveButton.disabled, true);
        assert.equal(saveButton.textContent.trim(), "Save to Cookbook");
        assert.equal(saveStatus.textContent, "Recipe saved successfully.");
        assert.equal(saveStatus.classList.contains("success"), true);
        assert.equal(context.document.getElementById("printButton").disabled, false);
        assert.equal(context.document.getElementById("cancelButton").disabled, false);
        assert.equal(context.document.getElementById("recipeName").disabled, false);
    } finally {
        context.cleanup();
    }
});

test("Save to Cookbook re-enables after an API error and displays its reason", async () => {
    const saveError = new Error("A recipe with this source already exists.");
    const context = await setupEditor({
        initialIsAuthenticated: true,
        saveRecipeToApi: async () => {
            throw saveError;
        }
    });

    try {
        const saveButton = context.document.getElementById("saveButton");
        const saveStatus = context.document.getElementById("saveStatus");

        await clickAndFlush(saveButton);

        assert.equal(saveButton.disabled, false);
        assert.equal(
            saveStatus.textContent,
            "The recipe could not be saved. Reason: A recipe with this source already exists."
        );
        assert.equal(saveStatus.classList.contains("error"), true);
        assert.deepEqual(context.calls.errors, [saveError]);
        assert.equal(context.document.getElementById("printButton").disabled, false);
    } finally {
        context.cleanup();
    }
});

test("an expired editor session removes the token and reveals login again", async () => {
    const authenticationError = Object.assign(
        new Error("Authentication has expired. Sign in again."),
        { status: 401 }
    );
    let removeAccessTokenCount = 0;
    const context = await setupEditor({
        initialIsAuthenticated: true,
        saveRecipeToApi: async () => {
            throw authenticationError;
        },
        removeAccessToken: async () => {
            removeAccessTokenCount += 1;
        }
    });

    try {
        await clickAndFlush(context.document.getElementById("saveButton"));

        assert.equal(removeAccessTokenCount, 1);
        assert.equal(context.document.getElementById("authSection").hidden, false);
        assert.equal(context.document.getElementById("saveButton").disabled, true);
        assert.equal(
            context.document.getElementById("saveStatus").textContent,
            "Your session expired. Sign in again."
        );
        assert.deepEqual(context.calls.errors, [authenticationError]);
    } finally {
        context.cleanup();
    }
});

test("a new authenticated editor instance starts with Save enabled", async () => {
    const firstContext = await setupEditor({ initialIsAuthenticated: true });

    try {
        await clickAndFlush(firstContext.document.getElementById("saveButton"));
        assert.equal(firstContext.document.getElementById("saveButton").disabled, true);
    } finally {
        firstContext.cleanup();
    }

    const secondContext = await setupEditor({ initialIsAuthenticated: true });

    try {
        assert.equal(secondContext.document.getElementById("saveButton").disabled, false);
        assert.equal(secondContext.document.getElementById("saveStatus").textContent, "");
    } finally {
        secondContext.cleanup();
    }
});

test("cancel requests window closing", async () => {
    const context = await setupEditor();

    try {
        context.document.getElementById("cancelButton").click();
        assert.equal(context.browser.calls.close, 1);
    } finally {
        context.cleanup();
    }
});

test("reports save and navigation callback errors without leaving the editor", async t => {
    const failures = [
        {
            name: "save",
            overrides: { saveRecipe: () => { throw new Error("save failed"); } }
        },
        {
            name: "navigation",
            overrides: { navigateToPrint: () => { throw new Error("navigation failed"); } }
        }
    ];

    for (const failure of failures) {
        await t.test(failure.name, async () => {
            const context = await setupEditor(failure.overrides);

            try {
                submit(context.window, context.document.getElementById("recipeForm"));

                assert.equal(context.calls.errors.length, 1);
                assert.match(context.calls.errors[0].message, /failed/);
                assert.match(
                    context.document.getElementById("errorMessage").textContent,
                    /could not be prepared for printing/
                );
            } finally {
                context.cleanup();
            }
        });
    }
});

test("the real edit entrypoint composes authentication, Cookbook saving, print, and close effects", async () => {
    const [html, script] = await Promise.all([
        readFile(editHtmlPath, "utf8"),
        readFile(editScriptPath, "utf8")
    ]);

    assert.match(html, /<script\s+type="module"\s+src="edit\.js"><\/script>/);
    assert.match(html, /id="saveButton"\s+type="button"\s+disabled/);
    assert.match(html, /id="authSection"/);
    assert.match(html, /id="loginButton"\s+type="button"/);
    assert.match(html, /id="saveStatus"[^>]+role="status"/);
    assert.match(script, /import\s*{\s*initializeEditController\s*}/);
    assert.match(
        script,
        /import\s*\{[\s\S]*?recipeExistsBySourceUrl,[\s\S]*?saveRecipeToApi[\s\S]*?}\s*from "\.\/api\/recipeApiClient\.js"/
    );
    assert.match(script, /from "\.\/api\/recipeApiClient\.js"/);
    assert.match(script, /from "\.\/api\/authenticationApiClient\.js"/);
    assert.match(script, /from "\.\/auth\/authStorage\.js"/);
    assert.match(script, /from "\.\/auth\/authenticationState\.js"/);
    assert.match(script, /chrome\.runtime\.sendMessage\(\{\s*type: "LOGIN"/);
    assert.match(script, /resolveAuthenticationState\(\{/);
    assert.match(script, /initialIsAuthenticationResolved:\s*false/);
    assert.match(script, /getAccessToken\(\)/);
    assert.match(
        script,
        /recipeExistsBySourceUrl:\s*async sourceUrl\s*=>\s*\{[\s\S]*?const accessToken\s*=\s*await getAccessToken\(\);[\s\S]*?return recipeExistsBySourceUrl\(\s*sourceUrl,\s*accessToken\s*\);[\s\S]*?}/
    );
    assert.equal(
        (script.match(/const accessToken\s*=\s*await getAccessToken\(\);/g) ?? [])
            .length,
        2
    );
    assert.doesNotMatch(script, /\bfetch\s*\(/);
    assert.match(script, /localStorage\.getItem\("recipeToPrint"\)/);
    assert.match(script, /localStorage\.setItem\("recipeToPrint"/);
    assert.match(script, /chrome\.runtime\.getURL\("print\.html"\)/);
    assert.match(script, /window\.close\(\)/);
    assert.ok(
        script.indexOf("initializeEditController({")
        < script.indexOf("resolveAuthenticationState({")
    );
});
