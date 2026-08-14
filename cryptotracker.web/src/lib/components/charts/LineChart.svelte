<script lang="ts">
	import { AreaChart, LineChart } from "layerchart";
	import { curveMonotoneX } from "d3-shape";
	import * as Chart from "$lib/components/ui/chart";
	import { Skeleton } from "$lib/components/ui/skeleton";
	import { colorForSymbol } from "$lib/charts/palette";
	import { LOCALE, formatDate } from "$lib/format";
	import { prefersReducedMotion } from "svelte/motion";
	import { cn } from "$lib/utils";

	type Dataset = { name: string; data: number[] };

	let {
		labels = [],
		datasets = [],
		fill = false,
		skeleton = false,
		color,
		axis = true,
		grid = true,
		smooth = false,
		gradientFill = false,
		stacked = false,
		valueFormatter,
		class: className
	}: {
		labels?: string[];
		datasets?: Dataset[];
		fill?: boolean;
		skeleton?: boolean;
		/** Overrides the single-series color, e.g. for the hero gradient */
		color?: string;
		axis?: boolean | "x" | "y";
		grid?: boolean;
		/** Monotone curve instead of straight segments */
		smooth?: boolean;
		/** Vertical gradient fill fading downwards (single series, fill mode) */
		gradientFill?: boolean;
		/** Stack the series (fill mode only) */
		stacked?: boolean;
		/** Formats tooltip values, e.g. as currency */
		valueFormatter?: (value: number) => string;
		class?: string;
	} = $props();

	const uid = $props.id();
	const singleColor = $derived(color ?? "var(--chart-1)");

	const rows = $derived(
		labels.map((label, i) => ({
			date: new Date(label),
			...Object.fromEntries(datasets.map((d) => [d.name, d.data[i] ?? 0]))
		}))
	);
	const allNames = $derived(datasets.map((d) => d.name));
	const series = $derived(
		datasets.map((d) => ({
			key: d.name,
			label: d.name,
			color: datasets.length === 1 ? (color ?? "var(--chart-1)") : colorForSymbol(d.name, allNames)
		}))
	);
	const config: Chart.ChartConfig = $derived(
		Object.fromEntries(series.map((s) => [s.key, { label: s.label, color: s.color }]))
	);
	const Component = $derived(fill ? AreaChart : LineChart);
	const motion = $derived(
		prefersReducedMotion.current ? ("none" as const) : { type: "tween" as const, duration: 400 }
	);
</script>

{#if skeleton}
	<Skeleton class={cn("aspect-video w-full", className)} />
{:else}
	{#if gradientFill}
		<!-- Same-document SVG paint server for the area fill -->
		<svg aria-hidden="true" class="absolute size-0">
			<defs>
				<linearGradient id="lc-fill-{uid}" x1="0" y1="0" x2="0" y2="1">
					<stop offset="0%" stop-color={singleColor} stop-opacity="0.55" />
					<stop offset="100%" stop-color={singleColor} stop-opacity="0" />
				</linearGradient>
			</defs>
		</svg>
	{/if}
	<Chart.Container {config} class={cn("aspect-video w-full", className)}>
		<Component
			data={rows}
			x="date"
			{series}
			{axis}
			{grid}
			legend={series.length > 1}
			{...fill && stacked ? { seriesLayout: "stack" as const } : {}}
			props={{
				spline: { motion, ...(smooth ? { curve: curveMonotoneX } : {}) },
				area: {
					motion,
					...(smooth ? { curve: curveMonotoneX } : {}),
					...(gradientFill ? { fill: `url(#lc-fill-${uid})`, fillOpacity: 1 } : {})
				},
				xAxis: {
					format: (d: Date) => d.toLocaleDateString(LOCALE, { day: "2-digit", month: "2-digit" })
				}
			}}
		>
			{#snippet tooltip()}
				<Chart.Tooltip
					labelFormatter={(d: Date) => formatDate(d)}
					{valueFormatter}
					sortItems={series.length > 1}
					showTotal={series.length > 1}
					maxItems={9}
					indicator={series.length > 1 ? "line" : "dot"}
				/>
			{/snippet}
		</Component>
	</Chart.Container>
{/if}
