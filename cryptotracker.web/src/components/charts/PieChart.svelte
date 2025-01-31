<script lang="ts">
	import { Skeleton } from '$lib/components/ui/skeleton';
	import { generateGUID } from '$lib/helpers';
	import { goto } from '$app/navigation';
	import { Chart } from 'chart.js/auto';
	import { onMount } from 'svelte';

	export let values: number[] = [];
	export let labels: string[] = [];
	export let skeleton: boolean = false;

	let id = 'chart_' + generateGUID();

	onMount(() => {
		if (skeleton) return;

		let ctx: any = document.getElementById(id);
		let myChart = new Chart(ctx, {
			type: 'pie',
			data: data,
			options: {
				onClick: (event, elements) => {
					if (elements.length > 0) {
						let index = elements[0].index;
						const label = labels[index];
						if (label == 'Other') return;
						goto(`/assets/${label}`);
					}
				}
			}
		});
	});

	let data = {
		labels: labels,
		datasets: [
			{
				label: 'CHF',
				data: values,
				hoverOffset: 8
			}
		]
	};
</script>

{#if skeleton}
	<Skeleton class="aspect-square w-full bg-gray-200"></Skeleton>
{:else}
	<canvas {id}></canvas>
{/if}
