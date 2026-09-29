import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import {
    extractRecipeFromTab,
    initializeSidePanelController
} from "../controllers/sidePanelController.js";
import {
    createBrowserEffectsMock,
    createChromeMock,
    createStorageMock
} from "./helpers/chromeMock.js";
import { createDomFromFile } from "./helpers/createDom.js";

const sidePanelHtmlPath = new URL("../sidepanel.html", import.meta.url);
const sidePanelScriptPath = new URL("../sidepanel.js", import.meta.url);

const recipe = {
    name: "Soup",
    sourceUrl: "https://recipes.example.test/soup",
    author: "Ana",
    imageUrl: "https://images.example.test/soup.jpg",
    ingredients: ["Water", "Salt"],
    steps: ["Boil.", "Serve."]
};

const secondRecipe = {
    name: "Salad",
    sourceUrl: "https://recipes.example.test/salad",
    author: "Mara",
    imageUrl: null,
    ingredients: ["Tomatoes", "Salt"],
    steps: ["Mix."]
};

async function setupSidePanel(extractRecipe, {
    saveRecipeToApi = async () => {},
    removeAccessToken = async () => {},
    login = async () => {},
    initialIsAuthenticated = false,
    initialIsAuthenticationResolved = true,
    reportError = () => {}
} = {}) {
    const context = await createDomFromFile(sidePanelHtmlPath);
    const storage = createStorageMock();
    const browser = createBrowserEffectsMock();

    const controller = initializeSidePanelController({
        document: context.document,
        extractRecipe,
        saveRecipe: value => storage.storage.setItem("recipeToPrint", JSON.stringify(value)),
        saveRecipeToApi,
        removeAccessToken,
        login,
        openWindow: browser.effects.openWindow,
        initialIsAuthenticated,
        initialIsAuthenticationResolved,
        reportError
    });

    return { ...context, storage, browser, controller };
}

async function clickAndFlush(element) {
    element.click();
    await Promise.resolve();
    await Promise.resolve();
}

async function requestExtraction(context, {
    requestId = "request-1",
    tabId = 101
} = {}) {
    await context.controller.handleExtractionRequest({ requestId, tabId });
}

test("starts with extraction guidance and no Extract action", async () => {
    const context = await setupSidePanel(async () => ({ success: false, error: "unused" }));

    try {
        const extractionStatus = context.document.getElementById("extractionStatus");

        assert.equal(context.document.getElementById("extractButton"), null);
        assert.equal(
            extractionStatus.textContent.trim(),
            "Click the extension icon to extract a recipe from the current page."
        );
        assert.equal(extractionStatus.getAttribute("role"), "status");
        assert.equal(extractionStatus.getAttribute("aria-live"), "polite");
        assert.deepEqual(
            [...context.document.querySelectorAll(".controls button")]
                .map(button => button.id),
            ["editButton", "printButton", "saveButton"]
        );
        assert.equal(context.document.getElementById("editButton").hidden, true);
        assert.equal(context.document.getElementById("printButton").hidden, true);
        assert.equal(context.document.getElementById("saveButton").hidden, true);
        assert.equal(context.document.getElementById("authSection").hidden, true);
        assert.equal(context.document.getElementById("registerButton"), null);
        assert.equal(context.document.getElementById("resultSeparator").hidden, true);
        assert.equal(context.document.getElementById("result").textContent, "");
    } finally {
        context.cleanup();
    }
});

test("starts extraction through the controller and passes the requested tab id", async () => {
    let receivedTabId;
    let completeExtraction;
    const context = await setupSidePanel(tabId => {
        receivedTabId = tabId;

        return new Promise(resolve => {
            completeExtraction = resolve;
        });
    });

    try {
        const extraction = context.controller.handleExtractionRequest({
            requestId: "request-1",
            tabId: 321
        });

        assert.equal(receivedTabId, 321);
        assert.equal(
            context.document.getElementById("extractionStatus").textContent,
            "Extracting recipe..."
        );
        assert.equal(context.document.getElementById("editButton").hidden, true);
        assert.equal(context.document.getElementById("printButton").hidden, true);
        assert.equal(context.document.getElementById("saveButton").hidden, true);

        completeExtraction({ success: true, recipe });
        await extraction;

        assert.equal(context.document.getElementById("extractionStatus").textContent, "");
        assert.equal(context.document.getElementById("editButton").hidden, false);
        assert.equal(context.document.getElementById("printButton").hidden, false);
        assert.equal(context.document.getElementById("saveButton").hidden, false);
    } finally {
        context.cleanup();
    }
});

