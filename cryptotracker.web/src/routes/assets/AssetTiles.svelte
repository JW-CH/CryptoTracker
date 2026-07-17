<script lang="ts">
	import { resolve } from "$app/paths";
	import * as Card from "$lib/components/ui/card";
	import { Skeleton } from "$lib/components/ui/skeleton";
	import * as api from "$lib/cryptotrackerApi";
	import { baseCurrency } from "$lib/stores/config";
	import { formatAmount, formatCurrency } from "$lib/format";

	let {
		assets = [],
		holdings = {},
		hidden = false,
		skeleton = false
	}: {
		assets?: api.AssetDto[];
		/** Latest holding per symbol — value/amount shown on the tile when present */
		holdings?: Record<string, api.AssetHoldingDto>;
		hidden?: boolean;
		skeleton?: boolean;
	} = $props();
</script>

{#if skeleton}
	{#each { length: 10 }, i (i)}
		<Card.Root class="flex h-full flex-col">
			<Card.Content class="flex flex-col items-center gap-3">
				<Skeleton class="bg-muted size-16 rounded-full" />
				<Skeleton class="bg-muted h-4 w-24" />
				<Skeleton class="bg-muted h-3 w-12" />
			</Card.Content>
		</Card.Root>
	{/each}
{:else}
	{#each assets.filter((x) => x.isHidden == hidden) as asset (asset.symbol)}
		{@const holding = holdings[asset.symbol ?? ""]}
		<a href={resolve("/assets/[slug]", { slug: asset.symbol ?? "" })} class="group">
			<Card.Root
				class="hover:border-primary/20 flex h-full flex-col transition-all duration-200 group-hover:-translate-y-0.5 hover:shadow-md"
			>
				<Card.Content class="flex flex-col items-center gap-3">
					{#if asset.image}
						<img class="size-16 rounded-full object-contain" src={asset.image} alt={asset.name} />
					{:else}
						<div
							class="bg-muted text-muted-foreground flex size-16 items-center justify-center rounded-full text-xl font-bold"
						>
							{(asset.symbol ?? "?").slice(0, 2).toUpperCase()}
						</div>
					{/if}
					<div class="text-center">
						<p class="leading-tight font-semibold">
							{asset.name ? asset.name : asset.symbol}
						</p>
						{#if asset.name}
							<p class="text-muted-foreground text-xs">{asset.symbol}</p>
						{/if}
						{#if holding}
							<p class="mt-2 text-sm font-medium tabular-nums">
								{formatCurrency(holding.totalValue ?? 0, $baseCurrency)}
							</p>
							<p class="text-muted-foreground text-xs tabular-nums">
								{formatAmount(holding.totalAmount ?? 0, asset.assetType)}
								{asset.symbol}
							</p>
						{/if}
					</div>
				</Card.Content>
			</Card.Root>
		</a>
	{/each}
{/if}
