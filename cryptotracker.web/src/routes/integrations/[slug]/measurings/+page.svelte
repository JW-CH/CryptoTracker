<script lang="ts">
	import { onMount } from 'svelte';
	import * as api from '$lib/cryptotrackerApi';
	import { page } from '$app/state';
	import Button from '$lib/components/ui/button/button.svelte';

	async function deleteMeasuring(measuring: api.DailyHoldingDto) {
		let x = await api.deleteIntegrationMeasuring(
			measuring.integrationId,
			measuring.symbol ?? '',
			measuring.date ?? ''
		);

		if (x.data) {
			measurings = measurings.filter(
				(m) => !(m.symbol === measuring.symbol && m.date === measuring.date)
			);
		}
	}

	onMount(async () => {
		api.getMeasuringsByIntegration(page.params.slug ?? '').then((response) => {
			measurings = response.data;
		});
	});

	let measurings: api.DailyHoldingDto[] = [];
</script>

Messungen für {page.params.slug}
{#each measurings.sort((a, b) => ((a.date ?? '') > (b.date ?? '') ? 1 : -1)) as measuring}
	<div class="grid-tem grid grid-cols-2 gap-4 p-2">
		<div class="items-center">
			{measuring.date} - {measuring.symbol}: {measuring.amount}
		</div>
		<Button onclick={() => deleteMeasuring(measuring)} variant="destructive" class="w-min">X</Button
		>
	</div>
{/each}
