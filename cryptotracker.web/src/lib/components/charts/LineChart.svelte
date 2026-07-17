<script lang="ts">
	import { AreaChart, LineChart } from 'layerchart';
	import * as Chart from '$lib/components/ui/chart';
	import { Skeleton } from '$lib/components/ui/skeleton';
	import { colorForSymbol } from '$lib/charts/palette';
	import { prefersReducedMotion } from 'svelte/motion';

	type Dataset = { name: string; data: number[] };

	let {
		labels = [],
		datasets = [],
		fill = false,
		skeleton = false
	}: { labels?: string[]; datasets?: Dataset[]; fill?: boolean; skeleton?: boolean } = $props();

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
			color: datasets.length === 1 ? 'var(--chart-1)' : colorForSymbol(d.name, allNames)
		}))
	);
	const config: Chart.ChartConfig = $derived(
		Object.fromEntries(series.map((s) => [s.key, { label: s.label, color: s.color }]))
	);
	const Component = $derived(fill ? AreaChart : LineChart);
	const motion = $derived(
		prefersReducedMotion.current ? ('none' as const) : { type: 'tween' as const, duration: 400 }
	);
</script>

{#if skeleton}
	<Skeleton class="aspect-video w-full" />
{:else}
	<Chart.Container {config} class="aspect-video w-full">
		<Component
			data={rows}
			x="date"
			{series}
			legend={series.length > 1}
			props={{
				spline: { motion },
				area: { motion },
				xAxis: {
					format: (d: Date) => d.toLocaleDateString('de-CH', { day: '2-digit', month: '2-digit' })
				}
			}}
		>
			{#snippet tooltip()}
				<Chart.Tooltip labelFormatter={(d: Date) => d.toLocaleDateString('de-CH')} />
			{/snippet}
		</Component>
	</Chart.Container>
{/if}
