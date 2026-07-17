<script lang="ts">
	import type { Snippet } from 'svelte';

	let {
		title,
		subtitle,
		media,
		meta,
		actions
	}: {
		title: string;
		subtitle?: string;
		/** Avatar/image left of the title */
		media?: Snippet;
		/** Badge or similar next to the title */
		meta?: Snippet;
		/** Action buttons, right-aligned */
		actions?: Snippet;
	} = $props();
</script>

<div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
	<div class="flex items-center gap-4">
		{#if media}
			{@render media()}
		{/if}
		<div>
			<div class="flex items-center gap-3">
				<h1 class="text-2xl font-bold tracking-tight">{title}</h1>
				{#if meta}
					{@render meta()}
				{/if}
			</div>
			{#if subtitle}
				<p class="text-muted-foreground text-sm">{subtitle}</p>
			{/if}
		</div>
	</div>
	{#if actions}
		<div class="flex flex-wrap gap-2">
			{@render actions()}
		</div>
	{/if}
</div>