test("prevents parallel extraction and ignores duplicate request ids", async () => {
    let extractionCount = 0;
    let completeExtraction;
    const context = await setupSidePanel(async () => {
        extractionCount += 1;

        return new Promise(resolve => {
            completeExtraction = resolve;
        });
    });

    try {
        const firstExtraction = context.controller.handleExtractionRequest({
            requestId: "request-1",
            tabId: 101
        });

        await context.controller.handleExtractionRequest({
            requestId: "request-1",
            tabId: 101
        });
        await context.controller.handleExtractionRequest({
            requestId: "request-2",
            tabId: 101
        });

        assert.equal(extractionCount, 1);
        assert.equal(
            context.document.getElementById("extractionStatus").textContent,
            "Recipe extraction is already in progress. Please wait, then click the extension icon again."
        );

        completeExtraction({ success: true, recipe });
        await firstExtraction;

        await context.controller.handleExtractionRequest({
            requestId: "request-1",
            tabId: 101
        });
        assert.equal(extractionCount, 1);
    } finally {
        context.cleanup();
    }
});

test("keeps actions hidden after a content-level extraction failure", async () => {
    const reportedErrors = [];
    const context = await setupSidePanel(
        async () => ({ success: false, error: "No recipe was detected." }),
        { reportError: error => reportedErrors.push(error) }
    );

    try {
        await requestExtraction(context);

        assert.equal(
            context.document.getElementById("extractionStatus").textContent,
            "No recipe was detected. Click the extension icon to try again."
        );
        assert.equal(context.document.getElementById("result").textContent, "");
        assert.equal(context.document.getElementById("editButton").hidden, true);
        assert.equal(context.document.getElementById("printButton").hidden, true);
        assert.equal(context.document.getElementById("saveButton").hidden, true);
        assert.deepEqual(reportedErrors, []);
    } finally {
        context.cleanup();
    }
});

test("renders a successful extraction with title, links, image, ingredients, and steps", async () => {
    const context = await setupSidePanel(async () => ({ success: true, recipe }));

    try {
        await requestExtraction(context);

        const result = context.document.getElementById("result");
        assert.equal(result.querySelector("h3").textContent, "Soup");
        assert.deepEqual(
            [...result.querySelectorAll("a")].map(link => link.href),
            [recipe.sourceUrl, recipe.imageUrl]
        );
        assert.equal(result.querySelector("img").src, recipe.imageUrl);
        assert.deepEqual(
            [...result.querySelectorAll("ul li")].map(item => item.textContent),
            recipe.ingredients
        );
        assert.deepEqual(
            [...result.querySelectorAll("ol li")].map(item => item.textContent),
            recipe.steps
        );
        assert.equal(context.document.getElementById("extractionStatus").textContent, "");
        assert.equal(context.document.getElementById("editButton").hidden, false);
        assert.equal(context.document.getElementById("editButton").disabled, false);
        assert.equal(context.document.getElementById("printButton").hidden, false);
        assert.equal(context.document.getElementById("printButton").disabled, false);
        assert.equal(context.document.getElementById("saveButton").hidden, false);
        assert.equal(context.document.getElementById("saveButton").disabled, true);
        assert.equal(context.document.getElementById("authSection").hidden, false);
    } finally {
        context.cleanup();
    }
});

