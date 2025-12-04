<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import type { HTMLAttributes } from 'svelte/elements';
	import { Button } from '$lib/components/ui/button';
	import * as ButtonGroup from '$lib/components/ui/button-group/index.js';

	interface Props {
		title?: string;
		class: $$Props['class'];
		children: $$Props['children'];
		selectedRange?: number;
	}

	type $$Props = HTMLAttributes<HTMLDivElement>;

	let {
		title = 'Card Title',
		class: className,
		children,
		selectedRange = $bindable(7)
	}: Props = $props();

	const ranges = [7, 14, 30, 90];
</script>

<Card.Root class={className}>
	<Card.Header class="flex items-center justify-between">
		<Card.Title>{title} (letzte {selectedRange} Tage)</Card.Title>
		<div class="flex gap-2">
			<ButtonGroup.Root>
				{#each ranges as range}
					<Button
						size="sm"
						variant="outline"
						class="text-sm transition hover:bg-gray-200
						{selectedRange === range ? 'bg-gray-200' : ''}"
						onclick={() => (selectedRange = range)}
					>
						{range}</Button
					>
				{/each}
			</ButtonGroup.Root>
		</div>
	</Card.Header>
	<Card.Content>
		{@render children?.()}
	</Card.Content>
</Card.Root>
