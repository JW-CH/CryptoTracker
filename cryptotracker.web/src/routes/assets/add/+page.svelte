<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import * as api from '$lib/cryptotrackerApi';

	let symbol: string = $state('');
	let externalId: string = $state('');
	let isFiat: boolean = $state(false);

	let values: api.Coin[] | api.Fiat[] | null = $state(null);

	import { onMount } from 'svelte';

	onMount(async () => {
		values = await GetStuff(isFiat);
	});

	$effect(() => {
		GetStuff(isFiat).then((value) => (values = value));
	});

	async function GetStuff(isFiat: boolean) {
		if (isFiat) {
			let request = await api.getFiats();

			if (request.status != 200) {
				return [];
			}

			return request.data;
		} else {
			let request = await api.getCoins();

			if (request.status != 200) {
				return [];
			}

			return request.data;
		}
	}

	async function AddIntegration() {
		if (!symbol) return;
		if (isFiat) {
			externalId = (values as api.Fiat[]).find((x) => x.symbol === symbol)?.symbol || '';
		} else {
			externalId = (values as api.Coin[]).find((x) => x.symbol === symbol)?.id ?? '';
		}

		if (!externalId) return;

		let request = await api.addAsset({
			symbol: symbol,
			externalId: externalId,
			isFiat: isFiat
		});

		if (request.data) {
			window.location.href = '/assets/' + symbol;
		}
	}
</script>

<Card.Root class="col-span-4">
	<Card.Header>
		<Card.Title>Vermögenswert hinzufügen</Card.Title>
	</Card.Header>
	<Card.Content class="flex items-center">
		<div>
			Fiat
			<input
				bind:checked={isFiat}
				type="checkbox"
				class="rounded-lg border-2 border-solid border-gray-200 px-3 py-2 text-sm focus:border-blue-500 focus:ring-blue-500 disabled:pointer-events-none disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-400 dark:placeholder-neutral-500 dark:focus:ring-neutral-600"
			/>
		</div>
		<div>
			asset:
			<select
				class="rounded-lg border-2 border-solid border-gray-200 px-3 py-2 pe-9 text-sm focus:border-blue-500 focus:ring-blue-500 disabled:pointer-events-none disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-400 dark:placeholder-neutral-500 dark:focus:ring-neutral-600"
				bind:value={symbol}
			>
				{#key values}
					{#each values ?? [] as item}
						<option value={item.symbol}>{item.name}</option>
					{/each}
				{/key}
			</select>
		</div>
		<Button on:click={AddIntegration}>Speichern</Button>
	</Card.Content>
</Card.Root>