test("enables all recipe actions after an authenticated extraction", async () => {
    const context = await setupSidePanel(
        async () => ({ success: true, recipe }),
        { initialIsAuthenticated: true }
    );

    try {
        await requestExtraction(context);

        assert.equal(context.document.getElementById("editButton").hidden, false);
        assert.equal(context.document.getElementById("editButton").disabled, false);
        assert.equal(context.document.getElementById("printButton").hidden, false);
        assert.equal(context.document.getElementById("printButton").disabled, false);
        assert.equal(context.document.getElementById("saveButton").hidden, false);
        assert.equal(context.document.getElementById("saveButton").disabled, false);
        assert.equal(context.document.getElementById("authSection").hidden, true);
    } finally {
        context.cleanup();
    }
});

test("shows a green status panel after a successful Cookbook save", async () => {
    const context = await setupSidePanel(
        async () => ({ success: true, recipe }),
        { initialIsAuthenticated: true }
    );

    try {
        await requestExtraction(context);
        await clickAndFlush(context.document.getElementById("saveButton"));

        const saveStatus = context.document.getElementById("saveStatus");

        assert.equal(saveStatus.textContent, "Recipe saved successfully.");
        assert.equal(saveStatus.classList.contains("success"), true);
        assert.equal(saveStatus.classList.contains("error"), false);
        assert.equal(context.document.getElementById("saveButton").disabled, true);
    } finally {
        context.cleanup();
    }
});

test("shows a red status panel after a failed Cookbook save", async () => {
    const saveError = new Error("The recipe already exists.");
    const reportedErrors = [];
    const context = await setupSidePanel(
        async () => ({ success: true, recipe }),
        {
            initialIsAuthenticated: true,
            saveRecipeToApi: async () => {
                throw saveError;
            },
            reportError: error => reportedErrors.push(error)
        }
    );

    try {
        await requestExtraction(context);
        await clickAndFlush(context.document.getElementById("saveButton"));

        const saveStatus = context.document.getElementById("saveStatus");

        assert.equal(
            saveStatus.textContent,
            "The recipe could not be saved. Reason: The recipe already exists."
        );
        assert.equal(saveStatus.classList.contains("error"), true);
        assert.equal(saveStatus.classList.contains("success"), false);
        assert.equal(context.document.getElementById("saveButton").disabled, false);
        assert.deepEqual(reportedErrors, [saveError]);
    } finally {
        context.cleanup();
    }
});

test("an expired session removes the token and reveals login again", async () => {
    const authenticationError = Object.assign(
        new Error("Authentication has expired. Sign in again."),
        { status: 401 }
    );
    const reportedErrors = [];
    let removeAccessTokenCount = 0;
    const context = await setupSidePanel(
        async () => ({ success: true, recipe }),
        {
            initialIsAuthenticated: true,
            saveRecipeToApi: async () => {
                throw authenticationError;
            },
            removeAccessToken: async () => {
                removeAccessTokenCount += 1;
            },
            reportError: error => reportedErrors.push(error)
        }
    );

    try {
        await requestExtraction(context);
        await clickAndFlush(context.document.getElementById("saveButton"));

        assert.equal(removeAccessTokenCount, 1);
        assert.equal(context.document.getElementById("authSection").hidden, false);
        assert.equal(context.document.getElementById("saveButton").disabled, true);
        assert.equal(
            context.document.getElementById("saveStatus").textContent,
            "Your session expired. Sign in again."
        );
        assert.deepEqual(reportedErrors, [authenticationError]);
    } finally {
        context.cleanup();
    }
});

