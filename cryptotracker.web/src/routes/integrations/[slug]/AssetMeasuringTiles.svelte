<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Skeleton } from '$lib/components/ui/skeleton';
	import * as api from '$lib/cryptotrackerApi';

	export let measurings: api.AssetHoldingDto[] = [];
	export let hidden: boolean = false;
	export let skeleton: boolean = false;
</script>

{#if skeleton}
	{#each { length: 6 } as _}
		<Card.Root class="flex h-full flex-col">
			<Card.Content class="flex items-center gap-3">
				<Skeleton class="bg-muted size-12 shrink-0 rounded-full" />
				<div class="min-w-0 flex-1">
					<Skeleton class="bg-muted mb-1 h-4 w-3/4" />
					<Skeleton class="bg-muted h-3 w-1/2" />
				</div>
			</Card.Content>
		</Card.Root>
	{/each}
{:else}
	{#each measurings.filter((x) => x.asset.isHidden == hidden) as measuring}
		<a href="/assets/{measuring.asset.symbol}" class="group">
			<Card.Root
				class="hover:border-primary/20 flex h-full flex-col transition-all duration-200 group-hover:-translate-y-0.5 hover:shadow-md"
			>
				<Card.Content class="flex items-center gap-3">
					{#if measuring.asset.image}
						<img
							class="size-12 shrink-0 rounded-full object-contain"
							src={measuring.asset.image}
							alt={measuring.asset.name}
						/>
					{:else}
						<div
							class="bg-muted text-muted-foreground flex size-12 shrink-0 items-center justify-center rounded-full text-sm font-bold"
						>
							{(measuring.asset.symbol ?? '?').slice(0, 2).toUpperCase()}
						</div>
					{/if}
					<div class="min-w-0 flex-1">
						<p class="truncate font-semibold">
							{measuring.asset.name ? measuring.asset.name : measuring.asset.symbol}
						</p>
						<p class="text-muted-foreground text-sm">
							{measuring.totalAmount?.toFixed(2)}
							{measuring.asset.symbol}
						</p>
					</div>
				</Card.Content>
			</Card.Root>
		</a>
	{/each}
{/if}
