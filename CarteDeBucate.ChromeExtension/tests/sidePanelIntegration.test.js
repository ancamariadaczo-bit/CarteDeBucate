import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import { API_BASE_URL } from "../config/apiConfig.js";

const manifestPath = new URL("../manifest.json", import.meta.url);
const serviceWorkerPath = new URL("../service-worker.js", import.meta.url);
const sidePanelHtmlPath = new URL("../sidepanel.html", import.meta.url);
const sidePanelCssPath = new URL("../sidepanel.css", import.meta.url);
const sidePanelScriptPath = new URL("../sidepanel.js", import.meta.url);

test("manifest declares the Side Panel contract without broad page access", async () => {
    const manifest = JSON.parse(await readFile(manifestPath, "utf8"));

    assert.equal(manifest.minimum_chrome_version, "116");
    assert.equal(manifest.side_panel?.default_path, "sidepanel.html");
    assert.equal(manifest.action?.default_title, "Extract recipe");
    assert.equal(Object.hasOwn(manifest.action, "default_popup"), false);
    assert.equal(manifest.permissions.includes("sidePanel"), true);
    assert.equal(manifest.permissions.includes("activeTab"), true);
    assert.equal(manifest.permissions.includes("scripting"), true);
    assert.equal(manifest.permissions.includes("storage"), true);
    assert.equal(manifest.permissions.includes("tabs"), false);
    assert.deepEqual(
        manifest.host_permissions,
        [`${new URL(API_BASE_URL).origin}/*`]
    );
    assert.equal(manifest.host_permissions.includes("<all_urls>"), false);
    assert.equal(manifest.host_permissions.includes("http://*/*"), false);
    assert.equal(manifest.host_permissions.includes("https://*/*"), false);
});

test("service worker wires the action launcher while preserving LOGIN handling", async () => {
    const source = await readFile(serviceWorkerPath, "utf8");

    assert.match(source, /createExtractionRequestStore/);
    assert.match(source, /initializeSidePanelLauncher/);
    assert.match(source, /chrome\.storage\.session/);
    assert.match(source, /chrome\.action\.onClicked\.addListener/);
    assert.match(source, /chrome\.sidePanel\.open/);
    assert.match(source, /crypto\.randomUUID\(\)/);
    assert.match(source, /message\.type\s*!==\s*"LOGIN"/);
    assert.doesNotMatch(source, /setPanelBehavior/);
});

test("Side Panel bootstrap synchronizes extraction and authentication storage", async () => {
    const source = await readFile(sidePanelScriptPath, "utf8");
    const listenerIndex = source.indexOf(
        "chrome.storage.onChanged.addListener(handleStorageChange)"
    );
    const initialConsumptionIndex = source.indexOf(
        "const initialConsumption = consumePendingRequests()"
    );
    const authenticationIndex = source.lastIndexOf(
        "resolveAuthenticationState({"
    );
    const awaitInitialConsumptionIndex = source.indexOf(
        "await initialConsumption"
    );

    assert.ok(listenerIndex >= 0);
    assert.ok(initialConsumptionIndex > listenerIndex);
    assert.ok(authenticationIndex > initialConsumptionIndex);
    assert.ok(awaitInitialConsumptionIndex > authenticationIndex);
    assert.match(source, /chrome\.windows\.getCurrent\(\)/);
    assert.match(source, /areaName\s*!==\s*"session"/);
    assert.match(source, /getAuthenticationStateFromStorageChange\(changes, areaName\)/);
    assert.match(source, /authenticationStateRevision\s*\+=\s*1/);
    assert.match(source, /controller\.setAuthenticationState\(isAuthenticated\)/);
    assert.match(source, /authenticationStateRevision\s*===\s*initialAuthenticationRevision/);
    assert.match(source, /forwardedRequestIds\.has\(request\.requestId\)/);
    assert.match(source, /key\.startsWith\(requestStorageKeyPrefix\)/);
    assert.match(source, /controller\.handleExtractionRequest\(request\)/);
    assert.match(source, /chrome\.storage\.onChanged\.removeListener/);
    assert.doesNotMatch(source, /setTimeout\s*\(/);
});

test("Side Panel markup and CSS are responsive and have no extraction button", async () => {
    const [html, css] = await Promise.all([
        readFile(sidePanelHtmlPath, "utf8"),
        readFile(sidePanelCssPath, "utf8")
    ]);

    assert.doesNotMatch(html, /id="extractButton"/);
    assert.doesNotMatch(html, />\s*(?:Extract Recipe|Extract again|Retry)\s*</i);
    assert.doesNotMatch(html, /Create account/i);
    assert.doesNotMatch(html, /width:\s*468px/);
    assert.match(css, /box-sizing:\s*border-box/);
    assert.match(css, /width:\s*100%/);
    assert.match(css, /flex-wrap:\s*wrap/);
    assert.match(css, /\.extraction-status\.error/);
    assert.doesNotMatch(css, /width:\s*420px/);
    assert.doesNotMatch(css, /@media\s+print/);
    assert.doesNotMatch(css, /\.popup-(?:header|icon|description)/);
});
