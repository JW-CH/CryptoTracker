<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import { onMount } from 'svelte';
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import SearchCombobox from '$lib/components/search-combobox.svelte';
	import * as api from '$lib/cryptotrackerApi';
	import { mutate } from '$lib/api/mutate';
	import Loader2Icon from '@lucide/svelte/icons/loader-2';

	let assets = $state<api.AssetDto[] | null>(null);
	let selectedAsset = $state('');
	let amount = $state(0);
	let date = $state(new Date().toISOString().split('T')[0]);
	let saving = $state(false);
	let validationError = $state<string | null>(null);

	onMount(async () => {
		try {
			const request = await api.getAssets();
			assets = request.status === 200 ? request.data : [];
		} catch {
			assets = [];
		}
	});

	const options = $derived(
		(assets ?? []).map((a) => ({
			value: a.symbol ?? '',
			label: a.name ? `${a.name} (${(a.symbol ?? '').toUpperCase()})` : (a.symbol ?? '')
		}))
	);

	async function save() {
		validationError = null;
		if (!date || !selectedAsset) {
			validationError = 'Please pick a date and an asset.';
			return;
		}
		const slug = page.params.slug ?? '';
		saving = true;
		await mutate(() => api.addIntegrationMeasuring(slug, { symbol: selectedAsset, date, amount }), {
			success: `Measurement for ${selectedAsset.toUpperCase()} saved.`,
			onSuccess: () => goto(resolve('/integrations/[slug]', { slug }))
		});
		saving = false;
	}
</script>

<svelte:head>
	<title>Add measurement · CryptoTracker</title>
</svelte:head>

<Card.Root class="max-w-lg">
	<Card.Header>
		<Card.Title>Add measurement</Card.Title>
	</Card.Header>
	<Card.Content>
		<form
			class="space-y-4"
			onsubmit={async (event) => {
				event.preventDefault();
				await save();
			}}
		>
			<div class="space-y-2">
				<Label for="date">Date</Label>
				<Input id="date" type="date" bind:value={date} />
			</div>
			<div class="space-y-2">
				<Label for="asset">Asset</Label>
				<SearchCombobox
					id="asset"
					items={options}
					bind:value={selectedAsset}
					disabled={assets === null}
					placeholder={assets === null ? 'Loading…' : 'Select asset'}
					searchPlaceholder="Search by name or symbol…"
				/>
				{#if validationError}
					<p class="text-destructive text-sm">{validationError}</p>
				{/if}
			</div>
			<div class="space-y-2">
				<Label for="amount">Amount</Label>
				<Input id="amount" type="number" step="any" bind:value={amount} />
			</div>
			<Button type="submit" disabled={saving}>
				{#if saving}
					<Loader2Icon class="size-4 animate-spin" />
				{/if}
				Save
			</Button>
		</form>
	</Card.Content>
</Card.Root>
