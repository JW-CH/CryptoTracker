<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import type { HTMLAttributes } from 'svelte/elements';

	interface Props {
		title?: string;
		class?: $$Props['class'];
		selectedRange?: number;
	}

	type $$Props = HTMLAttributes<HTMLDivElement>;

	let {
		title = 'Card Title',
		class: className = undefined,
		selectedRange = $bindable(7)
	}: Props = $props();

	const ranges = [7, 14, 30, 90];
</script>

<Card.Root class={className}>
	<Card.Header class="flex items-center justify-between">
		<Card.Title>{title} (letzte {selectedRange} Tage)</Card.Title>
		<div class="flex gap-1">
			{#each ranges as range}
				<button
					onclick={() => (selectedRange = range)}
					class="rounded-md px-2.5 py-1 text-xs font-medium transition-colors
						{selectedRange === range
						? 'bg-primary text-primary-foreground'
						: 'bg-secondary text-secondary-foreground hover:bg-secondary/80'}"
				>
					{range}d
				</button>
			{/each}
		</div>
	</Card.Header>
	<Card.Content>
		<slot />
	</Card.Content>
</Card.Root>
