<script lang="ts">
	import * as Card from "$lib/components/ui/card";
	import { Skeleton } from "$lib/components/ui/skeleton";
	import TrendingUpIcon from "@lucide/svelte/icons/trending-up";
	import TrendingDownIcon from "@lucide/svelte/icons/trending-down";

	let {
		label,
		value,
		delta = null,
		deltaLabel,
		href,
		skeleton = false
	}: {
		label: string;
		value: string;
		delta?: { text: string; direction: "up" | "down" | "flat" } | null;
		deltaLabel?: string;
		href?: string;
		skeleton?: boolean;
	} = $props();
</script>

{#snippet tile()}
	<Card.Root
		size="sm"
		class={href
			? "h-full transition-all group-hover:-translate-y-0.5 group-hover:shadow-lg"
			: "h-full"}
	>
		<Card.Content class="space-y-1">
			<p class="text-muted-foreground text-sm font-medium">{label}</p>
			<p class="text-2xl font-semibold tracking-tight">{value}</p>
			{#if delta}
				<p
					class="flex items-center gap-1.5 text-sm {delta.direction === 'up'
						? 'text-gain'
						: delta.direction === 'down'
							? 'text-loss'
							: 'text-muted-foreground'}"
				>
					{#if delta.direction === "up"}
						<TrendingUpIcon class="size-4" />
					{:else if delta.direction === "down"}
						<TrendingDownIcon class="size-4" />
					{/if}
					<span class="font-medium">{delta.text}</span>
					{#if deltaLabel}
						<span class="text-muted-foreground font-normal">{deltaLabel}</span>
					{/if}
				</p>
			{/if}
		</Card.Content>
	</Card.Root>
{/snippet}

{#if skeleton}
	<Skeleton class="h-28 w-full rounded-4xl" />
{:else if href}
	<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- callers pass resolved hrefs -->
	<a {href} class="group block">
		{@render tile()}
	</a>
{:else}
	{@render tile()}
{/if}
