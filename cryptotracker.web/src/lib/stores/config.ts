import { writable } from 'svelte/store';

// display form of the backend's base currency (e.g. "CHF"), default until loaded
export const baseCurrency = writable<string>('CHF');

// plain fetch instead of the generated client so this works before `make api`
// regenerates cryptotrackerApi.ts with the new /api/config endpoint
export async function loadConfig() {
	try {
		const res = await fetch('/api/config');
		if (res.ok) {
			const data = await res.json();
			if (data?.baseCurrency) {
				baseCurrency.set(String(data.baseCurrency).toUpperCase());
			}
		}
	} catch {
		// keep default; UI labels just fall back to CHF
	}
}
