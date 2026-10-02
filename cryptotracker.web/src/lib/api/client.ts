import { goto } from "$app/navigation";
import { resolve } from "$app/paths";
import * as api from "#lib/cryptotrackerApi.js";
import { defaults } from "#lib/cryptotrackerApi.js";
import { loadConfig } from "#lib/stores/config.js";
import { user } from "#lib/stores/user.js";
import { get } from "svelte/store";
import { toast } from "svelte-sonner";

export function loginPath(returnUrl?: string): string {
	const base = resolve("auth/login");

	return returnUrl ? `${base}?returnUrl=${encodeURIComponent(returnUrl)}` : base;
}

/** Fetches /auth/me and populates the user + config stores. */
export async function refreshUser(): Promise<boolean> {
	try {
		const res = await api.getMe();
		if (res.status === 200) {
			user.set(res.data);
			loadConfig();
			return true;
		}
	} catch {
		// fall through — treated as signed out
	}
	user.set(null);
	return false;
}

let installed = false;
let redirectingToLogin = false;

function requestUrl(input: RequestInfo | URL): string {
	return typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
}

function handleUnauthorized(res: Response, url: string) {
	if (res.status !== 401 || url.includes("/api/Auth/") || redirectingToLogin) return;
	if (typeof window === "undefined") return;

	redirectingToLogin = true;
	const wasSignedIn = get(user) !== null;
	user.set(null);
	if (wasSignedIn) {
		toast.info("Your session has expired — please sign in again.");
	}
	const path = window.location.pathname;
	const returnUrl = path.startsWith("/auth/") ? undefined : path + window.location.search;
	goto(loginPath(returnUrl)).finally(() => {
		redirectingToLogin = false;
	});
}

const loadFetches: Array<typeof fetch> = [];

// Installed at import time: load functions run before the layout mounts.
defaults.fetch = async (input, init) => {
	const fetchFn = loadFetches.at(-1) ?? globalThis.fetch.bind(globalThis);
	const res = await fetchFn(input, init);
	handleUnauthorized(res, requestUrl(input));
	return res;
};

/**
 * Run `fn` with SvelteKit's load `fetch` as the API client fetch.
 * Call API functions inside `fn`. Their request starts immediately, so the
 * Kit fetch is used even if the returned promise is streamed to the page.
 */
export function runInLoad<T>(fetchFn: typeof fetch, fn: () => T): T {
	loadFetches.push(fetchFn);
	try {
		return fn();
	} finally {
		loadFetches.pop();
	}
}

/**
 * Central 401 handling. Keyed on the *requested endpoint*, not on the current
 * page: during SPA navigation the load functions (and their 401s) run before
 * the URL changes, so checking window.location would swallow the 401.
 * /api/Auth/ endpoints are exempt — a failed login attempt is a normal error,
 * not an expired session.
 */
export function installAuthInterceptor() {
	if (installed) return;
	installed = true;
}
