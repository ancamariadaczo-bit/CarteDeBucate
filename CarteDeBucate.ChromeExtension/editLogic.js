export function isValidHttpUrl(value) {
    if (typeof value !== "string") {
        return false;
    }

    let url;

    try {
        url = new URL(value.trim());
    } catch {
        return false;
    }

    const isHttpOrHttps =
        url.protocol === "http:" ||
        url.protocol === "https:";

    return isHttpOrHttps && Boolean(url.hostname.trim());
}

export function normalizeNonEmptyValues(values) {
    if (!Array.isArray(values)) {
        return [];
    }

    return values
        .filter(value => typeof value === "string")
        .map(value => value.trim())
        .filter(value => value.length > 0);
}

export function normalizeInstruction(value) {
    if (typeof value !== "string") {
        return "";
    }

    return value
        .split(/\r?\n/)
        .filter(line => line.trim().length > 0)
        .join("\n")
        .trim();
}

export function normalizeInstructions(values) {
    if (!Array.isArray(values)) {
        return [];
    }

    return values
        .map(normalizeInstruction)
        .filter(value => value.length > 0);
}

export function parseRecipePayload(recipeJson) {
    if (typeof recipeJson !== "string" || !recipeJson) {
        return null;
    }

    let recipe;

    try {
        recipe = JSON.parse(recipeJson);
    } catch {
        return null;
    }

    if (!recipe || typeof recipe !== "object" || Array.isArray(recipe)) {
        return null;
    }

    return recipe;
}

export function validateAndBuildEditedRecipe(originalRecipe, formValues) {
    const name = typeof formValues?.name === "string"
        ? formValues.name.trim()
        : "";

    const sourceUrl = typeof formValues?.sourceUrl === "string"
        ? formValues.sourceUrl.trim()
        : "";

    const imageUrl = typeof formValues?.imageUrl === "string"
        ? formValues.imageUrl.trim()
        : "";

    const ingredients = normalizeNonEmptyValues(formValues?.ingredients);
    const steps = normalizeInstructions(formValues?.steps);
    const errors = [];

    if (!name) {
        errors.push("Recipe name is required.");
    }

    if (!sourceUrl) {
        errors.push("Source URL is required.");
    } else if (!isValidHttpUrl(sourceUrl)) {
        errors.push("Source URL must be a valid HTTP or HTTPS URL.");
    }

    if (imageUrl && !isValidHttpUrl(imageUrl)) {
        errors.push("Image URL must be a valid HTTP or HTTPS URL.");
    }

    if (ingredients.length === 0) {
        errors.push("At least one ingredient is required.");
    }

    if (steps.length === 0) {
        errors.push("At least one instruction is required.");
    }

    if (errors.length > 0) {
        return {
            success: false,
            errors
        };
    }

    return {
        success: true,
        errors: [],
        recipe: {
            ...originalRecipe,
            name,
            sourceUrl,
            imageUrl: imageUrl || null,
            ingredients,
            steps
        }
    };
}
