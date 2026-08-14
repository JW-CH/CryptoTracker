<script lang="ts">
	import { formatShare } from "$lib/format";

	let { segments }: { segments: { type: string; value: number; share: number }[] } = $props();

	// Color follows the entity, never the rank
	const TYPE_COLORS: Record<string, string> = {
		Crypto: "var(--chart-2)",
		Stock: "var(--chart-4)",
		Fiat: "var(--chart-6)"
	};
	const colorFor = (type: string) => TYPE_COLORS[type] ?? "var(--chart-other)";
</script>

{#if segments.length > 1}
	<div class="space-y-2">
		<div class="flex h-2.5 w-full gap-0.5 overflow-hidden rounded-full">
			{#each segments as segment (segment.type)}
				<div style="flex: {segment.value}; background: {colorFor(segment.type)}"></div>
			{/each}
		</div>
		<div class="text-muted-foreground flex flex-wrap gap-x-4 gap-y-1 text-sm">
			{#each segments as segment (segment.type)}
				<span class="flex items-center gap-1.5">
					<span class="size-2.5 rounded-full" style="background: {colorFor(segment.type)}"></span>
					{segment.type}
					{formatShare(segment.share)}
				</span>
			{/each}
		</div>
	</div>
{/if}
