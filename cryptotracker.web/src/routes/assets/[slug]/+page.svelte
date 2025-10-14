<script lang="ts">
	import { page } from '$app/state';
	import * as Card from '$lib/components/ui/card';
	import * as api from '$lib/cryptotrackerApi';
	import { onMount } from 'svelte';
	import Button from '$lib/components/ui/button/button.svelte';
	import { Skeleton } from '$lib/components/ui/skeleton';
	import LineChart from '$lib/components/charts/LineChart.svelte';
	import CardWithDays from '$lib/components/ui/card/card-with-days.svelte';

	interface DailyMeasurings {
		date: string;
		measurings: api.MessungDto[];
	}

	let assetInitialized = $state<boolean>(false);
	let assetData = $state<api.AssetData>();

	let range = $state<number>(7);

	let measuringsInitialized = $state<boolean>(false);
	let dailyMeasurings = $state<DailyMeasurings[]>([]);

	let selectedCoin = $state<string>('');
	let selectedAssetType = $state<api.AssetType>('Fiat');
	let assetType = $state<api.AssetType>('Fiat');
	let hidden = $state<boolean>(false);

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

	async function setSelectedAssetType() {
		if (!assetData?.asset.symbol) return;

		let request = await api.setAssetTypeForSymbol(assetData.asset.symbol, selectedAssetType);

		if (request.data) {
			assetType = selectedAssetType;
		}
	}

	async function ResetAsset() {
		if (!assetData?.asset.symbol) return;

		let request = await api.resetAsset(assetData.asset.symbol);

		if (request.data) {
			assetData = await LoadAssetData();
		}
	}

	async function DeleteAsset() {
		if (!assetData?.asset.symbol) return;

		let request = await api.deleteAsset(assetData.asset.symbol);

		if (request.status != 200) {
			return;
		}

		if (request.data) {
			window.location.href = '/assets';
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
		let request = await api.getAsset(page.params.slug ?? '');

		if (request.status != 200) {
			console.error('Error loading asset data');
			return;
		}

		let data = request.data;
		selectedCoin = data.asset.externalId ?? '';
		hidden = data.asset.isHidden ?? false;
		selectedAssetType = data.asset.assetType ?? 'Fiat';
		assetType = data.asset.assetType ?? 'Fiat';
		return data;
	}

	async function LoadMessungen(days: number, symbol: string) {
		measuringsInitialized = false;
		let request = await api.getMeasuringsByDays(days, { $symbol: symbol });

		var dates = StringKeysToDates(Object.keys(request.data));
		var values = Object.values(request.data);

		dailyMeasurings = [];

		for (let key in Object.keys(request.data)) {
			let date = dates[key];
			let val = values[key];
			dailyMeasurings.push({ date: date, measurings: val });
		}
		measuringsInitialized = true;
	}

	let lastRange: number | undefined;
	$effect(() => {
		if (!assetData?.asset.symbol) return;

		if (range !== lastRange) {
			LoadMessungen(range, assetData.asset.symbol);
			lastRange = range;
		}
	});

	onMount(async () => {
		assetData = await LoadAssetData();

		if (!assetData) {
			console.error('No asset data found');
			return;
		}

		assetInitialized = true;

		await LoadMessungen(range, assetData.asset.symbol!);
		lastRange = range;
	});
</script>

<Button onclick={SetVisibility}>Asset {hidden ? 'Anzeigen' : 'Verstecken'}</Button>
<Button onclick={ResetAsset}>Reset</Button>
<Button onclick={DeleteAsset} class="btn bg-destructive">Löschen</Button>
{#if assetInitialized && assetData?.asset.symbol && !assetData?.asset.name}
	{#if !selectedCoin}
		AssetType:
		<select
			class="rounded-lg border-2 border-solid border-gray-200 px-3 py-2 pe-9 text-sm focus:border-blue-500 focus:ring-blue-500 disabled:pointer-events-none disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-400 dark:placeholder-neutral-500 dark:focus:ring-neutral-600"
			bind:value={selectedAssetType}
		>
			<option value="Fiat">Fiat</option>
			<option value="Crypto">Crypto</option>
			<option value="Stock">Stock</option>
			<option value="ETF">ETF</option>
			<!-- <option value="Commodity">Commodity</option>
				<option value="RealEstate">RealEstate</option> -->
		</select>
		<Button onclick={setSelectedAssetType}>Setzen</Button>
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
				{#key assetType}
					{#if assetType === 'Fiat'}
						{#await api.findFiatBySymbol(assetData.asset.symbol) then coins}
							{#each coins.data as coin}
								<option value={coin.symbol}>{coin.name}</option>
							{/each}
						{/await}
					{:else if assetType === 'Crypto'}
						{#await api.findCoinsBySymbol(assetData.asset.symbol) then coins}
							{#each coins.data as coin}
								<option value={coin.id}>{coin.name}</option>
							{/each}
						{/await}
					{/if}
				{/key}
			</select>
			<Button onclick={setAssetData}>Speichern</Button>
		</Card.Content>
	</Card.Root>
{:else}
	<div class="space-y-4">
		<div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
			<Card.Root>
				<Card.Header class="flex flex-row items-center justify-between space-y-0 pb-2">
					<Card.Title class="text-center text-sm font-medium">Name</Card.Title>
				</Card.Header>
				<Card.Content>
					{#if assetInitialized}
						{assetData?.asset.name}
					{:else}
						<Skeleton class="h-6 w-1/2 bg-gray-200" />
					{/if}
				</Card.Content>
			</Card.Root>
			<Card.Root>
				<Card.Header class="flex flex-row items-center justify-between space-y-0 pb-2">
					<Card.Title class="text-center text-sm font-medium">Symbol</Card.Title>
				</Card.Header>
				<Card.Content>
					{#if assetInitialized}
						{assetData?.asset.symbol}
					{:else}
						<Skeleton class="h-6 w-1/2 bg-gray-200" />
					{/if}
				</Card.Content>
			</Card.Root>
			<Card.Root>
				<Card.Header class="flex flex-row items-center justify-between space-y-0 pb-2">
					<Card.Title class="text-center text-sm font-medium">Aktueller Preis</Card.Title>
				</Card.Header>
				<Card.Content>
					{#if assetInitialized}
						{assetData?.price} CHF
					{:else}
						<Skeleton class="h-6 w-1/2 bg-gray-200" />
					{/if}
				</Card.Content>
			</Card.Root>
			<Card.Root>
				<Card.Header class="flex flex-row items-center justify-between space-y-0 pb-2">
					<Card.Title class="text-center text-sm font-medium">Logo</Card.Title>
				</Card.Header>
				<Card.Content>
					{#if assetInitialized}
						<img class="h-10" src={assetData?.asset?.image} alt={assetData?.asset.name} />
					{:else}
						<Skeleton class="h-10 w-10 bg-gray-200" />
					{/if}
				</Card.Content>
			</Card.Root>
		</div>
		<div class="grid gap-4 md:grid-cols-4 lg:grid-cols-8">
			{#key [dailyMeasurings, measuringsInitialized]}
				<CardWithDays className="col-span-4" title="Bestand" bind:selectedRange={range}>
					<LineChart
						skeleton={!measuringsInitialized}
						fill={true}
						labels={dailyMeasurings.map((x) => x.date)}
						datasets={[
							{
								name: assetData?.asset.symbol ?? '',
								// take the first measuring because it is filtered by the asset symbol
								data: dailyMeasurings.map((x) => x.measurings.at(0)?.totalAmount ?? 0)
							}
						]}
					/>
				</CardWithDays>
				<CardWithDays className="col-span-4" title="Wert Bestand" bind:selectedRange={range}>
					<LineChart
						skeleton={!measuringsInitialized}
						fill={true}
						labels={dailyMeasurings.map((x) => x.date)}
						datasets={[
							{
								name: 'CHF',
								// take the first measuring because it is filtered by the asset symbol
								data: dailyMeasurings.map((x) => x.measurings.at(0)?.totalValue ?? 0)
							}
						]}
					/>
				</CardWithDays>
			{/key}
		</div>
		<p>Integrationen:</p>
		<div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
			{#if measuringsInitialized && dailyMeasurings.length > 0}
				<!-- loop through the last measuring (today) and the first integration (filtered) value -->
				{#each dailyMeasurings.at(-1)?.measurings.at(0)?.integrationValues! as integrationItem}
					<a href="/integrations/{integrationItem.integration.id}">
						<Card.Root>
							<Card.Header class="flex flex-row items-center justify-between space-y-0 pb-2">
								<Card.Title class="text-center text-sm font-medium"
									>{integrationItem.integration.name}</Card.Title
								>
							</Card.Header>
							<Card.Content>
								{integrationItem.integration.name}: {integrationItem.amount?.toFixed(2)}
							</Card.Content>
						</Card.Root>
					</a>
				{/each}
			{/if}
		</div>
	</div>
{/if}
