<script lang="ts">
	import { page } from '$app/state';
	import * as Card from '$lib/components/ui/card';
	import * as api from '$lib/cryptotrackerApi';
	import { onMount } from 'svelte';
	import LineChart from '../../../components/charts/LineChart.svelte';
	import Button from '$lib/components/ui/button/button.svelte';
	import NavBreadcrumb from '../../../components/navigation/NavBreadcrumb.svelte';

	let assetData: api.AssetData | null = null;
	let selectedCoin: string = '';
	let hidden: boolean = false;

	function StringKeysToDates(arr: string[]) {
		return arr.map((x) =>
			new Date(x).toLocaleDateString('de-CH', { day: '2-digit', month: '2-digit', year: 'numeric' })
		);
	}

	function SetVisibility() {
		api.setVisibilityForSymbol(assetData?.asset.symbol ?? '', !hidden).then(() => {
			hidden = !hidden;
		});
	}

	function setAssetData() {
		if (!assetData?.asset.symbol) return;

		if (!selectedCoin) return;

		api.setAssetForSymbol(assetData.asset.symbol, selectedCoin);
	}

	onMount(async () => {
		let xy = await api.getAsset(page.params.slug);
		assetData = xy.data;

		selectedCoin = assetData.asset.externalId ?? '';
		hidden = assetData.asset.isHidden ?? false;
	});
</script>

{#if assetData}
	<Button on:click={SetVisibility}>Asset {hidden ? 'Anzeigen' : 'Verstecken'}</Button>
	{#if assetData.asset.name}
		<div class="space-y-4">
			<div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
				<Card.Root>
					<Card.Header class="flex flex-row items-center justify-between space-y-0 pb-2">
						<Card.Title class="text-center text-sm font-medium">Name</Card.Title>
					</Card.Header>
					<Card.Content>
						{assetData.asset.name}
					</Card.Content>
				</Card.Root>
				<Card.Root>
					<Card.Header class="flex flex-row items-center justify-between space-y-0 pb-2">
						<Card.Title class="text-center text-sm font-medium">Symbol</Card.Title>
					</Card.Header>
					<Card.Content>
						{assetData.asset.symbol}
					</Card.Content>
				</Card.Root>
				<Card.Root>
					<Card.Header class="flex flex-row items-center justify-between space-y-0 pb-2">
						<Card.Title class="text-center text-sm font-medium">Aktueller Preis</Card.Title>
					</Card.Header>
					<Card.Content>{assetData.price} CHF</Card.Content>
				</Card.Root>
				<Card.Root>
					<Card.Header class="flex flex-row items-center justify-between space-y-0 pb-2">
						<Card.Title class="text-center text-sm font-medium">Logo</Card.Title>
					</Card.Header>
					<Card.Content>
						<img class="h-10" src={assetData.asset?.image} alt={assetData.asset.name} />
					</Card.Content>
				</Card.Root>
			</div>

			<div class="grid gap-4 md:grid-cols-4 lg:grid-cols-8">
				{#await api.getMeasuringsByDay(7, { $symbol: assetData.asset.symbol ?? '' })}
					<p>Loading...</p>
				{:then measurings}
					<Card.Root class="col-span-4">
						<Card.Header>
							<Card.Title>Bestand letzte 7 Tage</Card.Title>
						</Card.Header>
						<Card.Content>
							<LineChart
								fill={true}
								labels={StringKeysToDates(Object.keys(measurings.data))}
								datasets={[
									{
										name: assetData.asset.symbol ?? '',
										data: Object.values(measurings.data).map(
											(x) => x.find((y) => y.assetId === assetData?.asset.symbol)?.assetAmount ?? 0
										)
									}
								]}
							/>
						</Card.Content>
					</Card.Root>
					<Card.Root class="col-span-4">
						<Card.Header>
							<Card.Title>Wert Bestand letzte 7 Tage</Card.Title>
						</Card.Header>
						<Card.Content>
							<LineChart
								fill={true}
								labels={StringKeysToDates(Object.keys(measurings.data))}
								datasets={[
									{
										name: 'CHF',
										data: Object.values(measurings.data).map(
											(x) => x.find((y) => y.assetId === assetData?.asset.symbol)?.fiatValue ?? 0
										)
									}
								]}
							/>
						</Card.Content>
					</Card.Root>
				{:catch error}
					<p>{error.message}</p>
				{/await}
			</div>
		</div>
	{:else if assetData?.asset.symbol}
		{#await api.findCoinsBySymbol(assetData.asset.symbol) then coins}
			<Card.Root class="col-span-4">
				<Card.Header>
					<Card.Title>Externe ID verknüpfen</Card.Title>
				</Card.Header>
				<Card.Content>
					<select
						class="rounded-lg border-2 border-solid border-gray-200 px-3 py-2 pe-9 text-sm focus:border-blue-500 focus:ring-blue-500 disabled:pointer-events-none disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-400 dark:placeholder-neutral-500 dark:focus:ring-neutral-600"
						bind:value={selectedCoin}
					>
						{#each coins.data as coin}
							<option value={coin.id}>{coin.name}</option>
						{/each}
					</select>
					<Button on:click={setAssetData}>Speichern</Button>
				</Card.Content>
			</Card.Root>
		{/await}
	{/if}
{:else}
	<p>Loading...</p>
{/if}
