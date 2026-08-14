<script lang="ts">
	import { Badge } from "$lib/components/ui/badge";
	import TriangleAlertIcon from "@lucide/svelte/icons/triangle-alert";

	let { stale = false }: { stale?: boolean } = $props();
</script>

{#if stale}
	<Badge variant="outline" class="border-amber-600/40 text-amber-600 dark:text-amber-400">
		<TriangleAlertIcon class="size-3" />
		Sync overdue
	</Badge>
{/if}
