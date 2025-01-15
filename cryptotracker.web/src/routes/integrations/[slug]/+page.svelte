<script lang="ts">
	import { page } from '$app/state';
	import * as api from '$lib/cryptotrackerApi';
	import { onMount } from 'svelte';
	import AssetMeasuringTiles from './AssetMeasuringTiles.svelte';

	let isLoading: boolean = true;
	let details: api.IntegrationDetails;

	onMount(async () => {
		let request = await api.getIntegrationDetails(page.params.slug);
		details = request.data;
		isLoading = false;
	});
</script>

{#if isLoading}
	<p class="text-center">Inkludiert:</p>
	<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-3">
		<AssetMeasuringTiles skeleton={true} />
	</div>
{:else}
	<p>{details.name}</p>
	<p class="text-center">Inkludiert:</p>
	<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-3">
		<AssetMeasuringTiles measurings={details.measurings!} hidden={false} />
	</div>
	<p class="text-center">Versteckt:</p>
	<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-3">
		<AssetMeasuringTiles measurings={details.measurings!} hidden={true} />
	</div>
{/if}
