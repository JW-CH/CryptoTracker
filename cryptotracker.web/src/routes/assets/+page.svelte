<script lang="ts">
	import { resolve } from "$app/paths";
	import { Button } from "$lib/components/ui/button";
	import * as api from "$lib/cryptotrackerApi";
	import AssetTiles from "./AssetTiles.svelte";
	import PageHeader from "$lib/components/page-header.svelte";

	// portfolio (assets + latest holdings) is streamed from assets/+page.ts
	let { data } = $props();
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
			<Button variant="outline" size="sm" href={resolve("/assets/add")}>+ Add</Button>
		{/snippet}
	</PageHeader>

	{#await data.portfolio}
		{@render assetTileGrid([], {}, false, true)}
	{:then portfolio}
		{#if portfolio.assets.length === 0}
			<div class="text-muted-foreground py-16 text-center">
				<p class="text-foreground text-lg font-semibold">No assets yet</p>
				<p class="mt-1 text-sm">Add your first asset to start tracking.</p>
			</div>
		{:else}
			{@render assetTileGrid(portfolio.assets, portfolio.holdingsBySymbol, false)}

			{#if portfolio.assets.filter((x) => x.isHidden).length > 0}
				<div class="space-y-4">
					<div class="flex items-center gap-3">
						<div class="bg-border h-px grow"></div>
						<span class="text-muted-foreground text-sm font-medium">Hidden assets</span>
						<div class="bg-border h-px grow"></div>
					</div>
					{@render assetTileGrid(portfolio.assets, portfolio.holdingsBySymbol, true)}
				</div>
			{/if}
		{/if}
	{/await}
</div>
