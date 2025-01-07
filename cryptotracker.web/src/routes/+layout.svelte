<script lang="ts">
	import { page } from '$app/state';
	import '../app.css';
	import Footer from '../components/footer.svelte';
	import Navbar from '../components/navigation/navbar.svelte';
	import NavBreadcrumb from '../components/navigation/NavBreadcrumb.svelte';
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