test("a new successful extraction replaces the recipe and resets Save state", async () => {
    const results = [
        { success: true, recipe },
        { success: true, recipe: secondRecipe }
    ];
    const context = await setupSidePanel(
        async () => results.shift(),
        { initialIsAuthenticated: true }
    );

    try {
        await requestExtraction(context, { requestId: "request-1" });
        await clickAndFlush(context.document.getElementById("saveButton"));

        assert.equal(context.document.getElementById("saveButton").disabled, true);
        assert.equal(
            context.document.getElementById("saveStatus").textContent,
            "Recipe saved successfully."
        );

        await requestExtraction(context, { requestId: "request-2", tabId: 102 });

        assert.match(context.document.getElementById("result").textContent, /Salad/);
        assert.doesNotMatch(context.document.getElementById("result").textContent, /Soup/);
        assert.equal(context.document.getElementById("saveStatus").textContent, "");
        assert.equal(context.document.getElementById("saveButton").disabled, false);

        await clickAndFlush(context.document.getElementById("editButton"));
        await clickAndFlush(context.document.getElementById("printButton"));

        assert.deepEqual(context.storage.calls.setItem, [
            ["recipeToPrint", JSON.stringify(secondRecipe)],
            ["recipeToPrint", JSON.stringify(secondRecipe)]
        ]);
    } finally {
        context.cleanup();
    }
});

test("refuses a new extraction while Save is in progress", async () => {
    let extractionCount = 0;
    let completeSave;
    const context = await setupSidePanel(
        async () => {
            extractionCount += 1;
            return { success: true, recipe };
        },
        {
            initialIsAuthenticated: true,
            saveRecipeToApi: async () => new Promise(resolve => {
                completeSave = resolve;
            })
        }
    );

    try {
        await requestExtraction(context, { requestId: "request-1" });
        context.document.getElementById("saveButton").click();
        await Promise.resolve();

        await requestExtraction(context, { requestId: "request-2", tabId: 102 });

        assert.equal(extractionCount, 1);
        assert.match(context.document.getElementById("result").textContent, /Soup/);
        assert.equal(
            context.document.getElementById("extractionStatus").textContent,
            "Wait for the current save to finish, then click the extension icon again."
        );
        assert.equal(context.document.getElementById("editButton").disabled, true);
        assert.equal(context.document.getElementById("printButton").disabled, true);
        assert.equal(context.document.getElementById("saveButton").disabled, true);

        completeSave();
        await Promise.resolve();
        await Promise.resolve();
    } finally {
        context.cleanup();
    }
});

test("a successful login updates the Side Panel to the authenticated state", async () => {
    let loginCount = 0;
    const context = await setupSidePanel(
        async () => ({ success: true, recipe }),
        {
            login: async () => {
                loginCount += 1;
            }
        }
    );

    try {
        await requestExtraction(context);
        await clickAndFlush(context.document.getElementById("loginButton"));

        assert.equal(loginCount, 1);
        assert.match(context.document.getElementById("result").textContent, /Soup/);
        assert.equal(context.document.getElementById("saveButton").disabled, false);
        assert.equal(context.document.getElementById("authSection").hidden, true);
    } finally {
        context.cleanup();
    }
});

test("ignores a second login click while Side Panel authentication is in progress", async () => {
    let loginCount = 0;
    let completeLogin;
    const context = await setupSidePanel(
        async () => ({ success: true, recipe }),
        {
            login: () => {
                loginCount += 1;

                return new Promise(resolve => {
                    completeLogin = resolve;
                });
            }
        }
    );

    try {
        await requestExtraction(context);

        const loginButton = context.document.getElementById("loginButton");

        loginButton.click();
        loginButton.dispatchEvent(new context.window.Event("click", {
            bubbles: true,
            cancelable: true
        }));

        assert.equal(loginCount, 1);
        assert.equal(loginButton.disabled, true);

        completeLogin();
        await Promise.resolve();
        await Promise.resolve();

        assert.equal(context.document.getElementById("authSection").hidden, true);
        assert.equal(context.document.getElementById("saveButton").disabled, false);
    } finally {
        context.cleanup();
    }
});

test("initial authentication can resolve after extraction without blocking public actions", async () => {
    const context = await setupSidePanel(
        async () => ({ success: true, recipe }),
        { initialIsAuthenticationResolved: false }
    );

    try {
        await requestExtraction(context);

        assert.equal(context.document.getElementById("editButton").disabled, false);
        assert.equal(context.document.getElementById("printButton").disabled, false);
        assert.equal(context.document.getElementById("saveButton").disabled, true);
        assert.equal(context.document.getElementById("authSection").hidden, true);

        context.controller.setAuthenticationState(false);

        assert.equal(context.document.getElementById("saveButton").disabled, true);
        assert.equal(context.document.getElementById("authSection").hidden, false);

        context.controller.setAuthenticationState(true);

        assert.equal(context.document.getElementById("saveButton").disabled, false);
        assert.equal(context.document.getElementById("authSection").hidden, true);
    } finally {
        context.cleanup();
    }
});

