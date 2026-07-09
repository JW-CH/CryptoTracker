import * as api from '$lib/cryptotrackerApi';
import { writable } from 'svelte/store';

// display form of the backend's base currency (e.g. "CHF"), default until loaded
export const baseCurrency = writable<string>('CHF');

export async function loadConfig() {
	try {
		const res = await api.getConfig();
		if (res.status === 200 && res.data.baseCurrency) {
			baseCurrency.set(res.data.baseCurrency.toUpperCase());
		}
	} catch {
		// keep default; UI labels just fall back to CHF
	}
}
