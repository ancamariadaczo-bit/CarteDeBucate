const ACCESS_TOKEN_KEY = "accessToken";

export async function getAccessToken() {
    const result = await chrome.storage.local.get(ACCESS_TOKEN_KEY);

    return result[ACCESS_TOKEN_KEY] ?? null;
}

export async function saveAccessToken(token) {
    await chrome.storage.local.set({
        [ACCESS_TOKEN_KEY]: token
    });
}

export async function removeAccessToken() {
    await chrome.storage.local.remove(ACCESS_TOKEN_KEY);
}

export function getAuthenticationStateFromStorageChange(changes, areaName) {
    if (
        areaName !== "local"
        || !changes
        || typeof changes !== "object"
        || !Object.hasOwn(changes, ACCESS_TOKEN_KEY)
    ) {
        return null;
    }

    const accessToken = changes[ACCESS_TOKEN_KEY]?.newValue;

    return typeof accessToken === "string"
        && accessToken.trim().length > 0;
}
