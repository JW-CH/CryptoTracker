<script lang="ts">
	import { page } from '$app/state';
	import * as Card from '$lib/components/ui/card';
	import * as api from '$lib/cryptotrackerApi';
	import LineChart from '../../../components/charts/LineChart.svelte';

	function StringKeysToDates(arr: string[]) {
		return arr.map((x) =>
			new Date(x).toLocaleDateString('de-CH', { day: '2-digit', month: '2-digit', year: 'numeric' })
		);
	}
</script>

{#await api.getAsset(page.params.slug)}
	<p>Loading...</p>
{:then asset}
	<div class="space-y-4">
		<div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
			<Card.Root>
				<Card.Header class="flex flex-row items-center justify-between space-y-0 pb-2">
					<Card.Title class="text-center text-sm font-medium">Name</Card.Title>
				</Card.Header>
				<Card.Content>
					{asset.data.asset.name}
				</Card.Content>
			</Card.Root>
			<Card.Root>
				<Card.Header class="flex flex-row items-center justify-between space-y-0 pb-2">
					<Card.Title class="text-center text-sm font-medium">Symbol</Card.Title>
				</Card.Header>
				<Card.Content>
					{asset.data.asset.symbol}
				</Card.Content>
			</Card.Root>
			<Card.Root>
				<Card.Header class="flex flex-row items-center justify-between space-y-0 pb-2">
					<Card.Title class="text-center text-sm font-medium">Aktueller Preis</Card.Title>
				</Card.Header>
				<Card.Content>{asset.data.price} CHF</Card.Content>
			</Card.Root>
			<Card.Root>
				<Card.Header class="flex flex-row items-center justify-between space-y-0 pb-2">
					<Card.Title class="text-center text-sm font-medium">Logo</Card.Title>
				</Card.Header>
				<Card.Content>
					<img class="h-10" src={asset.data.asset?.image} alt={asset.data.asset.name} />
				</Card.Content>
			</Card.Root>
		</div>

		<div class="grid gap-4 md:grid-cols-4 lg:grid-cols-8">
			{#await api.getMeasuringsByDay(7, { $symbol: asset.data.asset.symbol ?? '' })}
				<p>Loading...</p>
			{:then measurings}
				<Card.Root class="col-span-4">
					<Card.Header>
						<Card.Title>Bestand letzte 7 Tage</Card.Title>
					</Card.Header>
					<Card.Content>
						<LineChart
							fill={true}
							labels={StringKeysToDates(Object.keys(measurings.data))}
							datasets={[
								{
									name: asset.data.asset.symbol ?? '',
									data: Object.values(measurings.data).map(
										(x) => x.find((y) => y.assetId === asset.data.asset.symbol)?.assetAmount ?? 0
									)
								}
							]}
						/>
					</Card.Content>
				</Card.Root>
				<Card.Root class="col-span-4">
					<Card.Header>
						<Card.Title>Wert Bestand letzte 7 Tage</Card.Title>
					</Card.Header>
					<Card.Content>
						<LineChart
							fill={true}
							labels={StringKeysToDates(Object.keys(measurings.data))}
							datasets={[
								{
									name: asset.data.asset.symbol ?? '',
									data: Object.values(measurings.data).map(
										(x) => x.find((y) => y.assetId === asset.data.asset.symbol)?.fiatValue ?? 0
									)
								}
							]}
						/>
					</Card.Content>
				</Card.Root>
			{:catch error}
				<p>{error.message}</p>
			{/await}
		</div>
	</div>
{/await}