test("a failed login reports the error and displays a user-facing message", async () => {
    const loginError = new Error("The authentication window was closed.");
    const reportedErrors = [];
    const context = await setupSidePanel(
        async () => ({ success: true, recipe }),
        {
            login: async () => {
                throw loginError;
            },
            reportError: error => reportedErrors.push(error)
        }
    );

    try {
        await requestExtraction(context);
        await clickAndFlush(context.document.getElementById("loginButton"));

        assert.deepEqual(reportedErrors, [loginError]);
        assert.equal(
            context.document.getElementById("authMessage").textContent,
            "Sign in was not completed. Please try again."
        );
        assert.equal(context.document.getElementById("authSection").hidden, false);
        assert.equal(context.document.getElementById("saveButton").disabled, true);
        assert.equal(context.document.getElementById("editButton").disabled, false);
        assert.equal(context.document.getElementById("printButton").disabled, false);
        assert.match(context.document.getElementById("result").textContent, /Soup/);
    } finally {
        context.cleanup();
    }
});

test("a new login attempt clears the previous user-facing error", async () => {
    let loginAttempt = 0;
    let completeSecondAttempt;
    const context = await setupSidePanel(
        async () => ({ success: true, recipe }),
        {
            login: async () => {
                loginAttempt += 1;

                if (loginAttempt === 1) {
                    throw new Error("The first login attempt failed.");
                }

                await new Promise(resolve => {
                    completeSecondAttempt = resolve;
                });
            }
        }
    );

    try {
        const loginButton = context.document.getElementById("loginButton");
        const authMessage = context.document.getElementById("authMessage");

        await requestExtraction(context);
        await clickAndFlush(loginButton);

        assert.equal(
            authMessage.textContent,
            "Sign in was not completed. Please try again."
        );

        loginButton.click();
        await Promise.resolve();

        assert.equal(authMessage.textContent, "Sign in to save recipes.");

        completeSecondAttempt();
        await Promise.resolve();
        await Promise.resolve();
    } finally {
        context.cleanup();
    }
});

test("treats extracted content as text instead of executable HTML", async () => {
    const unsafeRecipe = {
        ...recipe,
        name: '<img id="unsafe-title" src=x>',
        ingredients: ['<script id="unsafe-ingredient">run()</script>'],
        steps: ['<b id="unsafe-step">bold</b>']
    };
    const context = await setupSidePanel(async () => ({ success: true, recipe: unsafeRecipe }));

    try {
        await requestExtraction(context);
        const result = context.document.getElementById("result");

        assert.equal(result.querySelector("#unsafe-title"), null);
        assert.equal(result.querySelector("#unsafe-ingredient"), null);
        assert.equal(result.querySelector("#unsafe-step"), null);
        assert.match(result.textContent, /<img id="unsafe-title"/);
    } finally {
        context.cleanup();
    }
});

test("removes an image that fails and displays the existing message", async () => {
    const context = await setupSidePanel(async () => ({ success: true, recipe }));

    try {
        await requestExtraction(context);
        const image = context.document.querySelector("#result img");
        image.dispatchEvent(new context.window.Event("error"));

        assert.equal(context.document.querySelector("#result img"), null);
        assert.match(
            context.document.getElementById("result").textContent,
            /The image cannot be displayed directly\./
        );
    } finally {
        context.cleanup();
    }
});

