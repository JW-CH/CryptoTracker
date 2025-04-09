<script lang="ts">
	import { onMount } from 'svelte';
	import * as api from '$lib/cryptotrackerApi';
	import { page } from '$app/state';
	import { Skeleton } from '$lib/components/ui/skeleton';
	import * as Card from '$lib/components/ui/card';

	let data: api.MessungDto[] | null;
	let date: Date = new Date();

	onMount(async () => {
		const dateString = page.url.searchParams.get('date');

		if (dateString) {
			date = new Date(dateString);
		}

		let req = await api.getMeasuringsByDate({ date: date.toISOString() });
		data = req.data;
	});
</script>

{#if !data}
	<div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
		{#each { length: 20 } as _}
			<Card.Root class="flex h-full flex-col">
				<Card.Content class="grow p-3">
					<div class="flex flex-row items-center justify-between">
						<div class="text-sm font-medium"><Skeleton class="h-4 w-[50px] bg-gray-200" /></div>
						<div class="text-sm font-medium"><Skeleton class="h-4 w-[80px] bg-gray-200" /></div>
					</div>
					<div class="text-xs text-gray-500">
						<Skeleton class="h-4 w-[100px] bg-gray-200" />
					</div>
				</Card.Content>
			</Card.Root>
		{/each}
	</div>
{:else if !data.length}
	Keine Daten vorhanden
{:else}
	<div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
		{#each data
			.filter((x) => !x.asset.isHidden)
			.sort((a, b) => b.totalValue - a.totalValue) as measuring}
			<a href={`/assets/${measuring.asset.symbol}`}>
				<Card.Root class="flex h-full flex-col">
					<Card.Content class="grow p-3">
						<div class="flex flex-row items-center justify-between">
							<div class="text-sm font-medium">{measuring.asset.symbol}</div>
							<div class="text-sm font-medium">{measuring.totalValue.toFixed(2)} CHF</div>
						</div>
						<div class="text-xs text-gray-500">
							{measuring.totalAmount.toFixed(8)}
							{measuring.asset.symbol}
						</div>
					</Card.Content>
				</Card.Root>
			</a>
		{/each}
	</div>
{/if}
