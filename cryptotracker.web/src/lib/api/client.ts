import { goto } from '$app/navigation';
import { resolve } from '$app/paths';
import * as api from '$lib/cryptotrackerApi';
import { defaults } from '$lib/cryptotrackerApi';
import { loadConfig } from '$lib/stores/config';
import { user } from '$lib/stores/user';
import { get } from 'svelte/store';
import { toast } from 'svelte-sonner';

export function loginPath(returnUrl?: string): string {
	const base = resolve('/auth/login');
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
	const baseFetch = globalThis.fetch.bind(globalThis);
	defaults.fetch = async (input, init) => {
		const res = await baseFetch(input, init);
		const url = typeof input === 'string' ? input : input instanceof URL ? input.href : input.url;
		if (res.status === 401 && !url.includes('/api/Auth/') && !redirectingToLogin) {
			redirectingToLogin = true;
			const wasSignedIn = get(user) !== null;
			user.set(null);
			if (wasSignedIn) {
				toast.info('Your session has expired — please sign in again.');
			}
			const path = window.location.pathname;
			const returnUrl = path.startsWith('/auth/') ? undefined : path + window.location.search;
			// eslint-disable-next-line svelte/no-navigation-without-resolve -- loginPath builds on resolve()
			goto(loginPath(returnUrl)).finally(() => {
				redirectingToLogin = false;
			});
		}
		return res;
	};
}
