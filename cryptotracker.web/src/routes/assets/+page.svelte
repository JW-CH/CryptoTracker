<script lang="ts">
	import { resolve } from '$app/paths';
	import { Button } from '$lib/components/ui/button';
	import * as api from '$lib/cryptotrackerApi';
	import AssetTiles from './AssetTiles.svelte';
	import PageHeader from '$lib/components/page-header.svelte';

	// assets is streamed from assets/+page.ts (unawaited promise)
	let { data } = $props();
</script>

{#snippet assetTileGrid(assets: api.AssetDto[], hidden: boolean, skeleton = false)}
	<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
		<AssetTiles {assets} {hidden} {skeleton} />
	</div>
{/snippet}

<svelte:head>
	<title>Assets · CryptoTracker</title>
</svelte:head>

<div class="space-y-6">
	<PageHeader title="Assets">
		{#snippet actions()}
			<Button variant="outline" size="sm" href={resolve('/assets/add')}>+ Add</Button>
		{/snippet}
	</PageHeader>

	{#await data.assets}
		{@render assetTileGrid([], false, true)}
	{:then assets}
		{@render assetTileGrid(assets, false)}

		{#if assets.filter((x) => x.isHidden).length > 0}
			<div class="space-y-4">
				<div class="flex items-center gap-3">
					<div class="bg-border h-px grow"></div>
					<span class="text-muted-foreground text-sm font-medium">Hidden assets</span>
					<div class="bg-border h-px grow"></div>
				</div>
				{@render assetTileGrid(assets, true)}
			</div>
		{/if}
	{/await}
</div>
