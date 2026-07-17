import { goto } from '$app/navigation';
import { resolve } from '$app/paths';
import * as api from '$lib/cryptotrackerApi';
import { defaults } from '$lib/cryptotrackerApi';
import { loadConfig } from '$lib/stores/config';
import { user } from '$lib/stores/user';
import { toast } from 'svelte-sonner';

export function loginPath(returnUrl?: string): string {
	const base = resolve('/auth/login');
	return returnUrl ? `${base}?returnUrl=${encodeURIComponent(returnUrl)}` : base;
}

/** Fetches /auth/me and populates the user + config stores. */
export async function refreshUser(): Promise<boolean> {
	// The startup check must not trigger the interceptor's "session expired"
	// toast — a first-time visitor simply has no session yet.
	silentAuthCheck = true;
	try {
		const res = await api.getMe();
		if (res.status === 200) {
			user.set(res.data);
			loadConfig();
			return true;
		}
	} catch {
		// fall through — treated as signed out
	} finally {
		silentAuthCheck = false;
	}
	user.set(null);
	return false;
}

let installed = false;
let redirectingToLogin = false;
let silentAuthCheck = false;

/**
 * Central 401 handling: any API response with 401 (e.g. an expired JWT)
 * clears the user and redirects to login, carrying the current location as
 * returnUrl so the user comes back to where they were.
 */
export function installAuthInterceptor() {
	if (installed) return;
	installed = true;
	const baseFetch = globalThis.fetch.bind(globalThis);
	defaults.fetch = async (input, init) => {
		const res = await baseFetch(input, init);
		if (
			res.status === 401 &&
			!silentAuthCheck &&
			!window.location.pathname.startsWith('/auth/') &&
			!redirectingToLogin
		) {
			redirectingToLogin = true;
			user.set(null);
			toast.info('Your session has expired — please sign in again.');
			// eslint-disable-next-line svelte/no-navigation-without-resolve -- loginPath builds on resolve()
			goto(loginPath(window.location.pathname + window.location.search)).finally(() => {
				redirectingToLogin = false;
			});
		}
		return res;
	};
}
