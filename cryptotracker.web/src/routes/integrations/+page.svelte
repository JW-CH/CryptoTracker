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

<div class="space-y-6">
	<div class="flex items-center justify-between">
		<h1 class="text-2xl font-bold tracking-tight">Integrationen</h1>
		<Button variant="outline" size="sm" href="/integrations/add">+ Hinzufügen</Button>
	</div>

	<div class="grid gap-4 sm:grid-cols-1 md:grid-cols-2 lg:grid-cols-3">
		<IntegrationTiles skeleton={integrations == null} integrations={integrations ?? []} />
	</div>
</div>
