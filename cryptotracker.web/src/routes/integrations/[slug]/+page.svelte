<script lang="ts">
	import { page } from '$app/state';
	import * as api from '$lib/cryptotrackerApi';
	import { onMount } from 'svelte';
	import AssetMeasuringTiles from './AssetMeasuringTiles.svelte';
	import Button from '$lib/components/ui/button/button.svelte';

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
	{#if details.integration.isManual}
		<Button href="/integrations/add/{details.integration.id}">Manuelle Messung hinzufügen</Button>
		<Button>Reset</Button>
	{/if}
	<p>{details.integration.name}</p>
	<p class="text-center">Inkludiert:</p>
	<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-3">
		<AssetMeasuringTiles measurings={details.measurings!} hidden={false} />
	</div>
	<p class="text-center">Versteckt:</p>
	<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-3">
		<AssetMeasuringTiles measurings={details.measurings!} hidden={true} />
	</div>
{/if}
