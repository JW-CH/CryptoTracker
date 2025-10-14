<script lang="ts">
	import * as Card from '$lib/components/ui/card';

	interface Props {
		title?: string;
		className?: string;
		selectedRange?: number;
	}

	let { title = 'Card Title', className = '', selectedRange = $bindable(7) }: Props = $props();

	const ranges = [7, 14, 30, 90];
</script>

<Card.Root class={className}>
	<Card.Header class="flex items-center justify-between">
		<Card.Title>{title} (letzte {selectedRange} Tage)</Card.Title>
		<div class="flex gap-2">
			{#each ranges as range}
				<button
					onclick={() => (selectedRange = range)}
					class="rounded-md px-2 py-1 text-sm transition
						{selectedRange === range ? 'bg-gray-300' : 'bg-gray-100 hover:bg-gray-200'}"
				>
					{range}
				</button>
			{/each}
		</div>
	</Card.Header>
	<Card.Content>
		<slot />
	</Card.Content>
</Card.Root>
