<script lang="ts">
	import CheckIcon from '@lucide/svelte/icons/check';
	import ChevronsUpDownIcon from '@lucide/svelte/icons/chevrons-up-down';
	import * as Command from '$lib/components/ui/command';
	import * as Popover from '$lib/components/ui/popover';
	import { Button } from '$lib/components/ui/button';
	import { cn } from '$lib/utils';

	type Item = { value: string; label: string };

	let {
		items = [],
		value = $bindable(''),
		placeholder = 'Select…',
		searchPlaceholder = 'Search…',
		id,
		disabled = false
	}: {
		items?: Item[];
		value?: string;
		placeholder?: string;
		searchPlaceholder?: string;
		id?: string;
		disabled?: boolean;
	} = $props();

	let open = $state(false);
	let search = $state('');

	// The full list can be thousands of entries (CoinGecko) — filter manually
	// and cap the rendered results instead of mounting every item.
	const MAX_RESULTS = 100;
	const filtered = $derived.by(() => {
		const q = search.trim().toLowerCase();
		const matches = q
			? items.filter((i) => i.label.toLowerCase().includes(q) || i.value.toLowerCase().includes(q))
			: items;
		return matches.slice(0, MAX_RESULTS);
	});
	const selectedLabel = $derived(items.find((i) => i.value === value)?.label);
</script>

<Popover.Root bind:open>
	<Popover.Trigger {id} {disabled}>
		{#snippet child({ props })}
			<Button
				{...props}
				variant="outline"
				role="combobox"
				aria-expanded={open}
				class="w-full justify-between font-normal"
			>
				<span class={cn('truncate', !selectedLabel && 'text-muted-foreground')}>
					{selectedLabel ?? placeholder}
				</span>
				<ChevronsUpDownIcon class="size-4 shrink-0 opacity-50" />
			</Button>
		{/snippet}
	</Popover.Trigger>
	<Popover.Content class="w-(--bits-floating-anchor-width) p-0" align="start">
		<Command.Root shouldFilter={false}>
			<Command.Input placeholder={searchPlaceholder} bind:value={search} />
			<Command.List>
				<Command.Empty>No results.</Command.Empty>
				{#each filtered as item (item.value)}
					<Command.Item
						value={item.value}
						onSelect={() => {
							value = item.value;
							open = false;
						}}
					>
						<CheckIcon class={cn('size-4', value !== item.value && 'text-transparent')} />
						{item.label}
					</Command.Item>
				{/each}
				{#if filtered.length === MAX_RESULTS}
					<div class="text-muted-foreground px-2 py-1.5 text-center text-xs">
						Showing first {MAX_RESULTS} — keep typing to narrow down
					</div>
				{/if}
			</Command.List>
		</Command.Root>
	</Popover.Content>
</Popover.Root>
