<script lang="ts">
	import { page } from '$app/state';
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import * as api from '$lib/cryptotrackerApi';
	import { onMount } from 'svelte';

	let assets: api.Asset[];
	let selectedAsset: string;
	let amount: number = 0;
	let date: string = new Date().toISOString().split('T')[0];

	onMount(async () => {
		var request = await api.getAssets();

		assets = request.data;
	});

	async function SaveShit() {
		if (!date || !selectedAsset || !amount) return;
		console.log('datum: ' + date);
		console.log('selektiertes asset: ' + selectedAsset);
		console.log('wert: ' + amount);

		let res = await api.addIntegrationMeasurement(page.params.slug, {
			symbol: selectedAsset,
			date: date,
			amount: amount
		});

		console.log(res.data ? 'Erfolg' : 'Nicht hinzugefügt');
	}
</script>

<Card.Root class="col-span-4">
	<Card.Header>
		<Card.Title>Messung hinzufügen</Card.Title>
	</Card.Header>
	<Card.Content>
		<input
			bind:value={date}
			type="date"
			class="rounded-lg border-2 border-solid border-gray-200 px-3 py-2 text-sm focus:border-blue-500 focus:ring-blue-500 disabled:pointer-events-none disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-400 dark:placeholder-neutral-500 dark:focus:ring-neutral-600"
		/>
		<select
			class="rounded-lg border-2 border-solid border-gray-200 px-3 py-2 pe-9 text-sm focus:border-blue-500 focus:ring-blue-500 disabled:pointer-events-none disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-400 dark:placeholder-neutral-500 dark:focus:ring-neutral-600"
			bind:value={selectedAsset}
		>
			{#each assets as asset}
				<option value={asset.symbol}>{asset.name ? asset.name : asset.symbol}</option>
			{/each}
		</select>
		<input
			bind:value={amount}
			type="number"
			class="rounded-lg border-2 border-solid border-gray-200 px-3 py-2 text-sm focus:border-blue-500 focus:ring-blue-500 disabled:pointer-events-none disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-400 dark:placeholder-neutral-500 dark:focus:ring-neutral-600"
		/>
		<Button on:click={SaveShit}>Speichern</Button>
	</Card.Content>
</Card.Root>