test("shows extraction failure while preserving the previously extracted recipe", async () => {
    const results = [
        { success: true, recipe },
        { success: false, error: "No recipe here." }
    ];
    const context = await setupSidePanel(async () => results.shift());

    try {
        await requestExtraction(context, { requestId: "request-1" });
        await requestExtraction(context, { requestId: "request-2" });
        await clickAndFlush(context.document.getElementById("editButton"));

        const extractionStatus = context.document.getElementById("extractionStatus");

        assert.match(context.document.getElementById("result").textContent, /Soup/);
        assert.equal(
            extractionStatus.textContent,
            "No recipe here. Click the extension icon to try again."
        );
        assert.equal(extractionStatus.classList.contains("error"), true);
        assert.equal(context.document.getElementById("saveStatus").textContent, "");
        assert.equal(context.document.getElementById("extractButton"), null);
        assert.equal(context.document.getElementById("editButton").hidden, false);
        assert.equal(context.document.getElementById("printButton").hidden, false);
        assert.equal(context.document.getElementById("saveButton").hidden, false);
        assert.equal(context.document.getElementById("authSection").hidden, false);
        assert.equal(context.document.getElementById("resultSeparator").hidden, false);
        assert.deepEqual(context.storage.calls.setItem, [
            ["recipeToPrint", JSON.stringify(recipe)]
        ]);
        assert.deepEqual(context.browser.calls.openWindow, [
            { page: "edit.html", type: "popup", width: 900, height: 700 }
        ]);
    } finally {
        context.cleanup();
    }
});

test("reports a rejected extraction and restores the failure state", async () => {
    const extractionError = new Error("Cannot inject into this tab.");
    const reportedErrors = [];
    const context = await setupSidePanel(
        async () => { throw extractionError; },
        {
            reportError: error => reportedErrors.push(error)
        }
    );

    try {
        await requestExtraction(context);

        assert.deepEqual(reportedErrors, [extractionError]);
        assert.equal(
            context.document.getElementById("extractionStatus").textContent,
            "This page cannot be accessed. Open a regular web page and click the extension icon."
        );
        assert.equal(context.document.getElementById("result").textContent, "");
        assert.equal(
            context.document.getElementById("extractionStatus").classList.contains("error"),
            true
        );
        assert.equal(context.document.getElementById("extractButton"), null);
        assert.equal(context.document.getElementById("editButton").hidden, true);
        assert.equal(context.document.getElementById("printButton").hidden, true);
        assert.equal(context.document.getElementById("saveButton").hidden, true);
        assert.equal(context.document.getElementById("authSection").hidden, true);
        assert.equal(context.document.getElementById("resultSeparator").hidden, true);
        assert.equal(context.storage.calls.setItem.length, 0);
        assert.equal(context.browser.calls.openWindow.length, 0);
    } finally {
        context.cleanup();
    }
});

test("Edit and Print do nothing before a recipe is extracted", async () => {
    const context = await setupSidePanel(async () => ({ success: false, error: "unused" }));

    try {
        await clickAndFlush(context.document.getElementById("editButton"));
        await clickAndFlush(context.document.getElementById("printButton"));

        assert.equal(context.storage.calls.setItem.length, 0);
        assert.equal(context.browser.calls.openWindow.length, 0);
    } finally {
        context.cleanup();
    }
});

test("Edit and Print save the payload and open 900x700 popup windows", async () => {
    const context = await setupSidePanel(async () => ({ success: true, recipe }));

    try {
        await requestExtraction(context);
        await clickAndFlush(context.document.getElementById("editButton"));
        await clickAndFlush(context.document.getElementById("printButton"));

        assert.deepEqual(
            context.storage.calls.setItem,
            [
                ["recipeToPrint", JSON.stringify(recipe)],
                ["recipeToPrint", JSON.stringify(recipe)]
            ]
        );
        assert.deepEqual(context.browser.calls.openWindow, [
            { page: "edit.html", type: "popup", width: 900, height: 700 },
            { page: "print.html", type: "popup", width: 900, height: 700 }
        ]);
        assert.match(context.document.getElementById("result").textContent, /Soup/);
    } finally {
        context.cleanup();
    }
});

