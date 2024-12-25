<script lang="ts">
	import * as api from '$lib/cryptotrackerApi';
	import PieChart from '../../components/charts/PieChart.svelte';
</script>

{#await api.getLatestMeasurings()}
	<p>Loading...</p>
{:then measuring}
	<div class="h-1/2 w-1/2">
		<PieChart
			labels={measuring.data.map((x) => x.assetId ?? '')}
			values={measuring.data.map((x) => x.standingValue ?? 0)}
		/>
	</div>
{/await}

<!-- {#await api.getMeasuringsByDay(7)}
	<p>Loading...</p>
{:then stats}
	{console.log(stats.data)}
	{#each Object.keys(stats.data) as key}
		<h1 class="font-bold">{key}</h1>
		<ul>
			{#each stats.data[key] as stat}
				<li>{stat.assetId}: {stat.standingValue}</li>
			{/each}
		</ul>
	{/each}
{:catch error}
	<p>{error.message}</p>
{/await} -->
