<script lang="ts">
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";
	import * as Card from "$lib/components/ui/card";
	import { Button } from "$lib/components/ui/button";
	import { Skeleton } from "$lib/components/ui/skeleton";
	import * as ToggleGroup from "$lib/components/ui/toggle-group";
	import { baseCurrency } from "$lib/stores/config";
	import { formatCurrency, formatPercent } from "$lib/format";
	import { analyze, type Delta, type Mover } from "$lib/dashboard/analyze";
	import LineChart from "$lib/components/charts/LineChart.svelte";
	import PieChart from "$lib/components/charts/PieChart.svelte";
	import StatTile from "$lib/components/stat-tile.svelte";
	import TypeAllocationBar from "$lib/components/type-allocation-bar.svelte";
	import TrendingUpIcon from "@lucide/svelte/icons/trending-up";
	import TrendingDownIcon from "@lucide/svelte/icons/trending-down";

	let { data } = $props();

	const fmtValue = $derived((v: number) => formatCurrency(v, $baseCurrency));

	function setRange(value: string) {
		if (!value || Number(value) === data.range) return;
		// eslint-disable-next-line svelte/no-navigation-without-resolve -- query-only navigation on the current route
		goto(`${resolve("/")}?range=${value}`, { keepFocus: true, noScroll: true });
	}

	function direction(value: number): "up" | "down" | "flat" {
		return value > 0 ? "up" : value < 0 ? "down" : "flat";
	}

	function changeTile(change: Delta | null) {
		if (!change) return { value: "—", delta: null };
		return {
			value: `${change.value >= 0 ? "+" : ""}${formatCurrency(change.value, $baseCurrency)}`,
			delta:
				change.pct !== null
					? { text: formatPercent(change.pct), direction: direction(change.value) }
					: null
		};
	}

	function moverTile(mover: Mover | null) {
		if (!mover) return { value: "—", delta: null, href: undefined };
		return {
			value: mover.symbol,
			delta: { text: formatPercent(mover.pct), direction: direction(mover.value) },
			href: resolve("/assets/[slug]", { slug: mover.symbol })
		};
	}
</script>

{#await data.measurings}
	<div class="space-y-4">
		<Skeleton class="h-88 w-full rounded-4xl" />
		<div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
			<Skeleton class="h-28 w-full rounded-4xl" />
			<Skeleton class="h-28 w-full rounded-4xl" />
			<Skeleton class="h-28 w-full rounded-4xl" />
			<Skeleton class="h-28 w-full rounded-4xl" />
		</div>
		<div class="grid gap-4 md:grid-cols-2">
			<Skeleton class="h-80 w-full rounded-4xl" />
			<Skeleton class="h-80 w-full rounded-4xl" />
		</div>
	</div>
{:then measurings}
	{@const d = analyze(measurings)}
	{#if d.empty}
		<Card.Root>
			<Card.Content class="flex flex-col items-center gap-4 py-16 text-center">
				<p class="text-lg font-semibold">No data yet</p>
				<p class="text-muted-foreground max-w-md text-sm">
					Connect an exchange or add a manual integration with your first measurement — the
					dashboard fills up from there.
				</p>
				<Button href={resolve("/integrations")}>Go to integrations</Button>
			</Card.Content>
		</Card.Root>
	{:else}
		{@const change24h = changeTile(d.kpis.change24h)}
		{@const change7d = changeTile(d.kpis.change7d)}
		{@const gainer = moverTile(d.kpis.topGainer)}
		{@const loser = moverTile(d.kpis.topLoser)}
		<div class="space-y-4">
			<!-- Hero: the one gradient surface of the app. Everything on it is
			     primary-foreground only — no delta/series colors (R2). -->
			<Card.Root
				class="from-primary to-primary/75 text-primary-foreground gap-0 overflow-hidden border-0 bg-linear-to-br py-0"
			>
				<Card.Content class="p-0">
					<LineChart
						fill={true}
						smooth={true}
						gradientFill={true}
						labels={d.days}
						datasets={[{ name: $baseCurrency, data: d.totals }]}
						color="var(--primary-foreground)"
						axis={false}
						grid={false}
						valueFormatter={fmtValue}
						class="aspect-auto h-44"
					/>
					<div class="space-y-4 px-6 pt-4 pb-8">
						<span class="text-primary-foreground/80 text-sm font-medium">Portfolio</span>
						<div class="text-5xl font-bold tracking-tight md:text-6xl">
							{formatCurrency(d.current, $baseCurrency)}
						</div>
						<div class="flex flex-wrap items-end justify-between gap-4">
							{#if d.deltaPct !== null}
								<div class="flex items-center gap-2 text-sm">
									{#if d.delta >= 0}
										<TrendingUpIcon class="size-4" />
									{:else}
										<TrendingDownIcon class="size-4" />
									{/if}
									<span class="font-medium">
										{d.delta >= 0 ? "+" : ""}{formatCurrency(d.delta, $baseCurrency)}
										({formatPercent(d.deltaPct)})
									</span>
									<span class="text-primary-foreground/70">last {data.range} days</span>
								</div>
							{:else}
								<div></div>
							{/if}
							<ToggleGroup.Root
								type="single"
								value={String(data.range)}
								onValueChange={setRange}
								class="gap-1"
							>
								{#each data.ranges as range (range)}
									<ToggleGroup.Item
										value={String(range)}
										class="text-primary-foreground/70 hover:text-primary-foreground data-[state=on]:bg-primary-foreground/20 data-[state=on]:text-primary-foreground h-8 px-3 text-xs"
									>
										{range}D
									</ToggleGroup.Item>
								{/each}
							</ToggleGroup.Root>
						</div>
					</div>
				</Card.Content>
			</Card.Root>

			<!-- Snapshot deltas, not P&L: without transaction data, deposits and
			     price moves are indistinguishable — label as "change" only. -->
			<div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
				<StatTile
					label="24h change"
					value={change24h.value}
					delta={change24h.delta}
					deltaLabel="vs yesterday"
				/>
				<StatTile
					label="7d change"
					value={change7d.value}
					delta={change7d.delta}
					deltaLabel="vs 7 days ago"
				/>
				<StatTile
					label="Top gainer (24h)"
					value={gainer.value}
					delta={gainer.delta}
					href={gainer.href}
				/>
				<StatTile
					label="Top loser (24h)"
					value={loser.value}
					delta={loser.delta}
					href={loser.href}
				/>
			</div>

			<div class="grid gap-4 md:grid-cols-2">
				<Card.Root>
					<Card.Header>
						<Card.Title>Allocation</Card.Title>
					</Card.Header>
					<Card.Content class="space-y-6">
						<PieChart
							labels={d.trimmedLatest.map((x) => x.asset.symbol ?? "")}
							values={d.trimmedLatest.map((x) => x.totalValue ?? 0)}
							valueFormatter={fmtValue}
						/>
						<TypeAllocationBar segments={d.kpis.typeAllocation} />
					</Card.Content>
				</Card.Root>
				<Card.Root>
					<Card.Header>
						<Card.Title>Composition</Card.Title>
					</Card.Header>
					<Card.Content>
						<LineChart
							labels={d.days}
							datasets={d.composition}
							fill={true}
							stacked={true}
							valueFormatter={fmtValue}
							class="aspect-auto h-72"
						/>
					</Card.Content>
				</Card.Root>
			</div>
		</div>
	{/if}
{:catch}
	<p class="text-muted-foreground">Could not load the dashboard — please try again.</p>
{/await}
