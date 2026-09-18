import { API_ENDPOINTS } from "../config/apiConfig.js";

export const currentUserApiUrl = API_ENDPOINTS.currentUser;

export async function getCurrentUser(
    accessToken,
    fetchRequest = globalThis.fetch
) {
    if (!accessToken) {
        return null;
    }

    const response = await fetchRequest(currentUserApiUrl, {
        method: "GET",
        headers: {
            "Authorization": `Bearer ${accessToken}`
        }
    });

    if (response.status === 401) {
        return null;
    }

    if (!response.ok) {
        throw new Error(
            `Authentication check failed with status ${response.status}.`
        );
    }

    return await response.json();
}

export const exchangeAuthenticationCodeApiUrl = API_ENDPOINTS.exchangeCode;

export async function exchangeAuthenticationCode(
    code,
    fetchRequest = globalThis.fetch
) {
    if (!code) {
        throw new Error("Authentication code is required.");
    }

    const response = await fetchRequest(
        exchangeAuthenticationCodeApiUrl,
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                code
            })
        }
    );

    const responseBody = await response.json();

    if (!response.ok) {
        throw new Error(
            responseBody.message
            ?? `Authentication code exchange failed with status ${response.status}.`
        );
    }

    return responseBody.accessToken;
}
