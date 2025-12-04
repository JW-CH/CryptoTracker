<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Skeleton } from '$lib/components/ui/skeleton';
	import * as api from '$lib/cryptotrackerApi';

	export let measurings: api.MessungDto[] = [];
	export let hidden: boolean = false;
	export let skeleton: boolean = false;
</script>

{#if skeleton}
	{#each { length: 6 * 4 } as _}
		<Card.Root class="flex h-full flex-col">
			<Card.Content class="grow">
				<div class="grid grid-cols-5 items-center gap-4">
					<Skeleton class="h-10 w-full" />
					<Skeleton class="col-span-2 h-4 w-full" />
					<Skeleton class="col-span-2 h-4 w-full" />
				</div>
			</Card.Content>
		</Card.Root>
	{/each}
{:else}
	{#each measurings.filter((x) => x.asset.isHidden == hidden) as measuring}
		<a href="/assets/{measuring.asset.symbol}">
			<Card.Root class="flex h-full flex-col">
				<Card.Content class="grow">
					<div class="grid grid-cols-6 items-center gap-4">
						<img
							class="w-3/4"
							src={measuring.asset.image
								? measuring.asset.image
								: 'https://png.pngtree.com/png-vector/20210604/ourmid/pngtree-gray-network-placeholder-png-image_3416659.jpg'}
							alt={measuring.asset.name}
						/>
						<p class="col-span-3 text-center">
							{measuring.asset.name ? measuring.asset.name : measuring.asset.symbol}
						</p>
						<div class="col-span-2">
							<p>{measuring.totalAmount?.toFixed(2)} {measuring.asset.symbol}</p>
							<!-- <p>{measuring.totalValue?.toFixed(2)} CHF</p> -->
						</div>
					</div>
				</Card.Content>
			</Card.Root>
		</a>
	{/each}
{/if}
