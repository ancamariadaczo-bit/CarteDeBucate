import { readFile } from "node:fs/promises";
import { JSDOM } from "jsdom";

export function createDom({
    html = "<!DOCTYPE html><html><head></head><body></body></html>",
    url = "https://recipes.example.test/current-recipe",
    runScripts = false
} = {}) {
    const options = { url };

    if (runScripts) {
        options.runScripts = runScripts === true
            ? "outside-only"
            : runScripts;
    }

    const dom = new JSDOM(html, options);

    return {
        dom,
        window: dom.window,
        document: dom.window.document,
        cleanup() {
            dom.window.close();
        }
    };
}

export async function createDomFromFile(filePath, options = {}) {
    const html = await readFile(filePath, "utf8");

    return createDom({
        ...options,
        html
    });
}
