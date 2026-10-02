<script lang="ts">
	import { resolve } from "$app/paths";
	import { Button } from "#lib/components/ui/button/index.js";
	import * as api from "#lib/cryptotrackerApi.js";
	import AssetTiles from "./AssetTiles.svelte";
	import PageHeader from "#lib/components/page-header.svelte";
	import { streamed } from "#lib/api/streamed.svelte.js";

	let { data } = $props();

	const portfolio = streamed(() => data.portfolio);
</script>

{#snippet assetTileGrid(
	assets: api.AssetDto[],
	holdings: Record<string, api.AssetHoldingDto>,
	hidden: boolean,
	skeleton = false
)}
	<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
		<AssetTiles {assets} {holdings} {hidden} {skeleton} />
	</div>
{/snippet}

<svelte:head>
	<title>Assets · CryptoTracker</title>
</svelte:head>

<div class="space-y-6">
	<PageHeader title="Assets">
		{#snippet actions()}
			<Button variant="outline" size="sm" href={resolve("assets/add")}>+ Add</Button>
		{/snippet}
	</PageHeader>

	{#if portfolio.value === null}
		{@render assetTileGrid([], {}, false, true)}
	{:else if portfolio.value.assets.length === 0}
		<div class="text-muted-foreground py-16 text-center">
			<p class="text-foreground text-lg font-semibold">No assets yet</p>
			<p class="mt-1 text-sm">Add your first asset to start tracking.</p>
		</div>
	{:else}
		{@render assetTileGrid(portfolio.value.assets, portfolio.value.holdingsBySymbol, false)}

		{#if portfolio.value.assets.filter((x) => x.isHidden).length > 0}
			<div class="space-y-4">
				<div class="flex items-center gap-3">
					<div class="bg-border h-px grow"></div>
					<span class="text-muted-foreground text-sm font-medium">Hidden assets</span>
					<div class="bg-border h-px grow"></div>
				</div>
				{@render assetTileGrid(portfolio.value.assets, portfolio.value.holdingsBySymbol, true)}
			</div>
		{/if}
	{/if}
</div>
