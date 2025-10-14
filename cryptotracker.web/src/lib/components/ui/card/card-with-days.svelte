<script lang="ts">
	import * as Card from '$lib/components/ui/card';

	export let title: string = 'Card Title';
	export let className: string = 'Card Title';
	let selectedRange = 14;
	const ranges = [7, 14, 30, 90];
</script>

<Card.Root class={className}>
	<Card.Header class="flex items-center justify-between">
		<Card.Title>{title} (letzte {selectedRange} Tage)</Card.Title>
		<div class="flex gap-2">
			{#each ranges as range}
				<button
					on:click={() => (selectedRange = range)}
					class="rounded-md px-2 py-1 text-sm transition
						{selectedRange === range ? 'bg-gray-300' : 'bg-gray-100 hover:bg-gray-200'}"
				>
					{range}
				</button>
			{/each}
		</div>
	</Card.Header>
	<Card.Content>
		<slot range={selectedRange} />
	</Card.Content>
</Card.Root>
