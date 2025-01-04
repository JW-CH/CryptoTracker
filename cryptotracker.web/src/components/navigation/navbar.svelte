<script lang="ts">
	import NavItem from './nav-item.svelte';
	import { cn } from '$lib/utils.js';

	let className: string | undefined | null = undefined;
	export { className as class };

	let showMenu = false;

	function toggleNavbar() {
		showMenu = !showMenu;
	}
</script>

<div class="border-b-2">
	<div class="container mx-auto flex h-16 items-center px-4">
		<nav class={cn('flex items-center space-x-4 lg:space-x-6', className)}>
			<NavItem path="" text="Home" />
			<NavItem path="stats" text="Stats" />
			<NavItem path="info" text="Info" />
		</nav>
	</div>
</div>
