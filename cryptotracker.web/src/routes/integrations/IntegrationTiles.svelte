<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import * as api from '$lib/cryptotrackerApi';
	import { Skeleton } from '$lib/components/ui/skeleton/index.js';

	export let integrations: api.IntegrationDto[] = [];
	export let hidden: boolean = false;
	export let skeleton: boolean = false;
</script>

{#if skeleton}
	{#each { length: 8 } as _}
		<Card.Root class="flex h-full flex-col">
			<Card.Header>
				<Card.Title class="self-center">
					<Skeleton class="h-4 w-[150px] bg-gray-200" />
				</Card.Title>
			</Card.Header>
			<Card.Content class="flex-grow"></Card.Content>
		</Card.Root>
	{/each}
{:else}
	{#each integrations.filter((x) => x.isHidden == hidden) as integration}
		<a href="/integrations/{integration.id}">
			<Card.Root class="flex h-full flex-col {integration.isManual ? 'bg-gray-300' : ''}">
				<Card.Header>
					<Card.Title class="text-center">{integration.name}</Card.Title>
				</Card.Header>
				<Card.Content class="flex-grow"></Card.Content>
			</Card.Root>
		</a>
	{/each}
{/if}
