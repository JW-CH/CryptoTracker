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
		lastRange = range;
		assetData = await LoadAssetData();

		if (!assetData?.asset?.symbol) {
			console.error('Asset or symbol is missing');
			return;
		}

		assetInitialized = true;

		await LoadMessungen(range, assetData.asset.symbol);
	});
</script>

<div class="space-y-6">
	<!-- Header -->
	<div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
		<div class="flex items-center gap-4">
			{#if assetInitialized && assetData?.asset?.image}
				<img
					class="size-12 rounded-full object-contain"
					src={assetData.asset.image}
					alt={assetData?.asset.name}
				/>
			{:else if assetInitialized}
				<div
					class="flex size-12 items-center justify-center rounded-full bg-muted text-lg font-bold text-muted-foreground"
				>
					{(assetData?.asset.symbol ?? '?').slice(0, 2).toUpperCase()}
				</div>
			{:else}
				<Skeleton class="size-12 rounded-full bg-muted" />
			{/if}
			<div>
				{#if assetInitialized}
					<div class="flex items-center gap-3">
						<h1 class="text-2xl font-bold tracking-tight">
							{assetData?.asset.name ?? assetData?.asset.symbol}
						</h1>
						<span
							class="rounded-full bg-primary/10 px-3 py-1 text-sm font-semibold text-primary"
						>
							{assetData?.price} CHF
						</span>
					</div>
					{#if assetData?.asset.name}
						<p class="text-sm text-muted-foreground">{assetData?.asset.symbol}</p>
					{/if}
				{:else}
					<Skeleton class="mb-1 h-7 w-40 bg-muted" />
					<Skeleton class="h-4 w-20 bg-muted" />
				{/if}
			</div>
		</div>
		<div class="flex gap-2">
			<Button variant="outline" size="sm" onclick={SetVisibility}>
				{hidden ? 'Anzeigen' : 'Verstecken'}
			</Button>
			<Button variant="outline" size="sm" onclick={ResetAsset}>Reset</Button>
			<Button variant="destructive" size="sm" onclick={DeleteAsset}>Löschen</Button>
		</div>
	</div>

	{#if assetInitialized && assetData?.asset.symbol && !assetData?.asset.name}
		<!-- Unlinked asset: show linking UI -->
		{#if !selectedCoin}
			<Card.Root>
				<Card.Header>
					<Card.Title>Asset-Typ</Card.Title>
				</Card.Header>
				<Card.Content class="flex items-center gap-3">
					<select
						class="rounded-lg border border-input bg-background px-3 py-2 pe-9 text-sm focus:border-ring focus:ring-1 focus:ring-ring"
						bind:value={selectedAssetType}
					>
						<option value="Fiat">Fiat</option>
						<option value="Crypto">Crypto</option>
						<option value="Stock">Stock</option>
						<option value="ETF">ETF</option>
					</select>
					<Button size="sm" onclick={setSelectedAssetType}>Setzen</Button>
				</Card.Content>
			</Card.Root>
		{/if}
		<Card.Root>
			<Card.Header>
				<Card.Title>Externe ID verknüpfen</Card.Title>
			</Card.Header>
			<Card.Content class="flex items-center gap-3">
				<select
					class="rounded-lg border border-input bg-background px-3 py-2 pe-9 text-sm focus:border-ring focus:ring-1 focus:ring-ring"
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
				<Button size="sm" onclick={setAssetData}>Speichern</Button>
			</Card.Content>
		</Card.Root>
	{:else}
		<!-- Linked asset: show data -->
		<div class="space-y-6">
			<!-- Charts -->
			<div class="grid gap-4 md:grid-cols-2">
				{#key [dailyMeasurings, measuringsInitialized]}
					<CardWithDays title="Bestand" bind:selectedRange={range}>
						<LineChart
							skeleton={!measuringsInitialized}
							fill={true}
							labels={dailyMeasurings.map((x) => x.date)}
							datasets={[
								{
									name: assetData?.asset.symbol ?? '',
									data: dailyMeasurings.map((x) => x.measurings.at(0)?.totalAmount ?? 0)
								}
							]}
						/>
					</CardWithDays>
					<CardWithDays title="Wert Bestand" bind:selectedRange={range}>
						<LineChart
							skeleton={!measuringsInitialized}
							fill={true}
							labels={dailyMeasurings.map((x) => x.date)}
							datasets={[
								{
									name: 'CHF',
									data: dailyMeasurings.map((x) => x.measurings.at(0)?.totalValue ?? 0)
								}
							]}
						/>
					</CardWithDays>
				{/key}
			</div>

			<!-- Integrations -->
			{#if measuringsInitialized && dailyMeasurings.length > 0}
				<div class="space-y-3">
					<h2 class="text-lg font-semibold">Integrationen</h2>
					<div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
						{#each dailyMeasurings.at(-1)?.measurings.at(0)?.integrationValues! as integrationItem}
							<a href="/integrations/{integrationItem.integration.id}" class="group">
								<Card.Root
									class="transition-all duration-200 hover:shadow-md hover:border-primary/20 group-hover:-translate-y-0.5"
								>
									<Card.Content class="pt-6">
										<p class="text-sm font-medium text-muted-foreground">
											{integrationItem.integration.name}
										</p>
										<p class="mt-1 text-xl font-semibold">
											{integrationItem.amount?.toFixed(2)}
										</p>
									</Card.Content>
								</Card.Root>
							</a>
						{/each}
					</div>
				</div>
			{/if}
		</div>
	{/if}
</div>
