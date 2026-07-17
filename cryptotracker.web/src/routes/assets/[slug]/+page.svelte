<script lang="ts">
	import { page } from "$app/state";
	import { goto } from "$app/navigation";
	import * as Card from "$lib/components/ui/card";
	import * as api from "$lib/cryptotrackerApi";
	import { baseCurrency } from "$lib/stores/config";
	import { formatAmount } from "$lib/format";
	import { onMount, untrack } from "svelte";
	import Button from "$lib/components/ui/button/button.svelte";
	import LineChart from "$lib/components/charts/LineChart.svelte";
	import PageHeader from "$lib/components/page-header.svelte";
	import CardWithDays from "$lib/components/ui/card/card-with-days.svelte";

	interface DailyMeasurings {
		date: string;
		measurings: api.AssetHoldingDto[];
	}

	let { data } = $props();

	const initial = untrack(() => data.asset);

	let assetData = $state<api.AssetWithPriceDto>(initial);

	let range = $state<number>(7);

	let measuringsInitialized = $state<boolean>(false);
	let dailyMeasurings = $state<DailyMeasurings[]>([]);

	let selectedCoin = $state<string>(initial.asset.externalId ?? "");
	let selectedAssetType = $state<api.AssetType>(initial.asset.assetType ?? "Fiat");
	let assetType = $state<api.AssetType>(initial.asset.assetType ?? "Fiat");
	let hidden = $state<boolean>(initial.asset.isHidden ?? false);

	async function SetVisibility() {
		let request = await api.setVisibilityForSymbol(assetData?.asset.symbol ?? "", !hidden);

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
			const fresh = await LoadAssetData();
			if (fresh) assetData = fresh;
		}
	}

	function EditAsset() {
		goto(`${page.url.pathname}/edit`);
	}

	async function DeleteAsset() {
		if (!assetData?.asset.symbol) return;

		let request = await api.deleteAsset(assetData.asset.symbol);

		if (request.status != 200) {
			return;
		}

		if (request.data) {
			window.location.href = "/assets";
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
		let request = await api.getAsset(page.params.slug ?? "");

		if (request.status != 200) {
			console.error("Error loading asset data");
			return;
		}

		let data = request.data;
		selectedCoin = data.asset.externalId ?? "";
		hidden = data.asset.isHidden ?? false;
		selectedAssetType = data.asset.assetType ?? "Fiat";
		assetType = data.asset.assetType ?? "Fiat";
		return data;
	}

	async function LoadMessungen(days: number, symbol: string) {
		measuringsInitialized = false;
		let request = await api.getMeasuringsByDays(days, { $symbol: symbol });

		// Keep the raw ISO date keys — LineChart parses them into a real time axis
		dailyMeasurings = Object.entries(request.data).map(([date, measurings]) => ({
			date,
			measurings
		}));
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

		if (!assetData?.asset?.symbol) {
			console.error("Asset or symbol is missing");
			return;
		}

		await LoadMessungen(range, assetData.asset.symbol);
	});
</script>

<div class="space-y-6">
	<!-- Header -->
	<PageHeader
		title={assetData?.asset.name ?? assetData?.asset.symbol ?? ""}
		subtitle={assetData?.asset.name ? (assetData?.asset.symbol ?? undefined) : undefined}
	>
		{#snippet media()}
			{#if assetData?.asset?.image}
				<img
					class="size-12 rounded-full object-contain"
					src={assetData.asset.image}
					alt={assetData?.asset.name}
				/>
			{:else}
				<div
					class="bg-muted text-muted-foreground flex size-12 items-center justify-center rounded-full text-lg font-bold"
				>
					{(assetData?.asset.symbol ?? "?").slice(0, 2).toUpperCase()}
				</div>
			{/if}
		{/snippet}
		{#snippet meta()}
			<span class="bg-primary/10 text-primary rounded-full px-3 py-1 text-sm font-semibold">
				{assetData?.price}
				{$baseCurrency}
			</span>
		{/snippet}
		{#snippet actions()}
			{#if assetData?.asset.externalId}
				<Button variant="outline" size="sm" onclick={EditAsset}>Edit</Button>
			{/if}
			<Button variant="outline" size="sm" onclick={SetVisibility}>
				{hidden ? "Show" : "Hide"}
			</Button>
			<Button variant="outline" size="sm" onclick={ResetAsset}>Reset</Button>
			<Button variant="destructive" size="sm" onclick={DeleteAsset}>Delete</Button>
		{/snippet}
	</PageHeader>

	{#if assetData?.asset.symbol && !assetData?.asset.name}
		<!-- Unlinked asset: show linking UI -->
		{#if !selectedCoin}
			<Card.Root>
				<Card.Header>
					<Card.Title>Asset-Typ</Card.Title>
				</Card.Header>
				<Card.Content class="flex items-center gap-3">
					<select
						class="border-input bg-background focus:border-ring focus:ring-ring rounded-lg border px-3 py-2 pe-9 text-sm focus:ring-1"
						bind:value={selectedAssetType}
					>
						<option value="Fiat">Fiat</option>
						<option value="Crypto">Crypto</option>
						<option value="Stock">Stock</option>
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
					class="border-input bg-background focus:border-ring focus:ring-ring rounded-lg border px-3 py-2 pe-9 text-sm focus:ring-1"
					bind:value={selectedCoin}
				>
					{#key assetType}
						{#if assetType === "Fiat"}
							{#await api.findFiatBySymbol(assetData.asset.symbol) then coins}
								{#each coins.data as coin}
									<option value={coin.symbol}>{coin.name}</option>
								{/each}
							{/await}
						{:else if assetType === "Crypto"}
							{#await api.findCoinsBySymbol(assetData.asset.symbol) then coins}
								{#each coins.data as coin}
									<option value={coin.externalId}>{coin.name}</option>
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
									name: assetData?.asset.symbol ?? "",
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
									name: $baseCurrency,
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
									class="hover:border-primary/20 transition-all duration-200 group-hover:-translate-y-0.5 hover:shadow-md"
								>
									<Card.Content class="pt-6">
										<p class="text-muted-foreground text-sm font-medium">
											{integrationItem.integration.name}
										</p>
										<p class="mt-1 text-xl font-semibold">
											{integrationItem.amount != null
												? formatAmount(integrationItem.amount, assetData?.asset.assetType)
												: ""}
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
