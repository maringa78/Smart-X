const themeStorageKey = "smartx-theme";

window.smartXTheme = {
    get() {
        try {
            return window.localStorage.getItem(themeStorageKey);
        } catch {
            return null;
        }
    },
    set(theme) {
        const normalizedTheme = theme === "dark" ? "dark" : "light";
        document.documentElement.setAttribute("data-theme", normalizedTheme);

        try {
            window.localStorage.setItem(themeStorageKey, normalizedTheme);
        } catch {
        }

        return normalizedTheme;
    }
};

document.documentElement.setAttribute("data-theme", window.smartXTheme.get() === "dark" ? "dark" : "light");
