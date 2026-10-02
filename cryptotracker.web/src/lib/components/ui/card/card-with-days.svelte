<script lang="ts">
	import * as Card from "#lib/components/ui/card/index.js";
	import type { Snippet } from "svelte";

	let {
		title = "Card Title",
		class: className = undefined,
		selectedRange = $bindable(7),
		children
	}: {
		title?: string;
		class?: string;
		selectedRange?: number;
		children?: Snippet;
	} = $props();

	const ranges = [7, 14, 30, 90];
</script>

<Card.Root class={className}>
	<Card.Header class="flex items-center justify-between">
		<Card.Title>{title} (last {selectedRange} days)</Card.Title>
		<div class="flex gap-1">
			{#each ranges as range (range)}
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
		{@render children?.()}
	</Card.Content>
</Card.Root>
