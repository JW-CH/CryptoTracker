<script lang="ts">
	import { resolve } from "$app/paths";
	import Button from "$lib/components/ui/button/button.svelte";
	import * as api from "$lib/cryptotrackerApi";
	import { onMount } from "svelte";
	import IntegrationTiles from "./IntegrationTiles.svelte";
	import PageHeader from "$lib/components/page-header.svelte";

	let integrations: api.IntegrationDto[] | null = null;

	onMount(async () => {
		let request = await api.getIntegrations();

		if (request.data) {
			integrations = request.data;
		}
	});
</script>

<svelte:head>
	<title>Integrations · CryptoTracker</title>
</svelte:head>

<div class="space-y-6">
	<PageHeader title="Integrations">
		{#snippet actions()}
			<Button variant="outline" size="sm" href={resolve("/integrations/add")}>+ Add</Button>
		{/snippet}
	</PageHeader>

	<div class="grid gap-4 sm:grid-cols-1 md:grid-cols-2 lg:grid-cols-3">
		<IntegrationTiles skeleton={integrations == null} integrations={integrations ?? []} />
	</div>
</div>
