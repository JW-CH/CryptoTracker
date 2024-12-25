<script lang="ts">
	import { generateGUID } from '$lib/helpers';
	import { Chart } from 'chart.js/auto';
	import { onMount } from 'svelte';

	export let values: number[] = [300, 50, 100];
	export let labels: string[] = ['Red', 'Blue', 'Yellow'];

	let id = 'chart_' + generateGUID();

	onMount(() => {
		let ctx: any = document.getElementById(id);
		let myChart = new Chart(ctx, {
			type: 'pie',
			data: data
		});
	});

	let data = {
		labels: labels,
		datasets: [
			{
				label: 'My First Dataset',
				data: values,
				hoverOffset: 8
			}
		]
	};
</script>

<canvas {id}></canvas>
