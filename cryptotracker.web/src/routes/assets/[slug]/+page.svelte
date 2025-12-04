<script lang="ts">
	import { page } from '$app/state';
	import * as Card from '$lib/components/ui/card';
	import * as api from '$lib/cryptotrackerApi';
	import { onMount } from 'svelte';
	import { Button } from '$lib/components/ui/button';
	import { Skeleton } from '$lib/components/ui/skeleton';
	import * as ButtonGroup from '$lib/components/ui/button-group/index.js';
	import LineChart from '$lib/components/charts/LineChart.svelte';
	import CardWithDays from '$lib/components/ui/card/card-with-days.svelte';
	import Badge from '$lib/components/ui/badge/badge.svelte';

	interface DailyMeasurings {
		date: string;
		measurings: api.MessungDto[];
	}

	const selectClasses =
		'flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50';

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

{#if assetInitialized && assetData?.asset.symbol && !assetData?.asset.name}
	<section class="mx-auto max-w-4xl space-y-6">
		<Card.Root>
			<Card.Header>
				<Card.Title>Asset konfigurieren</Card.Title>
				<Card.Description>
					Ordne dem Asset eine Quelle zu, damit wir Preis- und Bestandsdaten automatisiert beziehen
					können.
				</Card.Description>
			</Card.Header>
			<Card.Content class="space-y-6">
				{#if !selectedCoin}
					<div class="space-y-2">
						<label class="text-muted-foreground text-sm font-medium" for="asset-type"
							>Asset-Typ</label
						>
						<select id="asset-type" class={selectClasses} bind:value={selectedAssetType}>
							<option value="Fiat">Fiat</option>
							<option value="Crypto">Crypto</option>
							<option value="Stock">Stock</option>
							<option value="ETF">ETF</option>
						</select>
						<div class="flex justify-end">
							<Button size="sm" variant="outline" onclick={setSelectedAssetType}>Übernehmen</Button>
						</div>
					</div>
				{/if}
				<div class="space-y-2">
					<label class="text-muted-foreground text-sm font-medium" for="external-id"
						>Externe ID</label
					>
					<select id="external-id" class={selectClasses} bind:value={selectedCoin}>
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
				</div>
			</Card.Content>
			<Card.Footer class="border-border bg-muted/40 flex items-center justify-end gap-2 border-t">
				<Button variant="outline" size="sm" onclick={ResetAsset}>Zurücksetzen</Button>
				<Button variant="destructive" size="sm" onclick={DeleteAsset}>Löschen</Button>
				<Button size="sm" onclick={setAssetData}>Verknüpfen</Button>
			</Card.Footer>
		</Card.Root>
	</section>
{:else}
	<section class="space-y-8">
		<Card.Root>
			<Card.Content class="space-y-6">
				<div class="flex flex-col gap-6 md:flex-row md:items-center md:justify-between">
					<div class="flex items-center gap-4">
						{#if assetInitialized}
							{#if assetData?.asset.image}
								<div
									class="border-border bg-background flex h-16 w-16 items-center justify-center overflow-hidden rounded-full border p-2"
								>
									<img
										class="h-full w-full object-contain"
										src={assetData.asset.image}
										alt={assetData.asset.name}
									/>
								</div>
							{:else}
								<div class="border-border h-16 w-16 rounded-full border border-dashed"></div>
							{/if}
						{:else}
							<Skeleton class="h-16 w-16 rounded-full" />
						{/if}
						<div class="space-y-1">
							{#if assetInitialized}
								<h1 class="text-2xl font-semibold tracking-tight">
									{assetData?.asset.name ?? assetData?.asset.symbol}
								</h1>
								<p class="text-muted-foreground text-sm uppercase">
									{assetData?.asset.symbol}
									<Badge class="ml-2 uppercase">{assetData?.asset.assetType}</Badge>
								</p>
							{:else}
								<div class="space-y-2">
									<Skeleton class="h-6 w-48" />
									<Skeleton class="h-4 w-24" />
								</div>
							{/if}
						</div>
					</div>
					<div class="flex flex-wrap items-center gap-2">
						<div>
							<p class="text-muted-foreground text-xs font-medium">Aktueller Preis</p>
							{#if assetInitialized}
								<p class="mt-2 text-lg font-semibold">{assetData?.price} CHF</p>
							{:else}
								<Skeleton class="mt-2 h-6 w-1/2" />
							{/if}
						</div>
					</div>
					<div class="flex flex-wrap items-center gap-2">
						<ButtonGroup.Root>
							<Button size="sm" variant="outline" onclick={SetVisibility}>
								Asset {hidden ? 'anzeigen' : 'ausblenden'}
							</Button>
							<Button size="sm" variant="outline" onclick={ResetAsset}>Zurücksetzen</Button>
							<Button size="sm" variant="destructive" onclick={DeleteAsset}>Löschen</Button>
						</ButtonGroup.Root>
					</div>
				</div>
			</Card.Content>
		</Card.Root>
		{#key [dailyMeasurings, measuringsInitialized]}
			<div class="grid gap-4 lg:grid-cols-2">
				<CardWithDays class="h-full" title="Bestand" bind:selectedRange={range}>
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
				<CardWithDays class="h-full" title="Wert Bestand" bind:selectedRange={range}>
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
			</div>
		{/key}
		<Card.Root>
			<Card.Header>
				<Card.Title>Integrationen</Card.Title>
			</Card.Header>
			<Card.Content>
				{#if measuringsInitialized && dailyMeasurings.length > 0}
					<div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
						{#each dailyMeasurings.at(-1)?.measurings.at(0)?.integrationValues! as integrationItem}
							<a
								class="border-border bg-background hover:border-primary group block rounded-xl border p-4 transition hover:-translate-y-0.5 hover:shadow-lg"
								href="/integrations/{integrationItem.integration.id}"
							>
								<p class="text-muted-foreground text-sm font-medium">
									{integrationItem.integration.name}
								</p>
								<p class="mt-3 text-xl font-semibold">
									{integrationItem.amount?.toFixed(2)}
								</p>
							</a>
						{/each}
					</div>
				{:else}
					<div
						class="border-border bg-muted/20 text-muted-foreground rounded-lg border border-dashed p-6 text-center text-sm"
					>
						Keine aktiven Integrationen gefunden.
					</div>
				{/if}
			</Card.Content>
		</Card.Root>
	</section>
{/if}
