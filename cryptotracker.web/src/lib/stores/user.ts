import { writable } from 'svelte/store';

export type UiUser = {
    authenticated: boolean;
    userId?: string;
    name?: string;
    email?: string;
    picture?: string;
    roles?: string[];
};

export const user = writable<UiUser>({ authenticated: false });
