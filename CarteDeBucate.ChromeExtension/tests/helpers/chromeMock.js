export function createChromeMock({
    tabs = [{ id: 101 }],
    executeScriptResults = []
} = {}) {
    const queuedExecutionResults = [...executeScriptResults];
    const calls = {
        tabsQuery: [],
        executeScript: [],
        windowsCreate: [],
        runtimeGetUrl: []
    };

    const chrome = {
        tabs: {
            async query(queryInfo) {
                calls.tabsQuery.push(queryInfo);
                return tabs;
            }
        },
        scripting: {
            async executeScript(details) {
                calls.executeScript.push(details);
                return queuedExecutionResults.shift() ?? [];
            }
        },
        windows: {
            async create(details) {
                calls.windowsCreate.push(details);
                return { id: calls.windowsCreate.length, ...details };
            }
        },
        runtime: {
            getURL(path) {
                calls.runtimeGetUrl.push(path);
                return `chrome-extension://test-extension/${path}`;
            }
        }
    };

    return { chrome, calls };
}

export function createStorageMock(initialValues = {}) {
    const values = new Map(Object.entries(initialValues));
    const calls = {
        getItem: [],
        setItem: [],
        removeItem: []
    };

    return {
        storage: {
            getItem(key) {
                calls.getItem.push(key);
                return values.has(key) ? values.get(key) : null;
            },
            setItem(key, value) {
                calls.setItem.push([key, value]);
                values.set(key, String(value));
            },
            removeItem(key) {
                calls.removeItem.push(key);
                values.delete(key);
            }
        },
        calls,
        values
    };
}

export function createBrowserEffectsMock() {
    const calls = {
        navigate: [],
        close: 0,
        print: 0,
        openWindow: []
    };

    return {
        effects: {
            navigate(url) {
                calls.navigate.push(url);
            },
            closeWindow() {
                calls.close += 1;
            },
            requestPrint() {
                calls.print += 1;
            },
            async openWindow(details) {
                calls.openWindow.push(details);
                return { id: calls.openWindow.length, ...details };
            }
        },
        calls
    };
}

export function createControlledTimers() {
    let nextId = 1;
    const tasks = new Map();
    const calls = {
        scheduled: [],
        cleared: []
    };

    function setTimeout(callback, delay) {
        const id = nextId++;
        tasks.set(id, { id, callback, delay });
        calls.scheduled.push({ id, delay });
        return id;
    }

    function clearTimeout(id) {
        calls.cleared.push(id);
        tasks.delete(id);
    }

    function runTask(task) {
        if (!tasks.has(task.id)) {
            return false;
        }

        tasks.delete(task.id);
        task.callback();
        return true;
    }

    function runNext() {
        const task = [...tasks.values()]
            .sort((left, right) => left.delay - right.delay || left.id - right.id)[0];

        return task ? runTask(task) : false;
    }

    function runByDelay(delay) {
        const matchingTasks = [...tasks.values()]
            .filter(task => task.delay === delay)
            .sort((left, right) => left.id - right.id);

        for (const task of matchingTasks) {
            runTask(task);
        }

        return matchingTasks.length;
    }

    function runAll() {
        while (runNext()) {
            // Tasks may schedule more controlled tasks.
        }
    }

    return {
        setTimeout,
        clearTimeout,
        runNext,
        runByDelay,
        runAll,
        pending: () => [...tasks.values()],
        calls
    };
}
