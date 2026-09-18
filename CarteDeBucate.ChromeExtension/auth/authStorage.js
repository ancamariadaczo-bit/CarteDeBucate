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