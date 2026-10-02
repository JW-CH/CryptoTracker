<script lang="ts">
	import { resolve } from "$app/paths";
	import Button from "#lib/components/ui/button/button.svelte";
	import * as Card from "#lib/components/ui/card/index.js";
	import IntegrationTiles from "./IntegrationTiles.svelte";
	import PageHeader from "#lib/components/page-header.svelte";
	import { streamed } from "#lib/api/streamed.svelte.js";

	let { data } = $props();

	const integrations = streamed(() => data.integrations);
</script>

<svelte:head>
	<title>Integrations · CryptoTracker</title>
</svelte:head>

<div class="space-y-6">
	<PageHeader title="Integrations">
		{#snippet actions()}
			<Button variant="outline" size="sm" href={resolve("integrations/add")}>+ Add</Button>
		{/snippet}
	</PageHeader>

	{#if integrations.value === null && !integrations.failed}
		<div class="grid gap-4 sm:grid-cols-1 md:grid-cols-2 lg:grid-cols-3">
			<IntegrationTiles skeleton />
		</div>
	{:else if integrations.failed}
		<p class="text-muted-foreground">Could not load integrations — please try again.</p>
	{:else if integrations.value?.length === 0}
		<Card.Root>
			<Card.Content class="flex flex-col items-center gap-4 py-16 text-center">
				<p class="text-lg font-semibold">No integrations yet</p>
				<p class="text-muted-foreground max-w-md text-sm">
					Automatic integrations come from the server configuration; manual ones track wallets and
					accounts you update yourself.
				</p>
				<Button href={resolve("integrations/add")}>Add a manual integration</Button>
			</Card.Content>
		</Card.Root>
	{:else}
		<div class="grid gap-4 sm:grid-cols-1 md:grid-cols-2 lg:grid-cols-3">
			<IntegrationTiles integrations={integrations.value ?? []} />
		</div>
	{/if}
</div>
