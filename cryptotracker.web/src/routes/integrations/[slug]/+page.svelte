<script lang="ts">
	import { page } from '$app/state';
	import * as api from '$lib/cryptotrackerApi';
	import { onMount } from 'svelte';
	import AssetMeasuringTiles from './AssetMeasuringTiles.svelte';
	import Button from '$lib/components/ui/button/button.svelte';
	import { Skeleton } from '$lib/components/ui/skeleton';

	let isLoading: boolean = true;
	let details: api.IntegrationDetails;

	onMount(async () => {
		let request = await api.getIntegrationDetails(page.params.slug ?? '');
		details = request.data;
		isLoading = false;
	});
</script>

<div class="space-y-6">
	{#if isLoading}
		<!-- Header Skeleton -->
		<div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
			<div class="flex items-center gap-4">
				<Skeleton class="size-12 rounded-full bg-muted" />
				<div>
					<Skeleton class="mb-1 h-7 w-40 bg-muted" />
					<Skeleton class="h-4 w-24 bg-muted" />
				</div>
			</div>
		</div>

		<!-- Content Skeleton -->
		<div>
			<Skeleton class="mb-3 h-6 w-32 bg-muted" />
			<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-3">
				<AssetMeasuringTiles skeleton={true} />
			</div>
		</div>
	{:else}
		<!-- Header -->
		<div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
			<div class="flex items-center gap-4">
				<div
					class="flex size-12 items-center justify-center rounded-full text-lg font-bold {details.integration.isManual
						? 'bg-secondary text-secondary-foreground'
						: 'bg-primary/10 text-primary'}"
				>
					{details.integration.name
						? details.integration.name.slice(0, 2).toUpperCase()
						: '??'}
				</div>
				<div>
					<div class="flex items-center gap-3">
						<h1 class="text-2xl font-bold tracking-tight">{details.integration.name}</h1>
						<span
							class="rounded-full px-2 py-0.5 text-xs font-medium {details.integration.isManual
								? 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-400'
								: 'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-400'}"
						>
							{details.integration.isManual ? 'Manuell' : 'Automatisch'}
						</span>
					</div>
					{#if details.integration.description}
						<p class="text-sm text-muted-foreground">{details.integration.description}</p>
					{/if}
				</div>
			</div>
			{#if details.integration.isManual}
				<div class="flex gap-2">
					<Button variant="outline" size="sm" href="/integrations/{details.integration.id}/add">
						+ Messung
					</Button>
					<Button
						variant="outline"
						size="sm"
						href="/integrations/{details.integration.id}/measurings"
					>
						Verwalten
					</Button>
				</div>
			{/if}
		</div>

		<!-- Assets -->
		<div class="space-y-6">
			<div>
				<h2 class="mb-3 text-lg font-semibold">Inkludierte Vermögenswerte</h2>
				<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-3">
					<AssetMeasuringTiles measurings={details.measurings!} hidden={false} />
				</div>
			</div>

			{#if details.measurings && details.measurings.filter((x) => x.asset.isHidden).length > 0}
				<div class="space-y-3">
					<div class="flex items-center gap-3">
						<div class="h-px grow bg-border"></div>
						<span class="text-sm font-medium text-muted-foreground">Versteckte Vermögenswerte</span>
						<div class="h-px grow bg-border"></div>
					</div>
					<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-3">
						<AssetMeasuringTiles measurings={details.measurings!} hidden={true} />
					</div>
				</div>
			{/if}
		</div>
	{/if}
</div>
