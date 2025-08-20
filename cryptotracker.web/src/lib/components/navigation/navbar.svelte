<script lang="ts">
	import NavItem from './nav-item.svelte';
	import { cn } from '$lib/utils.js';
	import { user } from '$lib/stores/user';

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
			<NavItem path="report" text="Report" />
			<NavItem path="integrations" text="Integrationen" />
			<NavItem path="assets" text="Vermögenswerte" />
			<NavItem path="info" text="Info" />
			{#if $user.authenticated}
				<NavItem path="user/me" text="Profil ({$user.name})" />
				<NavItem path="user/logout" text="Logout" />
			{:else}
				<NavItem path="user/login" text="Login" />
			{/if}
		</nav>
	</div>
</div>
