<script lang="ts">
	import { resolve } from "$app/paths";
	import * as Card from "$lib/components/ui/card";
	import { Skeleton } from "$lib/components/ui/skeleton/index.js";
	import * as api from "$lib/cryptotrackerApi";
	import IntegrationAvatar from "$lib/components/integration-avatar.svelte";
	import IntegrationTypeBadge from "$lib/components/integration-type-badge.svelte";
	import SyncStatusBadge from "$lib/components/sync-status-badge.svelte";
	import { formatCurrency, formatRelativeTime } from "$lib/format";
	import { baseCurrency, updateIntervalMinutes } from "$lib/stores/config";
	import { isStale } from "$lib/integrations/health";

	let {
		integrations = [],
		skeleton = false
	}: { integrations?: api.IntegrationDto[]; skeleton?: boolean } = $props();
</script>

{#if skeleton}
	{#each { length: 6 }, i (i)}
		<Card.Root class="flex h-full flex-col">
			<Card.Content class="flex flex-col items-center gap-3">
				<Skeleton class="bg-muted size-16 rounded-full" />
				<Skeleton class="bg-muted h-4 w-32" />
				<Skeleton class="bg-muted h-3 w-20" />
			</Card.Content>
		</Card.Root>
	{/each}
{:else}
	{#each integrations as integration (integration.id)}
		{@const stale = isStale(integration, $updateIntervalMinutes)}
		<a href={resolve("/integrations/[slug]", { slug: integration.id })} class="group">
			<Card.Root
				class="hover:border-primary/20 relative flex h-full flex-col transition-all duration-200 group-hover:-translate-y-0.5 hover:shadow-md"
			>
				<div class="absolute top-3 right-3 flex flex-col items-end gap-1">
					<IntegrationTypeBadge isManual={integration.isManual} />
					<SyncStatusBadge {stale} />
				</div>
				<Card.Content class="flex flex-col items-center gap-3">
					<IntegrationAvatar
						name={integration.name}
						isManual={integration.isManual}
						class="size-16 text-xl"
					/>
					<div class="text-center">
						<p class="leading-tight font-semibold">{integration.name}</p>
						{#if integration.description}
							<p class="text-muted-foreground text-xs">{integration.description}</p>
						{/if}
						<p class="mt-2 text-xl font-semibold tracking-tight">
							{formatCurrency(integration.currentValue ?? 0, $baseCurrency)}
						</p>
						<p
							class="mt-1 text-xs {stale
								? 'text-amber-600 dark:text-amber-400'
								: 'text-muted-foreground'}"
						>
							{#if integration.lastSyncedAtUtc}
								{integration.isManual ? "Last measurement" : "Last sync"}
								{formatRelativeTime(integration.lastSyncedAtUtc)}
							{:else}
								No data yet
							{/if}
						</p>
					</div>
				</Card.Content>
			</Card.Root>
		</a>
	{/each}
{/if}
