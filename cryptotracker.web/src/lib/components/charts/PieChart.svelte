<script lang="ts">
	import { PieChart } from "layerchart";
	import * as Chart from "$lib/components/ui/chart";
	import { Skeleton } from "$lib/components/ui/skeleton";
	import { colorForSymbol, OTHER_SYMBOL } from "$lib/charts/palette";
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";
	import { prefersReducedMotion } from "svelte/motion";

	let {
		labels = [],
		values = [],
		skeleton = false
	}: { labels?: string[]; values?: number[]; skeleton?: boolean } = $props();

	const data = $derived(
		labels.map((label, i) => ({
			label,
			value: values[i] ?? 0,
			color: colorForSymbol(label, labels)
		}))
	);
	const config: Chart.ChartConfig = $derived(
		Object.fromEntries(data.map((d) => [d.label, { label: d.label, color: d.color }]))
	);
</script>

{#if skeleton}
	<Skeleton class="aspect-square w-full" />
{:else}
	<Chart.Container {config} class="mx-auto aspect-square w-full">
		<PieChart
			{data}
			key="label"
			value="value"
			c="color"
			padAngle={0.01}
			cornerRadius={4}
			props={{
				pie: {
					motion: prefersReducedMotion.current ? "none" : { type: "tween", duration: 300 }
				},
				arc: { class: "cursor-pointer" }
			}}
			onArcClick={(_, detail) => {
				const label = detail.data?.label;
				if (label && label !== OTHER_SYMBOL) goto(resolve("/assets/[slug]", { slug: label }));
			}}
		>
			{#snippet tooltip()}
				<Chart.Tooltip hideLabel />
			{/snippet}
		</PieChart>
	</Chart.Container>
{/if}
