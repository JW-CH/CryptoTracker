<script lang="ts">
	import Skeleton from '$lib/components/ui/skeleton/skeleton.svelte';
	import { generateGUID } from '$lib/helpers';
	import { Chart } from 'chart.js/auto';
	import { onMount } from 'svelte';

	// export let values: number[][];
	export let datasets: { name: string; data: number[] }[] = [];
	export let labels: string[] = [];
	export let fill: boolean = false;
	export let skeleton: boolean = false;

	let id = 'chart_' + generateGUID();

	onMount(() => {
		if (skeleton) return;

		let ctx: any = document.getElementById(id);

		let myChart = new Chart(ctx, {
			type: 'line',
			data: data
		});
	});

	let data = {
		labels: labels,
		datasets: datasets.map((value, index) => ({
			label: value.name,
			data: value.data,
			fill: fill
		}))
	};
</script>

{#if skeleton}
	<Skeleton class="aspect-video w-full bg-gray-200"></Skeleton>
{:else}
	<canvas {id}></canvas>
{/if}
