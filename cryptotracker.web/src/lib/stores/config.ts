import * as api from "$lib/cryptotrackerApi";
import { writable } from "svelte/store";

// display form of the backend's base currency (e.g. "CHF"), default until loaded
export const baseCurrency = writable<string>("CHF");

// backend sync interval; drives the staleness threshold on integration tiles
export const updateIntervalMinutes = writable<number>(60);

export async function loadConfig() {
	try {
		const res = await api.getConfig();
		if (res.status === 200) {
			if (res.data.baseCurrency) baseCurrency.set(res.data.baseCurrency.toUpperCase());

			if (res.data.updateIntervalMinutes) updateIntervalMinutes.set(res.data.updateIntervalMinutes);
		}
	} catch {
		// keep defaults; UI labels just fall back to CHF
	}
}
