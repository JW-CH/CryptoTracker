<script lang="ts">
	import Button from '$lib/components/ui/button/button.svelte';
	import * as api from '$lib/cryptotrackerApi';
	import IntegrationTiles from './IntegrationTiles.svelte';
</script>

{#await api.getIntegrations()}
	<div class="grid gap-4 sm:grid-cols-1 md:grid-cols-2 lg:grid-cols-2">
		<IntegrationTiles skeleton={true} />
	</div>
{:then integrations}
	<Button href="/integrations/add">Manuelle Integration hinzufügen</Button>
	<div class="grid gap-4 sm:grid-cols-1 md:grid-cols-2 lg:grid-cols-2">
		<IntegrationTiles integrations={integrations.data} hidden={false} />
	</div>
	{#if integrations.data.filter((x) => x.isHidden).length > 0}
		<p class="mb-2 mt-10 border-b-2 text-center">Versteckte</p>
		<div class="grid gap-4 sm:grid-cols-1 md:grid-cols-2 lg:grid-cols-2">
			<IntegrationTiles integrations={integrations.data} hidden={true} />
		</div>
	{/if}
{/await}
