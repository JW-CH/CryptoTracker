/**
 * Consume a promise streamed from a load function without `{#await}`.
 * `{#await}` calls `flushSync` when the promise resolves, which reads deriveds
 * of a page that may already have been destroyed.
 * `null` means still loading.
 */
export function streamed<T>(getPromise: () => Promise<T>) {
	let value = $state<T | null>(null);
	let failed = $state(false);

	$effect(() => {
		const promise = getPromise();
		let cancelled = false;
		value = null;
		failed = false;
		promise.then(
			(next) => {
				if (!cancelled) value = next;
			},
			() => {
				if (!cancelled) failed = true;
			}
		);
		return () => {
			cancelled = true;
		};
	});

	return {
		get value() {
			return value;
		},
		get failed() {
			return failed;
		}
	};
}
