<script lang="ts">
	import { goto } from '$app/navigation';
	import * as api from '$lib/cryptotrackerApi';
	import { onMount } from 'svelte';

	onMount(async () => {
		try {
			await api.logout();
			goto('/auth/login');
		} catch (err) {
			console.error('Logout failed', err);
		} finally {
			window.location.href = '/auth/login';
		}
	});
</script>
