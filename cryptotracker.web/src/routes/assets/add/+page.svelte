<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import * as api from '$lib/cryptotrackerApi';

	let symbol: string = $state('');
	let externalId: string = $state('');
	let assetType: api.AssetType = $state('Fiat');

	let values: api.Coin[] | api.Fiat[] | null = $state(null);

	import { onMount } from 'svelte';

	onMount(async () => {
		values = await GetStuff(assetType);
	});

	$effect(() => {
		GetStuff(assetType).then((value) => (values = value));
	});

	async function GetStuff(assettype: api.AssetType) {
		let request;
		switch (assettype) {
			case 'Fiat':
				request = await api.getFiats();

				if (request.status != 200) {
					return [];
				}

				return request.data;
			case 'Crypto':
				request = await api.getCoins();

				if (request.status != 200) {
					return [];
				}

				return request.data;
			default:
				return [];
		}
	}

	async function AddIntegration() {
		if (!symbol) return;
		if (assetType) {
			externalId = (values as api.Fiat[]).find((x) => x.symbol === symbol)?.symbol || '';
		} else {
			externalId = (values as api.Coin[]).find((x) => x.symbol === symbol)?.id ?? '';
		}

		if (!externalId) return;

		let request = await api.addAsset({
			symbol: symbol,
			externalId: externalId,
			assetType: assetType
		});

		if (request.data) {
			window.location.href = '/assets/' + symbol;
		}
	}
</script>

<Card.Root class="col-span-4">
	<Card.Header>
		<Card.Title>Vermögenswert hinzufügen</Card.Title>
	</Card.Header>
	<Card.Content class="flex items-center">
		<div>
			AssetType:
			<select
				class="rounded-lg border-2 border-solid border-gray-200 px-3 py-2 pe-9 text-sm focus:border-blue-500 focus:ring-blue-500 disabled:pointer-events-none disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-400 dark:placeholder-neutral-500 dark:focus:ring-neutral-600"
				bind:value={assetType}
			>
				<option value="Fiat">Fiat</option>
				<option value="Crypto">Crypto</option>
				<option value="Stock">Stock</option>
				<option value="ETF">ETF</option>
				<!-- <option value="Commodity">Commodity</option>
				<option value="RealEstate">RealEstate</option> -->
			</select>
		</div>
		<div>
			asset:
			<select
				class="min-w-48 rounded-lg border-2 border-solid border-gray-200 px-3 py-2 pe-9 text-sm focus:border-blue-500 focus:ring-blue-500 disabled:pointer-events-none disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-400 dark:placeholder-neutral-500 dark:focus:ring-neutral-600"
				bind:value={symbol}
			>
				{#key values}
					{#each values ?? [] as item}
						<option value={item.symbol}>{item.name}</option>
					{/each}
				{/key}
			</select>
		</div>
		<Button on:click={AddIntegration}>Speichern</Button>
	</Card.Content>
</Card.Root>
