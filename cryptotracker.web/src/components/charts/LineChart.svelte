<script lang="ts">
	import { generateGUID } from '$lib/helpers';
	import { Chart } from 'chart.js/auto';
	import { onMount } from 'svelte';

	// export let values: number[][];
	export let datasets: { name: string; data: number[] }[];
	export let labels: string[];
	export let fill: boolean = false;

	let id = 'chart_' + generateGUID();

	onMount(() => {
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

<canvas {id}></canvas>
