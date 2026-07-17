<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import * as api from '$lib/cryptotrackerApi';
	import { Skeleton } from '$lib/components/ui/skeleton/index.js';

	export let integrations: api.IntegrationDto[] = [];
	export let skeleton: boolean = false;
</script>

{#if skeleton}
	{#each { length: 6 } as _}
		<Card.Root class="flex h-full flex-col">
			<Card.Content class="flex flex-col items-center gap-3">
				<Skeleton class="bg-muted size-16 rounded-full" />
				<Skeleton class="bg-muted h-4 w-32" />
				<Skeleton class="bg-muted h-3 w-20" />
			</Card.Content>
		</Card.Root>
	{/each}
{:else}
	{#each integrations as integration}
		<a href="/integrations/{integration.id}" class="group">
			<Card.Root
				class="hover:border-primary/20 relative flex h-full flex-col transition-all duration-200 group-hover:-translate-y-0.5 hover:shadow-md"
			>
				<span
					class="absolute top-3 right-3 rounded-full px-2 py-0.5 text-xs font-medium {integration.isManual
						? 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-400'
						: 'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-400'}"
				>
					{integration.isManual ? 'Manuell' : 'Automatisch'}
				</span>
				<Card.Content class="flex flex-col items-center gap-3">
					<div
						class="flex size-16 items-center justify-center rounded-full text-xl font-bold {integration.isManual
							? 'bg-secondary text-secondary-foreground'
							: 'bg-primary/10 text-primary'}"
					>
						{integration.name ? integration.name.slice(0, 2).toUpperCase() : '??'}
					</div>
					<div class="text-center">
						<p class="leading-tight font-semibold">{integration.name}</p>
						{#if integration.description}
							<p class="text-muted-foreground text-xs">{integration.description}</p>
						{/if}
					</div>
				</Card.Content>
			</Card.Root>
		</a>
	{/each}
{/if}