test("extracts from the requested tab using the production Chrome API sequence", async () => {
    const extractionResult = { success: true, recipe };
    const extractorFiles = [
        "extractors/jsonLdImporter.js",
        "extractors/htmlImporter.js",
        "extractors/recipeExtractor.js"
    ];
    const chromeMock = createChromeMock({
        tabs: [{ id: 321 }],
        executeScriptResults: [[], [{ result: extractionResult }]]
    });

    const reportedExtractions = [];

    const result = await extractRecipeFromTab({
        chrome: chromeMock.chrome,
        extractorFiles,
        tabId: 321,
        reportExtraction: value => reportedExtractions.push(value)
    });

    assert.deepEqual(result, extractionResult);
    assert.deepEqual(reportedExtractions, [extractionResult]);
    assert.deepEqual(chromeMock.calls.tabsQuery, []);
    assert.equal(chromeMock.calls.executeScript.length, 2);
    assert.deepEqual(chromeMock.calls.executeScript[0], {
        target: { tabId: 321 },
        files: extractorFiles
    });
    assert.deepEqual(chromeMock.calls.executeScript[1].target, { tabId: 321 });
    assert.equal(typeof chromeMock.calls.executeScript[1].func, "function");
});

test("rejects an invalid requested tab id before injecting", async () => {
    const chromeMock = createChromeMock();

    await assert.rejects(
        extractRecipeFromTab({
            chrome: chromeMock.chrome,
            extractorFiles: [],
            tabId: -1
        }),
        /tabId must be a non-negative integer/
    );

    assert.deepEqual(chromeMock.calls.executeScript, []);
});

test("rejects when script execution returns no extraction result", async () => {
    const chromeMock = createChromeMock({
        executeScriptResults: [[], []]
    });

    await assert.rejects(
        extractRecipeFromTab({
            chrome: chromeMock.chrome,
            extractorFiles: ["extractor.js"],
            tabId: 321
        }),
        /did not return a result/
    );
});

test("the real classic Side Panel bootstrap preserves injection and dependency composition", async () => {
    const [html, script] = await Promise.all([
        readFile(sidePanelHtmlPath, "utf8"),
        readFile(sidePanelScriptPath, "utf8")
    ]);

    assert.match(html, /<script\s+src="sidepanel\.js"><\/script>/);
    assert.doesNotMatch(html, /<script[^>]+type="module"[^>]+src="sidepanel\.js"/);
    assert.doesNotMatch(html, /width:\s*468px/);
    assert.doesNotMatch(html, /popup-(?:header|icon|description)/);
    assert.match(html, /class="side-panel-header"/);
    assert.match(html, /id="extractionStatus"[^>]+role="status"[^>]+aria-live="polite"/);
    assert.doesNotMatch(html, /id="extractButton"/);
    assert.match(script, /import\("\.\/controllers\/sidePanelController\.js"\)\.then/);
    assert.match(script, /import\("\.\/api\/recipeApiClient\.js"\)/);
    assert.match(script, /extractRecipeFromTab/);
    assert.match(script, /initializeSidePanelController\s*\(/);
    assert.match(script, /localStorage\.setItem\("recipeToPrint"/);
    assert.match(script, /const reportError\s*=\s*error\s*=>/);
    assert.match(script, /resolveAuthenticationState\s*\(/);
    assert.match(script, /url:\s*chrome\.runtime\.getURL\(page\)/);
    assert.match(script, /openWindow:\s*\(\{\s*page,\s*type,\s*width,\s*height\s*}\)\s*=>/);
    assert.match(script, /chrome\.windows\.create\(\{[\s\S]*?type,[\s\S]*?width,[\s\S]*?height/);

    const jsonLdIndex = script.indexOf('"extractors/jsonLdImporter.js"');
    const htmlIndex = script.indexOf('"extractors/htmlImporter.js"');
    const recipeIndex = script.indexOf('"extractors/recipeExtractor.js"');

    assert.ok(jsonLdIndex >= 0 && jsonLdIndex < htmlIndex);
    assert.ok(htmlIndex < recipeIndex);
});
