<script lang="ts">
	import { goto } from '$app/navigation';
	import * as api from '$lib/cryptotrackerApi';
	import { onMount } from 'svelte';

	onMount(async () => {
		try {
			await api.logout();
		} catch (err) {
			console.error('Logout failed', err);
		}

		goto('/auth/login');
	});
</script>
