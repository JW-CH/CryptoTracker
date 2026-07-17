<script lang="ts">
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";
	import * as Card from "$lib/components/ui/card";
	import { Button } from "$lib/components/ui/button";
	import { Input } from "$lib/components/ui/input";
	import { Label } from "$lib/components/ui/label";
	import * as Select from "$lib/components/ui/select";
	import SearchCombobox from "$lib/components/search-combobox.svelte";
	import * as api from "$lib/cryptotrackerApi";
	import { mutate } from "$lib/api/mutate";
	import Loader2Icon from "@lucide/svelte/icons/loader-2";

	// Commodity/RealEstate exist in the enum but have no price source yet;
	// ETF was offered before but could never be saved — so it is not listed.
	const assetTypes: api.AssetType[] = ["Fiat", "Crypto", "Stock"];
	const hasProviderList = (type: api.AssetType) => type === "Fiat" || type === "Crypto";

	let assetType = $state<api.AssetType>("Fiat");
	// Provider lists are selected by externalId — symbols are NOT unique on
	// CoinGecko, and duplicate keys break the rendered list. Stock uses the
	// ticker typed into the input.
	let selectedId = $state("");
	let symbol = $state("");
	let providerAssets = $state<api.ProviderAsset[] | null>(null);
	let saving = $state(false);
	let validationError = $state<string | null>(null);

	// Single fetch trigger with race guard (replaces the onMount + $effect double fetch)
	$effect(() => {
		const type = assetType;
		let cancelled = false;
		providerAssets = null;
		if (hasProviderList(type)) {
			loadProviderAssets(type).then((result) => {
				if (!cancelled) providerAssets = result;
			});
		}
		return () => {
			cancelled = true;
		};
	});

	async function loadProviderAssets(type: api.AssetType): Promise<api.ProviderAsset[]> {
		try {
			const request = type === "Fiat" ? await api.getFiats() : await api.getCoins();
			return request.status === 200 ? request.data : [];
		} catch {
			return [];
		}
	}

	const options = $derived.by(() => {
		const seen: Record<string, true> = {};
		const result: { value: string; label: string }[] = [];
		for (const a of providerAssets ?? []) {
			const value = a.externalId ?? a.symbol ?? "";
			if (!value || seen[value]) continue;
			seen[value] = true;
			result.push({
				value,
				label: a.name ? `${a.name} (${(a.symbol ?? "").toUpperCase()})` : (a.symbol ?? "")
			});
		}
		return result;
	});

	async function save() {
		validationError = null;
		let payloadSymbol = "";
		let externalId = "";
		if (hasProviderList(assetType)) {
			const found = providerAssets?.find((x) => (x.externalId ?? x.symbol) === selectedId);
			payloadSymbol = found?.symbol ?? "";
			externalId = found?.externalId ?? "";
		} else {
			payloadSymbol = symbol;
			externalId = symbol;
		}
		if (!payloadSymbol || !externalId) {
			validationError = "Please choose an asset first.";
			return;
		}
		saving = true;
		await mutate(() => api.addAsset({ symbol: payloadSymbol, externalId, assetType }), {
			success: `${payloadSymbol.toUpperCase()} added.`,
			onSuccess: () => goto(resolve("/assets/[slug]", { slug: payloadSymbol }))
		});
		saving = false;
	}
</script>

<svelte:head>
	<title>Add asset · CryptoTracker</title>
</svelte:head>

<Card.Root class="max-w-lg">
	<Card.Header>
		<Card.Title>Add asset</Card.Title>
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
				<Label for="asset-type">Type</Label>
				<Select.Root
					type="single"
					value={assetType}
					onValueChange={(v) => {
						assetType = v as api.AssetType;
						symbol = "";
						selectedId = "";
					}}
				>
					<Select.Trigger id="asset-type" class="w-full">{assetType}</Select.Trigger>
					<Select.Content>
						{#each assetTypes as type (type)}
							<Select.Item value={type} label={type} />
						{/each}
					</Select.Content>
				</Select.Root>
			</div>
			<div class="space-y-2">
				<Label for="asset">Asset</Label>
				{#if hasProviderList(assetType)}
					<SearchCombobox
						id="asset"
						items={options}
						bind:value={selectedId}
						disabled={providerAssets === null}
						placeholder={providerAssets === null ? "Loading…" : "Select asset"}
						searchPlaceholder="Search by name or symbol…"
					/>
				{:else}
					<Input id="asset" bind:value={symbol} placeholder="Ticker symbol, e.g. AAPL" />
				{/if}
				{#if validationError}
					<p class="text-destructive text-sm">{validationError}</p>
				{/if}
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
