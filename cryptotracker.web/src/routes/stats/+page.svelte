<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import * as api from '$lib/cryptotrackerApi';
	import LineChart from '../../components/charts/LineChart.svelte';
	import PieChart from '../../components/charts/PieChart.svelte';

	let assets: Set<string> = new Set<string>();
	let summarize: boolean = true;

	function AddAsset(assetId: string) {
		assets.add(assetId);
	}

	function StringKeysToDates(arr: string[]) {
		return arr.map((x) =>
			new Date(x).toLocaleDateString('de-CH', { day: '2-digit', month: '2-digit', year: 'numeric' })
		);
	}

	function TrimMeasurings(data: api.AssetMeasuringDto[]) {
		if (!summarize || data.length <= 7) {
			return data;
		}

		let sortedMeasuring = data.sort((a, b) => (b.fiatValue ?? 0) - (a.fiatValue ?? 0));
		let topMeasuring = sortedMeasuring.slice(0, 7);
		let otherMeasuring = sortedMeasuring.slice(7);
		let otherFiatValue = otherMeasuring.reduce((acc, curr) => acc + (curr.fiatValue ?? 0), 0);

		return topMeasuring.concat({ assetId: 'Other', fiatValue: otherFiatValue });
	}
</script>

<div class="space-y-4">
	<div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
		<Card.Root>
			<Card.Header class="flex flex-row items-center justify-between space-y-0 pb-2">
				<Card.Title class="text-sm font-medium">Aktueller Wert</Card.Title>
				<!-- <CreditCard class="text-muted-foreground h-4 w-4" /> -->
			</Card.Header>
			<Card.Content>
				{#await api.getLatestStanding()}
					<p>Loading...</p>
				{:then standing}
					<div class="text-2xl font-bold">{standing.data.toFixed(2)} CHF</div>
				{/await}
				<!-- <p class="text-muted-foreground text-xs">+19% from last month</p> -->
			</Card.Content>
		</Card.Root>
	</div>

	<div class="grid gap-4 md:grid-cols-4 lg:grid-cols-8">
		<Card.Root class="col-span-4">
			<Card.Header>
				<Card.Title>Aktuelle Zusammensetzung</Card.Title>
			</Card.Header>
			<Card.Content>
				{#await api.getLatestMeasurings()}
					<p>Loading...</p>
				{:then measuring}
					<PieChart
						labels={TrimMeasurings(measuring.data).map((x) => x.assetId ?? '')}
						values={TrimMeasurings(measuring.data).map((x) => x.fiatValue ?? 0)}
					/>
				{/await}
			</Card.Content>
		</Card.Root>
		<Card.Root class="col-span-4">
			<Card.Header>
				<Card.Title>Wert letzte 7 Tage</Card.Title>
			</Card.Header>
			<Card.Content>
				{#await api.getStandingsByDay(7)}
					<p>Loading...</p>
				{:then standings}
					<LineChart
						fill={true}
						labels={StringKeysToDates(Object.keys(standings.data))}
						datasets={[{ name: 'CHF', data: Object.values(standings.data) }]}
					/>
				{:catch error}
					<p>{error.message}</p>
				{/await}
			</Card.Content>
		</Card.Root>
		<Card.Root class="col-span-4">
			<Card.Header>
				<Card.Title>Zusammensetzung letzte 7 Tage</Card.Title>
			</Card.Header>
			<Card.Content>
				{#await api.getMeasuringsByDay(7)}
					<p>Loading...</p>
				{:then stats}
					{#each Object.values(stats.data).flat() as stat}
						{AddAsset(stat.assetId ?? '')}
					{/each}
					<LineChart
						labels={StringKeysToDates(Object.keys(stats.data))}
						datasets={Array.from(assets).map((assetId) => ({
							name: assetId,
							data: Object.values(stats.data).map(
								(x) => x.find((y) => y.assetId === assetId)?.fiatValue ?? 0
							)
						}))}
					/>
				{:catch error}
					<p>{error.message}</p>
				{/await}
			</Card.Content>
		</Card.Root>
	</div>
</div>
