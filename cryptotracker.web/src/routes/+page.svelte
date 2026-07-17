<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Skeleton } from '$lib/components/ui/skeleton';
	import * as api from '$lib/cryptotrackerApi';
	import { baseCurrency } from '$lib/stores/config';
	import { formatCurrency } from '$lib/format';
	import LineChart from '$lib/components/charts/LineChart.svelte';
	import PieChart from '$lib/components/charts/PieChart.svelte';
	import CardWithDays from '$lib/components/ui/card/card-with-days.svelte';

	let selectedRange = $state<number>(14);

	function UniqueSymbols(data: { [key: string]: api.AssetHoldingDto[] }) {
		return [
			...new Set(
				Object.values(data)
					.flat()
					.map((m) => m.asset.symbol ?? '')
			)
		];
	}

	function TrimMeasurings(data: api.AssetHoldingDto[]) {
		const sorted = [...data].sort((a, b) => (b.totalValue ?? 0) - (a.totalValue ?? 0));
		if (sorted.length <= 7) {
			return sorted;
		}

		const top = sorted.slice(0, 7);
		const otherValue = sorted.slice(7).reduce((acc, curr) => acc + (curr.totalValue ?? 0), 0);
		return top.concat({
			asset: { symbol: 'Other', assetType: 'Crypto' },
			totalValue: otherValue,
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
					<Skeleton class="h-6 w-1/2 bg-gray-200" />
				{:then standing}
					<div class="text-2xl font-bold">{formatCurrency(standing.data, $baseCurrency)}</div>
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
					{@const trimmed = TrimMeasurings(measuring.data)}
					<PieChart
						labels={trimmed.map((x) => x.asset.symbol ?? '')}
						values={trimmed.map((x) => x.totalValue ?? 0)}
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
					labels={Object.keys(standings.data)}
					datasets={[{ name: $baseCurrency, data: Object.values(standings.data) }]}
				/>
			{:catch error}
				<p>{error.message}</p>
			{/await}
		</CardWithDays>
		<CardWithDays class="col-span-4" title="Zusammensetzung" bind:selectedRange>
			{#await api.getMeasuringsByDays(selectedRange)}
				<LineChart skeleton={true} />
			{:then stats}
				<LineChart
					labels={Object.keys(stats.data)}
					datasets={UniqueSymbols(stats.data).map((assetId) => ({
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
