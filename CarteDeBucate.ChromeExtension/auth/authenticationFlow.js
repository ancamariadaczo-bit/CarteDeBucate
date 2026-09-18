export async function runAuthenticationFlow({
    launchWebAuthFlow,
    exchangeAuthenticationCode,
    saveAccessToken,
    loginUrl
}) {
    const finalUrl = await launchWebAuthFlow({
        url: loginUrl,
        interactive: true
    });

    if (!finalUrl) {
        throw new Error("Authentication did not complete.");
    }

    let callbackUrl;

    try {
        callbackUrl = new URL(finalUrl);
    } catch {
        throw new Error("Authentication callback URL is invalid.");
    }

    const code = callbackUrl.searchParams.get("code");

    if (!code) {
        throw new Error("Authentication code was not returned.");
    }

    const accessToken = await exchangeAuthenticationCode(code);

    await saveAccessToken(accessToken);
}
