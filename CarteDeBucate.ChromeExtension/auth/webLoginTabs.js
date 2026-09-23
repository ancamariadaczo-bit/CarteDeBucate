export async function refreshWebLoginTabs({
    queryTabs,
    reloadTab,
    loginPageUrl
}) {
    const expectedLoginUrl = new URL(loginPageUrl);
    const tabs = await queryTabs({});

    const reloadOperations = tabs
        .filter(tab => isWebLoginTab(tab, expectedLoginUrl))
        .map(tab => reloadTab(tab.id));

    await Promise.all(reloadOperations);
}

function isWebLoginTab(tab, expectedLoginUrl) {
    if (!Number.isInteger(tab.id) || !tab.url) {
        return false;
    }

    try {
        const tabUrl = new URL(tab.url);

        return tabUrl.origin === expectedLoginUrl.origin
            && normalizePath(tabUrl.pathname) === normalizePath(expectedLoginUrl.pathname);
    } catch {
        return false;
    }
}

function normalizePath(path) {
    return path.replace(/\/+$/, "").toLowerCase();
}
