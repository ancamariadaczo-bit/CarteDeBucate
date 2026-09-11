import { readFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";

export async function loadClassicScript(window, scriptPath) {
    const source = await readFile(scriptPath, "utf8");
    const filename = scriptPath instanceof URL
        ? fileURLToPath(scriptPath)
        : scriptPath;

    window.eval(`${source}\n//# sourceURL=${filename}`);

    return window.RecipeClipper;
}
