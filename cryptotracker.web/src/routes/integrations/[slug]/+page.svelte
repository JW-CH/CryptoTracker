<script lang="ts">
	import { page } from '$app/state';
	import * as api from '$lib/cryptotrackerApi';
	import { onMount } from 'svelte';
	import AssetMeasuringTiles from './AssetMeasuringTiles.svelte';
	import Button from '$lib/components/ui/button/button.svelte';
	import { Badge } from '$lib/components/ui/badge';
	import { Skeleton } from '$lib/components/ui/skeleton';
	import PageHeader from '$lib/components/page-header.svelte';

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
				<Skeleton class="bg-muted size-12 rounded-full" />
				<div>
					<Skeleton class="bg-muted mb-1 h-7 w-40" />
					<Skeleton class="bg-muted h-4 w-24" />
				</div>
			</div>
		</div>

		<!-- Content Skeleton -->
		<div>
			<Skeleton class="bg-muted mb-3 h-6 w-32" />
			<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-3">
				<AssetMeasuringTiles skeleton={true} />
			</div>
		</div>
	{:else}
		<!-- Header -->
		<PageHeader
			title={details.integration.name ?? ''}
			subtitle={details.integration.description ?? undefined}
		>
			{#snippet media()}
				<div
					class="flex size-12 items-center justify-center rounded-full text-lg font-bold {details
						.integration.isManual
						? 'bg-secondary text-secondary-foreground'
						: 'bg-primary/10 text-primary'}"
				>
					{details.integration.name ? details.integration.name.slice(0, 2).toUpperCase() : '??'}
				</div>
			{/snippet}
			{#snippet meta()}
				<Badge variant={details.integration.isManual ? 'secondary' : 'default'}>
					{details.integration.isManual ? 'Manual' : 'Automatic'}
				</Badge>
			{/snippet}
			{#snippet actions()}
				{#if details.integration.isManual}
					<Button variant="outline" size="sm" href="/integrations/{details.integration.id}/add">
						+ Measurement
					</Button>
					<Button
						variant="outline"
						size="sm"
						href="/integrations/{details.integration.id}/measurings"
					>
						Manage
					</Button>
				{/if}
			{/snippet}
		</PageHeader>

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
						<div class="bg-border h-px grow"></div>
						<span class="text-muted-foreground text-sm font-medium">Versteckte Vermögenswerte</span>
						<div class="bg-border h-px grow"></div>
					</div>
					<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-3">
						<AssetMeasuringTiles measurings={details.measurings!} hidden={true} />
					</div>
				</div>
			{/if}
		</div>
	{/if}
</div>
