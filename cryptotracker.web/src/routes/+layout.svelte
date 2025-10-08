<script lang="ts">
	import { user } from '$lib/stores/user';
	import * as api from '$lib/cryptotrackerApi';
	import { afterNavigate, goto } from '$app/navigation';
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import Navbar from '$lib/components/navigation/navbar.svelte';
	import NavBreadcrumb from '$lib/components/navigation/NavBreadcrumb.svelte';
	import '../app.css';
	let { children } = $props();

	async function checkAuth() {
		try {
			const currentPath = page.url.pathname;

			console.log(`Current path: ${currentPath}`);

			if (currentPath.startsWith('/auth/')) {
				return;
			}

			const res = await api.getMe();

			console.log(res);

			if (res.status === 200) {
				const data = await res.data;
				user.set(data);
				return;
			}
		} catch {
			// ignore
		}

		user.set(null);
		goto('/auth/login');
	}

	onMount(() => {
		checkAuth();

		afterNavigate(() => {
			checkAuth();
		});
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
