<script lang="ts">
	import { page } from '$app/state';
	import Navbar from '$lib/components/navigation/navbar.svelte';
	import NavBreadcrumb from '$lib/components/navigation/NavBreadcrumb.svelte';
	import '../app.css';
	let { children } = $props();
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
