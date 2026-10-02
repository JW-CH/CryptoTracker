<script lang="ts">
	import { Badge } from "#lib/components/ui/badge/index.js";

	let { isManual = false }: { isManual?: boolean } = $props();
</script>

<Badge variant={isManual ? "secondary" : "default"}>{isManual ? "Manual" : "Automatic"}</Badge>
