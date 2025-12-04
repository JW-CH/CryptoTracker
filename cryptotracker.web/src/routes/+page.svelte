<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Skeleton } from '$lib/components/ui/skeleton';
	import * as api from '$lib/cryptotrackerApi';
	import LineChart from '$lib/components/charts/LineChart.svelte';
	import PieChart from '$lib/components/charts/PieChart.svelte';
	import CardWithDays from '$lib/components/ui/card/card-with-days.svelte';

	let assets: Set<string> = new Set<string>();
	let summarize: boolean = true;
	let selectedRange = $state<number>(14);

	function AddAsset(assetId: string) {
		assets.add(assetId);
	}

	function StringKeysToDates(arr: string[]) {
		return arr.map((x) =>
			new Date(x).toLocaleDateString('de-CH', { day: '2-digit', month: '2-digit', year: 'numeric' })
		);
	}

	function TrimMeasurings(data: api.MessungDto[]) {
		let sortedMeasuring = data.sort((a, b) => (b.totalValue ?? 0) - (a.totalValue ?? 0));
		if (!summarize || sortedMeasuring.length <= 7) {
			return sortedMeasuring;
		}

		let topMeasuring = sortedMeasuring.slice(0, 7);
		let otherMeasuring = sortedMeasuring.slice(7);
		let otherFiatValue = otherMeasuring.reduce((acc, curr) => acc + (curr.totalValue ?? 0), 0);
		return topMeasuring.concat({
			asset: { symbol: 'Other' },
			totalValue: otherFiatValue,
			price: 0,
			totalAmount: 0,
			integrationValues: []
		});
	}
</script>

<div class="space-y-4">
	<div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
		<Card.Root>
			<Card.Header class="flex flex-row items-center justify-between space-y-0 pb-2">
				<Card.Title class="text-sm font-medium">Aktueller Wert</Card.Title>
			</Card.Header>
			<Card.Content>
				{#await api.getLatestStanding()}
					<Skeleton class="h-6 w-1/2" />
				{:then standing}
					<div class="text-2xl font-bold">{standing.data.toFixed(2)} CHF</div>
				{:catch error}
					<p>{error.message}</p>
				{/await}
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
					<PieChart skeleton={true} />
				{:then measuring}
					<PieChart
						labels={TrimMeasurings(measuring.data).map((x) => x.asset.symbol ?? '')}
						values={TrimMeasurings(measuring.data).map((x) => x.totalValue ?? 0)}
					/>
				{:catch error}
					<p>{error.message}</p>
				{/await}
			</Card.Content>
		</Card.Root>
		<CardWithDays class="col-span-4" title="Wert" bind:selectedRange>
			{#await api.getStandingsByDay(selectedRange)}
				<LineChart skeleton={true} />
			{:then standings}
				<LineChart
					fill={true}
					labels={StringKeysToDates(Object.keys(standings.data))}
					datasets={[{ name: 'CHF', data: Object.values(standings.data) }]}
				/>
			{:catch error}
				<p>{error.message}</p>
			{/await}
		</CardWithDays>
		<CardWithDays class="col-span-4" title="Zusammensetzung" bind:selectedRange>
			{#await api.getMeasuringsByDays(selectedRange)}
				<LineChart skeleton={true} />
			{:then stats}
				{#each Object.values(stats.data).flat() as stat}
					{AddAsset(stat.asset.symbol ?? '')}
				{/each}
				<LineChart
					labels={StringKeysToDates(Object.keys(stats.data))}
					datasets={Array.from(assets).map((assetId) => ({
						name: assetId,
						data: Object.values(stats.data).map(
							(x) => x.find((y) => y.asset.symbol === assetId)?.totalValue ?? 0
						)
					}))}
				/>
			{:catch error}
				<p>{error.message}</p>
			{/await}
		</CardWithDays>
	</div>
</div>
