import type { MeResponse } from '$lib/cryptotrackerApi';
import { writable } from 'svelte/store';
export const user = writable<MeResponse | null>(null);