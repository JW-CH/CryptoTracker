<script lang="ts">
	import { cn } from "$lib/utils";

	let {
		name = "",
		isManual = false,
		class: className
	}: { name?: string | null; isManual?: boolean; class?: string } = $props();
</script>

<div
	class={cn(
		"flex items-center justify-center rounded-full font-bold",
		isManual ? "bg-secondary text-secondary-foreground" : "bg-primary/10 text-primary",
		className
	)}
>
	{name ? name.slice(0, 2).toUpperCase() : "??"}
</div>
