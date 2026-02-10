<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Skeleton } from '$lib/components/ui/skeleton';
	import * as api from '$lib/cryptotrackerApi';

	export let assets: api.Asset[] = [];
	export let hidden: boolean = false;
	export let skeleton: boolean = false;
</script>

{#if skeleton}
	{#each { length: 10 } as _}
		<Card.Root class="flex h-full flex-col">
			<Card.Content class="flex flex-col items-center gap-3">
				<Skeleton class="size-16 rounded-full bg-muted" />
				<Skeleton class="h-4 w-24 bg-muted" />
				<Skeleton class="h-3 w-12 bg-muted" />
			</Card.Content>
		</Card.Root>
	{/each}
{:else}
	{#each assets.filter((x) => x.isHidden == hidden) as asset}
		<a href="/assets/{asset.symbol}" class="group">
			<Card.Root
				class="flex h-full flex-col transition-all duration-200 hover:shadow-md hover:border-primary/20 group-hover:-translate-y-0.5"
			>
				<Card.Content class="flex flex-col items-center gap-3">
					{#if asset.image}
						<img
							class="size-16 rounded-full object-contain"
							src={asset.image}
							alt={asset.name}
						/>
					{:else}
						<div
							class="flex size-16 items-center justify-center rounded-full bg-muted text-xl font-bold text-muted-foreground"
						>
							{(asset.symbol ?? '?').slice(0, 2).toUpperCase()}
						</div>
					{/if}
					<div class="text-center">
						<p class="font-semibold leading-tight">
							{asset.name ? asset.name : asset.symbol}
						</p>
						{#if asset.name}
							<p class="text-xs text-muted-foreground">{asset.symbol}</p>
						{/if}
					</div>
				</Card.Content>
			</Card.Root>
		</a>
	{/each}
{/if}
