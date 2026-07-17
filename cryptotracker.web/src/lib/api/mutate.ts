import { toast } from 'svelte-sonner';

const statusMessages: Record<number, string> = {
	400: 'Invalid input.',
	401: 'You are not signed in.',
	403: 'You are not allowed to do that.',
	404: 'Not found.',
	409: 'This entry already exists.',
	500: 'Server error — please try again.'
};

/**
 * Wraps a mutating API call: shows a success toast, maps errors to readable
 * toasts, and returns null on failure so callers can simply bail out.
 */
export async function mutate<T>(
	fn: () => Promise<{ status: number; data: T }>,
	opts: { success: string; onSuccess?: (data: T) => void | Promise<void> }
): Promise<T | null> {
	try {
		const res = await fn();
		if (res.status >= 200 && res.status < 300) {
			toast.success(opts.success);
			await opts.onSuccess?.(res.data);
			return res.data;
		}
		const detail = typeof res.data === 'string' && res.data ? res.data : undefined;
		toast.error(detail ?? statusMessages[res.status] ?? `Request failed (${res.status}).`);
		return null;
	} catch {
		toast.error('Network error — check your connection.');
		return null;
	}
}
