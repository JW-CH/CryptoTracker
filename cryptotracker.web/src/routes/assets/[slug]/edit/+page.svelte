<script lang="ts">
	import { goto, invalidateAll } from "$app/navigation";
	import { resolve } from "$app/paths";
	import { mutate } from "$lib/api/mutate";
	import { untrack } from "svelte";
	import * as api from "$lib/cryptotrackerApi";
	import * as Card from "$lib/components/ui/card";
	import Button from "$lib/components/ui/button/button.svelte";

	// provided by [slug]/+layout.ts – reused without an extra fetch
	let { data } = $props();

	const asset = untrack(() => data.asset.asset);
	const symbol = asset.symbol!;

	let assetName = $state<string>(asset.name ?? "");
	let assetImage = $state<string>(asset.image ?? "");
	let saving = $state<boolean>(false);

	let imageError = $state<boolean>(false);
	$effect(() => {
		// reading assetImage makes this rerun whenever the URL changes
		if (typeof assetImage === "string") imageError = false;
	});

	const inputClass =
		"w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:border-ring focus:ring-1 focus:ring-ring focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50";

	async function Save() {
		saving = true;

		try {
			await mutate(
				() =>
					api.updateAssetMetadata(symbol, {
						name: assetName,
						image: imageError ? "" : assetImage
					}),
				{
					success: "Changes saved.",
					onSuccess: async () => {
						await invalidateAll();
						await goto(resolve("/assets/[slug]", { slug: symbol }));
					}
				}
			);
		} finally {
			saving = false;
		}
	}

	function Cancel() {
		goto(resolve("/assets/[slug]", { slug: symbol }));
	}
</script>

<div class="space-y-6">
	<!-- Form -->
	<Card.Root>
		<Card.Header>
			<Card.Title>Metadaten</Card.Title>
			<Card.Description>Name und Bild dieses Assets anpassen.</Card.Description>
		</Card.Header>
		<Card.Content class="space-y-5">
			<!-- Name -->
			<div class="space-y-1.5">
				<label for="asset-name" class="text-sm font-medium">Name</label>
				<input
					id="asset-name"
					type="text"
					class={inputClass}
					bind:value={assetName}
					placeholder="z.B. Ethereum"
					disabled={saving}
				/>
			</div>

			<!-- Image -->
			<div class="space-y-1.5">
				<label for="asset-image" class="text-sm font-medium">Bild-URL</label>
				<div class="flex items-center gap-3">
					{#if assetImage && !imageError}
						<img
							class="border-border size-10 shrink-0 rounded-full border object-contain"
							src={assetImage}
							alt="Vorschau"
							onerror={() => (imageError = true)}
						/>
					{:else}
						<div
							class="bg-muted text-muted-foreground flex size-10 shrink-0 items-center justify-center rounded-full text-sm font-bold"
						>
							{symbol.slice(0, 2).toUpperCase()}
						</div>
					{/if}
					<input
						id="asset-image"
						type="url"
						class={inputClass}
						bind:value={assetImage}
						placeholder="https://…"
						disabled={saving}
					/>
				</div>
				<p class="text-muted-foreground text-xs">Direkter Link zu einem Bild (PNG, SVG, …).</p>
			</div>
		</Card.Content>
	</Card.Root>

	<!-- Actions -->
	<div class="flex gap-2">
		<Button size="sm" onclick={Save} disabled={saving}>Speichern</Button>
		<Button variant="outline" size="sm" onclick={Cancel} disabled={saving}>Abbrechen</Button>
	</div>
</div>
