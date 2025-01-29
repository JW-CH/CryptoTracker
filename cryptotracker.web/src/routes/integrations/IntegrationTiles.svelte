<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import * as api from '$lib/cryptotrackerApi';
	import { Skeleton } from '$lib/components/ui/skeleton/index.js';

	export let integrations: api.IntegrationDto[] = [];
	export let hidden: boolean = false;
	export let skeleton: boolean = false;
</script>

{#if skeleton}
	{#each { length: 2 } as _}
		<div class="flex flex-col space-y-3">
			<Skeleton class="h-[125px] rounded-xl bg-gray-200" />
			<div class="space-y-2">
				<Skeleton class="h-4 w-[250px] bg-gray-200" />
				<Skeleton class="h-4 w-[200px] bg-gray-200" />
			</div>
		</div>
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
