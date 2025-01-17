<script lang="ts">
	import { Button } from '$lib/components/ui/button';
	import * as api from '$lib/cryptotrackerApi';
	import AssetTiles from './AssetTiles.svelte';
</script>

{#await api.getAssets()}
	<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-4 lg:grid-cols-6">
		<AssetTiles skeleton={true} />
	</div>
{:then assets}
	<Button href="/assets/add">Vermögenswert hinzufügen</Button>
	<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-4 lg:grid-cols-6">
		<AssetTiles assets={assets.data} hidden={false} />
	</div>
	{#if assets.data.filter((x) => x.isHidden).length > 0}
		<p class="mb-2 mt-10 border-b-2 text-center">Versteckte</p>
		<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-4 lg:grid-cols-6">
			<AssetTiles assets={assets.data} hidden={true} />
		</div>
	{/if}
{/await}
