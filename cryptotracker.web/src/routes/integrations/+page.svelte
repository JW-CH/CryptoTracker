<script lang="ts">
	import Button from '$lib/components/ui/button/button.svelte';
	import * as api from '$lib/cryptotrackerApi';
	import { onMount } from 'svelte';
	import IntegrationTiles from './IntegrationTiles.svelte';

	let integrations: api.IntegrationDto[] | null = null;

	onMount(async () => {
		let request = await api.getIntegrations();

		if (request.data) {
			integrations = request.data;
		}
	});
</script>

<Button href="/integrations/add">Manuelle Integration hinzufügen</Button>
<div class="grid gap-4 sm:grid-cols-1 md:grid-cols-2 lg:grid-cols-2">
	<IntegrationTiles
		skeleton={integrations == null}
		integrations={integrations ?? []}
		hidden={false}
	/>
</div>
{#if integrations != null && integrations.filter((x) => x.isHidden).length > 0}
	<p class="mb-2 mt-10 border-b-2 text-center">Versteckte</p>
	<div class="grid gap-4 sm:grid-cols-1 md:grid-cols-2 lg:grid-cols-2">
		<IntegrationTiles {integrations} hidden={true} />
	</div>
{/if}
