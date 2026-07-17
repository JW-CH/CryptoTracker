export type Theme = "light" | "dark" | "system";

const STORAGE_KEY = "theme";
const prefersDark = window.matchMedia("(prefers-color-scheme: dark)");

function apply(theme: Theme) {
	const dark = theme === "dark" || (theme === "system" && prefersDark.matches);
	document.documentElement.classList.toggle("dark", dark);
	document.documentElement.style.colorScheme = dark ? "dark" : "light";
}

const stored = localStorage.getItem(STORAGE_KEY);
let current = $state<Theme>(stored === "light" || stored === "dark" ? stored : "system");

apply(current);
prefersDark.addEventListener("change", () => {
	if (current === "system") apply("system");
});

export const theme = {
	get resolved(): "light" | "dark" {
		return current === "system" ? (prefersDark.matches ? "dark" : "light") : current;
	},
	get value() {
		return current;
	},
	set value(theme: Theme) {
		current = theme;
		localStorage.setItem(STORAGE_KEY, theme);
		apply(theme);
	}
};
