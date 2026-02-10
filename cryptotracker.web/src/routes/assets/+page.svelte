<script lang="ts">
	import { Button } from '$lib/components/ui/button';
	import * as api from '$lib/cryptotrackerApi';
	import { onMount } from 'svelte';
	import AssetTiles from './AssetTiles.svelte';

	let assets: api.Asset[] | null = null;

	onMount(async () => {
		let request = await api.getAssets();

		if (request.data) {
			assets = request.data;
		}
	});
</script>

<div class="space-y-6">
	<div class="flex items-center justify-between">
		<h1 class="text-2xl font-bold tracking-tight">Vermögenswerte</h1>
		<Button variant="outline" size="sm" href="/assets/add">+ Hinzufügen</Button>
	</div>

	<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
		{#key assets}
			<AssetTiles skeleton={assets == null} assets={assets ?? []} hidden={false} />
		{/key}
	</div>

	{#if assets != null && assets.filter((x) => x.isHidden).length > 0}
		<div class="space-y-4">
			<div class="flex items-center gap-3">
				<div class="bg-border h-px grow"></div>
				<span class="text-muted-foreground text-sm font-medium">Versteckte Vermögenswerte</span>
				<div class="bg-border h-px grow"></div>
			</div>
			<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
				<AssetTiles {assets} hidden={true} />
			</div>
		</div>
	{/if}
</div>
