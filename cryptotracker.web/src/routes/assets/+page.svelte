<script lang="ts">
	import { Button } from '$lib/components/ui/button';
	import * as api from '$lib/cryptotrackerApi';
	import AssetTiles from './AssetTiles.svelte';

	// assets is streamed from assets/+page.ts (unawaited promise)
	let { data } = $props();
</script>

{#snippet assetTileGrid(assets: api.AssetDto[], hidden: boolean, skeleton = false)}
	<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
		<AssetTiles {assets} {hidden} {skeleton} />
	</div>
{/snippet}

<div class="space-y-6">
	<div class="flex items-center justify-between">
		<h1 class="text-2xl font-bold tracking-tight">Vermögenswerte</h1>
		<Button variant="outline" size="sm" href="/assets/add">+ Hinzufügen</Button>
	</div>

	{#await data.assets}
		{@render assetTileGrid([], false, true)}
	{:then assets}
		{@render assetTileGrid(assets, false)}

		{#if assets.filter((x) => x.isHidden).length > 0}
			<div class="space-y-4">
				<div class="flex items-center gap-3">
					<div class="bg-border h-px grow"></div>
					<span class="text-muted-foreground text-sm font-medium">Versteckte Vermögenswerte</span>
					<div class="bg-border h-px grow"></div>
				</div>
				{@render assetTileGrid(assets, true)}
			</div>
		{/if}
	{/await}
</div>
