<script lang="ts">
	import { user } from '$lib/stores/user';
	import * as api from '$lib/cryptotrackerApi';
	import { afterNavigate, goto } from '$app/navigation';
	import { onDestroy, onMount } from 'svelte';
	import { page } from '$app/state';
	import Navbar from '$lib/components/navigation/navbar.svelte';
	import NavBreadcrumb from '$lib/components/navigation/NavBreadcrumb.svelte';
	import '../app.css';
	let { children } = $props();

	async function checkAuth() {
		try {
			const currentPath = page.url.pathname;

			if (currentPath.startsWith('/auth/')) {
				return;
			}

			const res = await api.getMe();

			if (res.status === 200) {
				const data = await res.data;
				user.set(data);
				return;
			}
		} catch (err) {
			// Authentication check failed, redirecting to login
			console.error('Authentication check failed', err);
		}

		user.set(null);
		goto('/auth/login');
	}

	onMount(() => {
		checkAuth();
	});

	afterNavigate(() => {
		checkAuth();
	});
</script>

<svelte:head>
	<title>cryptotracker</title>
</svelte:head>
<Navbar />
<div class="container mx-auto px-6 pb-8">
	{#key page.url.pathname}
		<NavBreadcrumb />
	{/key}
	{@render children()}
</div>

<!-- <Footer /> -->
