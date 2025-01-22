<script lang="ts">
	import { page } from '$app/state';
	import * as Card from '$lib/components/ui/card';
	import * as api from '$lib/cryptotrackerApi';
	import { onMount } from 'svelte';
	import LineChart from '../../../components/charts/LineChart.svelte';
	import Button from '$lib/components/ui/button/button.svelte';

	let assetData: api.AssetData | null = null;
	let selectedCoin: string = '';
	let isFiat: boolean = false;
	let hidden: boolean = false;

	function StringKeysToDates(arr: string[]) {
		return arr.map((x) =>
			new Date(x).toLocaleDateString('de-CH', { day: '2-digit', month: '2-digit', year: 'numeric' })
		);
	}

	async function SetVisibility() {
		let request = await api.setVisibilityForSymbol(assetData?.asset.symbol ?? '', !hidden);

		if (request.data) {
			hidden = !hidden;
		}
	}

	async function SetIsFiat() {
		if (!assetData?.asset.symbol) return;

		let request = await api.setFiatForSymbol(assetData.asset.symbol, !isFiat);

		if (request.data) {
			isFiat = !isFiat;
		}
	}

	async function ResetAsset() {
		if (!assetData?.asset.symbol) return;

		let request = await api.resetAsset(assetData.asset.symbol);

		if (request.data) {
			assetData = null;
			assetData = await LoadAssetData();
		}
	}

	async function setAssetData() {
		if (!assetData?.asset.symbol) return;

		if (!selectedCoin) return;

		let request = await api.setExternalIdForSymbol(assetData.asset.symbol, selectedCoin);

		if (request.data) {
			assetData = request.data;
		}
	}

	async function LoadAssetData() {
		let request = await api.getAsset(page.params.slug);
		let data = request.data;
		selectedCoin = data.asset.externalId ?? '';
		hidden = data.asset.isHidden ?? false;
		isFiat = data.asset.isFiat ?? false;
		return data;
	}

	onMount(async () => {
		assetData = await LoadAssetData();
	});
</script>

{#if assetData}
	<Button on:click={SetVisibility}>Asset {hidden ? 'Anzeigen' : 'Verstecken'}</Button>
	<Button on:click={ResetAsset}>Reset</Button>
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
					<Card.Root class="col-span-4">
						<Card.Header>
							<Card.Title>Bestand letzte 7 Tage</Card.Title>
						</Card.Header>
						<Card.Content>
							<LineChart skeleton={true} />
						</Card.Content>
					</Card.Root>
					<Card.Root class="col-span-4">
						<Card.Header>
							<Card.Title>Wert Bestand letzte 7 Tage</Card.Title>
						</Card.Header>
						<Card.Content>
							<LineChart skeleton={true} />
						</Card.Content>
					</Card.Root>
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
											(x) => x.find((y) => y.asset.id === assetData?.asset.symbol)?.amount ?? 0
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
											(x) => x.find((y) => y.asset.id === assetData?.asset.symbol)?.totalValue ?? 0
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
		{#if !selectedCoin}
			<Button on:click={SetIsFiat}>{isFiat ? 'Kein Fiat' : 'Fiat'}</Button>
		{/if}
		<Card.Root class="col-span-4">
			<Card.Header>
				<Card.Title>Externe ID verknüpfen</Card.Title>
			</Card.Header>
			<Card.Content>
				<select
					class="rounded-lg border-2 border-solid border-gray-200 px-3 py-2 pe-9 text-sm focus:border-blue-500 focus:ring-blue-500 disabled:pointer-events-none disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-400 dark:placeholder-neutral-500 dark:focus:ring-neutral-600"
					bind:value={selectedCoin}
				>
					{#key isFiat}
						{#if isFiat}
							{#await api.findFiatBySymbol(assetData.asset.symbol) then coins}
								{#each coins.data as coin}
									<option value={coin.symbol}>{coin.name}</option>
								{/each}
							{/await}
						{:else}
							{#await api.findCoinsBySymbol(assetData.asset.symbol) then coins}
								{#each coins.data as coin}
									<option value={coin.id}>{coin.name}</option>
								{/each}
							{/await}
						{/if}
					{/key}
				</select>
				<Button on:click={setAssetData}>Speichern</Button>
			</Card.Content>
		</Card.Root>
	{/if}
{:else}
	<p>Loading...</p>
{/if}
