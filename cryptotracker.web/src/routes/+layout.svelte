<script lang="ts">
	import '../app.css';
	import Footer from '../components/footer.svelte';
	import Navbar from '../components/navigation/navbar.svelte';
	let { children } = $props();
</script>

<svelte:head>
	<title>cryptotracker</title>
</svelte:head>
<Navbar />
<div class="container mx-auto px-6 py-8">
	{@render children()}
</div>

<!-- <Footer /> -->
