export async function resolveAuthenticationState({
    getAccessToken,
    getCurrentUser,
    removeAccessToken,
    reportError
}) {
    try {
        const accessToken = await getAccessToken();

        if (!accessToken) {
            return false;
        }

        const currentUser = await getCurrentUser(accessToken);

        if (currentUser) {
            return true;
        }

        await removeAccessToken();

        return false;
    } catch (error) {
        reportError(error);

        return false;
    }
}
