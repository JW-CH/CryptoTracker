<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import * as api from '$lib/cryptotrackerApi';

	export let assets: api.Asset[] = [];
	export let hidden: boolean = false;
</script>

{#each assets.filter((x) => x.isHidden == hidden) as asset}
	<a href="/assets/{asset.symbol}">
		<Card.Root class="flex h-full flex-col">
			<Card.Header>
				<Card.Title class="text-center">{asset.name ? asset.name : asset.symbol}</Card.Title>
			</Card.Header>
			<Card.Content class="flex-grow">
				<img
					class="w-max"
					src={asset.image
						? asset.image
						: 'https://png.pngtree.com/png-vector/20210604/ourmid/pngtree-gray-network-placeholder-png-image_3416659.jpg'}
					alt={asset.name}
				/>
			</Card.Content>
		</Card.Root>
	</a>
{/each}
