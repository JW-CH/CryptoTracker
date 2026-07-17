<script lang="ts">
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";
	import * as api from "$lib/cryptotrackerApi";
	import { user } from "$lib/stores/user";
	import { onMount } from "svelte";

	onMount(async () => {
		try {
			await api.logout();
		} catch {
			// even if the request fails, treat the client as signed out
		}
		user.set(null);
		await goto(resolve("/auth/login"));
	});
</script>

<p class="text-muted-foreground">Signing out…</p>
