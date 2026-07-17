<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { installAuthInterceptor, loginPath, refreshUser } from '$lib/api/client';
	import { theme } from '$lib/stores/theme.svelte';
	import Navbar from '$lib/components/navigation/navbar.svelte';
	import NavBreadcrumb from '$lib/components/navigation/NavBreadcrumb.svelte';
	import { Toaster } from '$lib/components/ui/sonner';
	import '../app.css';

	let { children } = $props();

	installAuthInterceptor();

	onMount(async () => {
		if (page.url.pathname.startsWith('/auth/')) return;
		const signedIn = await refreshUser();
		// eslint-disable-next-line svelte/no-navigation-without-resolve -- loginPath builds on resolve()
		if (!signedIn) goto(loginPath(page.url.pathname + page.url.search));
	});
</script>

<svelte:head>
	<title>CryptoTracker</title>
</svelte:head>
<Navbar />
<div class="container mx-auto px-6 pb-8">
	{#key page.url.pathname}
		<NavBreadcrumb />
	{/key}
	{@render children()}
</div>
<Toaster theme={theme.resolved} />
