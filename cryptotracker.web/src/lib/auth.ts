// src/lib/auth.ts
import { user } from '$lib/stores/user';

async function fetchUserLogin(): Promise<void> {
    try {
        const res = await fetch('/user/me', { cache: 'no-store' });
        if (!res.ok) {
            user.set({ authenticated: false });
            return;
        }
        const data = await res.json();
        // The /user/me endpoint should return at least { authenticated: boolean, ... }
        user.set(data);
    } catch {
        user.set({ authenticated: false });
    }
}

export const auth = {
    fetchUserLogin
};
