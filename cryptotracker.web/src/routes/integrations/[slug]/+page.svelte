<script lang="ts">
	import { goto } from "$app/navigation";
	import { page } from "$app/state";
	import { resolve } from "$app/paths";
	import * as api from "$lib/cryptotrackerApi";
	import { onMount } from "svelte";
	import AssetMeasuringTiles from "./AssetMeasuringTiles.svelte";
	import EditIntegrationDialog from "./EditIntegrationDialog.svelte";
	import DeleteIntegrationDialog from "./DeleteIntegrationDialog.svelte";
	import Button from "$lib/components/ui/button/button.svelte";
	import { Skeleton } from "$lib/components/ui/skeleton";
	import CardWithDays from "$lib/components/ui/card/card-with-days.svelte";
	import PageHeader from "$lib/components/page-header.svelte";
	import IntegrationAvatar from "$lib/components/integration-avatar.svelte";
	import IntegrationTypeBadge from "$lib/components/integration-type-badge.svelte";
	import SyncStatusBadge from "$lib/components/sync-status-badge.svelte";
	import LineChart from "$lib/components/charts/LineChart.svelte";
	import { baseCurrency, updateIntervalMinutes } from "$lib/stores/config";
	import { formatCurrency } from "$lib/format";
	import { isStale } from "$lib/integrations/health";

	const slug = $derived(page.params.slug ?? "");

	// undefined = loading, null = not found / failed
	let details = $state<api.IntegrationDetails | null | undefined>(undefined);

	async function load() {
		try {
			const res = await api.getIntegrationDetails(slug);
			details = res.status === 200 && res.data ? res.data : null;
		} catch {
			details = null;
		}
	}
	onMount(load);

	const currentValue = $derived(
		(details?.measurings ?? []).reduce((acc, m) => acc + (m.totalValue ?? 0), 0)
	);
	const stale = $derived(details ? isStale(details.integration, $updateIntervalMinutes) : false);

	// ── Value over time ──
	let range = $state(30);
	let standings = $state<{ [key: string]: number } | null>(null);

	$effect(() => {
		const id = slug;
		const days = range;
		standings = null;
		api
			.getIntegrationStandingByDays(id, days)
			.then((res) => (standings = res.status === 200 ? res.data : {}))
			.catch(() => (standings = {}));
	});

	const chartDays = $derived(Object.keys(standings ?? {}).sort());
	const chartValues = $derived(chartDays.map((d) => standings?.[d] ?? 0));

	let editOpen = $state(false);
	let deleteOpen = $state(false);
</script>

<div class="space-y-6">
	{#if details === undefined}
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
		<div class="space-y-6">
			<Skeleton class="h-80 w-full rounded-4xl" />
			<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-3">
				<AssetMeasuringTiles skeleton={true} />
			</div>
		</div>
	{:else if details === null}
		<p class="text-muted-foreground">Integration not found.</p>
	{:else}
		<!-- Header -->
		<PageHeader
			title={details.integration.name ?? ""}
			subtitle={details.integration.description ?? undefined}
		>
			{#snippet media()}
				<IntegrationAvatar
					name={details!.integration.name}
					isManual={details!.integration.isManual}
					class="size-12 text-lg"
				/>
			{/snippet}
			{#snippet meta()}
				<span class="bg-primary/10 text-primary rounded-full px-3 py-1 text-sm font-semibold">
					{formatCurrency(currentValue, $baseCurrency)}
				</span>
				<IntegrationTypeBadge isManual={details!.integration.isManual} />
				<SyncStatusBadge {stale} />
			{/snippet}
			{#snippet actions()}
				{#if details!.integration.isManual}
					<Button
						variant="outline"
						size="sm"
						href={resolve("/integrations/[slug]/measurings", {
							slug: details!.integration.id ?? ""
						})}
					>
						Measurements
					</Button>
				{/if}
				<Button variant="outline" size="sm" onclick={() => (editOpen = true)}>Edit</Button>
				<Button variant="destructive" size="sm" onclick={() => (deleteOpen = true)}>Delete</Button>
			{/snippet}
		</PageHeader>

		<!-- Value over time -->
		<CardWithDays title="Value" bind:selectedRange={range}>
			<LineChart
				fill={true}
				smooth={true}
				labels={chartDays}
				datasets={[{ name: $baseCurrency, data: chartValues }]}
				valueFormatter={(v) => formatCurrency(v, $baseCurrency)}
				skeleton={standings === null}
				class="aspect-auto h-64"
			/>
		</CardWithDays>

		<!-- Assets -->
		<div class="space-y-6">
			<div>
				<h2 class="mb-3 text-lg font-semibold">Included assets</h2>
				<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-3">
					<AssetMeasuringTiles measurings={details.measurings ?? []} hidden={false} />
				</div>
			</div>

			{#if details.measurings && details.measurings.filter((x) => x.asset.isHidden).length > 0}
				<div class="space-y-3">
					<div class="flex items-center gap-3">
						<div class="bg-border h-px grow"></div>
						<span class="text-muted-foreground text-sm font-medium">Hidden assets</span>
						<div class="bg-border h-px grow"></div>
					</div>
					<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-3">
						<AssetMeasuringTiles measurings={details.measurings ?? []} hidden={true} />
					</div>
				</div>
			{/if}
		</div>

		<EditIntegrationDialog integration={details.integration} bind:open={editOpen} onSaved={load} />
		<DeleteIntegrationDialog
			integration={details.integration}
			bind:open={deleteOpen}
			onDeleted={() => goto(resolve("/integrations"))}
		/>
	{/if}
</div>
