<script lang="ts">
	import { page } from '$app/state';
	import { cn } from '$lib/utils';

	let { path = '', text }: { path?: string; text: string } = $props();

	// "/" would match every path via startsWith — needs an exact comparison
	const active = $derived(
		path === '/' ? page.url.pathname === '/' : path !== '' && page.url.pathname.startsWith(path)
	);
</script>

{#if path === ''}
	<span class="text-muted-foreground cursor-default text-sm font-medium">{text}</span>
{:else}
	<!-- eslint-disable svelte/no-navigation-without-resolve -->
	<!-- static app paths passed by navbar -->
	<a
		href={path}
		class={cn(
			'text-muted-foreground hover:text-foreground text-sm font-medium transition-colors',
			active && 'text-foreground decoration-primary underline decoration-2 underline-offset-8'
		)}
	>
		{text}
	</a>
{/if}
