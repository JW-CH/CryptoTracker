<script lang="ts">
	import NavItem from './nav-item.svelte';

	let showMenu = false;

	function toggleNavbar() {
		showMenu = !showMenu;
	}
</script>

<div class="bg-gray-400">
	<nav class="container mx-auto w-screen px-6 py-8 md:flex md:items-center md:justify-between">
		<!-- svelte-ignore a11y_no_static_element_interactions -->
		<div class="flex items-center justify-between">
			<a class="text-xl font-bold text-gray-800 hover:text-blue-400 md:text-2xl" href="/"
				>cryptotracker
			</a>
			<!-- Mobile menu button -->
			<!-- svelte-ignore a11y_click_events_have_key_events -->
			<div on:click={toggleNavbar} class="flex md:hidden">
				<button
					type="button"
					class="text-gray-800 hover:text-gray-400 focus:text-gray-400 focus:outline-none"
				>
					<svg
						xmlns="http://www.w3.org/2000/svg"
						fill="none"
						viewBox="0 0 24 24"
						stroke-width="1.5"
						stroke="currentColor"
						class="h-6 w-6"
					>
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5"
						/>
					</svg>
					test
				</button>
			</div>
		</div>

		<!-- Mobile Menu open: "block", Menu closed: "hidden" -->
		<div
			class="mt-8 flex-col space-y-4 md:mt-0 md:flex md:flex-row md:items-center md:space-x-10 md:space-y-0 {showMenu
				? 'flex'
				: 'hidden'}"
		>
			<NavItem path="" text="Home" />
			<NavItem path="stats" text="Stats" />
		</div>
	</nav>
</div>
