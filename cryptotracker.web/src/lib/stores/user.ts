import type { MeResponse } from "#lib/cryptotrackerApi.js";
import { writable } from "svelte/store";
export const user = writable<MeResponse | null>(null);
