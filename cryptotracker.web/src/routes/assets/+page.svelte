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

<Button href="/assets/add">Vermögenswert hinzufügen</Button>
<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-4 lg:grid-cols-6">
	{#key assets}
		<AssetTiles skeleton={assets == null} assets={assets ?? []} hidden={false} />
	{/key}
</div>
{#if assets != null && assets.filter((x) => x.isHidden).length > 0}
	<p class="mb-2 mt-10 border-b-2 text-center">Versteckte</p>
	<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-4 lg:grid-cols-6">
		<AssetTiles {assets} hidden={true} />
	</div>
{/if}
