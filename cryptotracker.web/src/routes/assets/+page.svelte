<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import * as api from '$lib/cryptotrackerApi';
</script>

{#await api.getAssets()}
	<p>Loading...</p>
{:then assets}
	<div class="grid gap-4 sm:grid-cols-2 md:grid-cols-4 lg:grid-cols-6">
		{#each assets.data as asset}
			<a href="/assets/{asset.symbol}">
				<Card.Root>
					<Card.Header>
						<Card.Title class="text-center">{asset.name ? asset.name : asset.symbol}</Card.Title>
					</Card.Header>
					<Card.Content>
						<img
							class="w-max"
							src={asset.image
								? asset.image
								: 'https://png.pngtree.com/png-vector/20210604/ourmid/pngtree-gray-network-placeholder-png-image_3416659.jpg'}
							alt={asset.name}
						/>
						<!-- <p>{asset.symbol}</p> -->
					</Card.Content>
				</Card.Root>
			</a>
		{/each}
	</div>
{/await}
