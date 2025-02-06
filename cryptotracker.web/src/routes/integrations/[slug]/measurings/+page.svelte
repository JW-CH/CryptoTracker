<script lang="ts">
	import { onMount } from 'svelte';
	import * as api from '$lib/cryptotrackerApi';
	import { page } from '$app/state';
	import Button from '$lib/components/ui/button/button.svelte';

	async function deleteMeasuring(measuringId: string) {
		let x = await api.deleteMeasuringById(measuringId);

		if (x.data) {
			measurings = measurings.filter((x) => x.id !== measuringId);
		}
	}

	onMount(async () => {
		api.getMeasuringsByIntegration(page.params.slug).then((response) => {
			measurings = response.data;
		});
	});

	let measurings: api.AssetMeasuringDto[] = [];
</script>

Messungen für {page.params.slug}
{#each measurings.sort((a, b) => ((a.timestamp ?? '') > (b.timestamp ?? '') ? 1 : -1)) as measuring}
	<div class="grid-tem grid grid-cols-2 gap-4 p-2">
		<div class="items-center">
			{measuring.timestamp} - {measuring.symbol}: {measuring.amount}
		</div>
		<Button on:click={() => deleteMeasuring(measuring.id!)} variant="destructive" class="w-min"
			>X</Button
		>
	</div>
{/each}
