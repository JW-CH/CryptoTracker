<script lang="ts">
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";
	import * as Card from "$lib/components/ui/card";
	import { Button } from "$lib/components/ui/button";
	import { Skeleton } from "$lib/components/ui/skeleton";
	import * as ToggleGroup from "$lib/components/ui/toggle-group";
	import * as api from "$lib/cryptotrackerApi";
	import { baseCurrency } from "$lib/stores/config";
	import { formatCurrency, formatPercent } from "$lib/format";
	import LineChart from "$lib/components/charts/LineChart.svelte";
	import PieChart from "$lib/components/charts/PieChart.svelte";
	import TrendingUpIcon from "@lucide/svelte/icons/trending-up";
	import TrendingDownIcon from "@lucide/svelte/icons/trending-down";

	let { data } = $props();

	function setRange(value: string) {
		if (!value || Number(value) === data.range) return;
		// eslint-disable-next-line svelte/no-navigation-without-resolve -- query-only navigation on the current route
		goto(`${resolve("/")}?range=${value}`, { keepFocus: true, noScroll: true });
	}

	function TrimMeasurings(holdings: api.AssetHoldingDto[]) {
		const sorted = [...holdings].sort((a, b) => (b.totalValue ?? 0) - (a.totalValue ?? 0));
		if (sorted.length <= 7) {
			return sorted;
		}

		const top = sorted.slice(0, 7);
		const otherValue = sorted.slice(7).reduce((acc, curr) => acc + (curr.totalValue ?? 0), 0);
		return top.concat({
			asset: { symbol: "Other", assetType: "Crypto" },
			totalValue: otherValue,
			price: 0,
			totalAmount: 0,
			integrationValues: []
		});
	}

	function analyze(measurings: { [key: string]: api.AssetHoldingDto[] }) {
		const days = Object.keys(measurings).sort();
		const totals = days.map((d) => measurings[d].reduce((acc, m) => acc + (m.totalValue ?? 0), 0));
		const current = totals.at(-1) ?? 0;
		const first = totals[0] ?? 0;
		const delta = current - first;
		const deltaPct = first !== 0 ? delta / first : null;

		const latest = days.length ? measurings[days.at(-1)!] : [];
		const trimmedLatest = TrimMeasurings(latest);

		const composition = latest
			.sort((a, b) => (b.totalValue ?? 0) - (a.totalValue ?? 0))
			.map((entry) => {
				const symbol = entry.asset.symbol ?? "";
				return {
					name: symbol,
					data: days.map(
						(d) => measurings[d].find((m) => m.asset.symbol === symbol)?.totalValue ?? 0
					)
				};
			});

		return {
			days,
			totals,
			current,
			delta,
			deltaPct,
			trimmedLatest,
			composition,
			empty: latest.length === 0
		};
	}
</script>

{#await data.measurings}
	<div class="space-y-4">
		<Skeleton class="h-88 w-full rounded-4xl" />
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

			<div class="grid gap-4 md:grid-cols-2">
				<Card.Root>
					<Card.Header>
						<Card.Title>Allocation</Card.Title>
					</Card.Header>
					<Card.Content>
						<PieChart
							labels={d.trimmedLatest.map((x) => x.asset.symbol ?? "")}
							values={d.trimmedLatest.map((x) => x.totalValue ?? 0)}
						/>
					</Card.Content>
				</Card.Root>
				<Card.Root>
					<Card.Header>
						<Card.Title>Composition</Card.Title>
					</Card.Header>
					<Card.Content>
						<LineChart labels={d.days} datasets={d.composition} />
					</Card.Content>
				</Card.Root>
			</div>
		</div>
	{/if}
{:catch}
	<p class="text-muted-foreground">Could not load the dashboard — please try again.</p>
{/await}
