<script lang="ts">
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";
	import { user } from "$lib/stores/user";
	import NavItem from "./nav-item.svelte";
	import ThemeToggle from "./theme-toggle.svelte";
	import * as Avatar from "$lib/components/ui/avatar";
	import * as DropdownMenu from "$lib/components/ui/dropdown-menu";
	import * as Sheet from "$lib/components/ui/sheet";
	import { buttonVariants } from "$lib/components/ui/button";
	import { cn } from "$lib/utils";
	import ChartPieIcon from "@lucide/svelte/icons/chart-pie";
	import LogOutIcon from "@lucide/svelte/icons/log-out";
	import MenuIcon from "@lucide/svelte/icons/menu";

	const links = [
		{ path: resolve("/"), text: "Home" },
		{ path: resolve("/report"), text: "Report" },
		{ path: resolve("/integrations"), text: "Integrations" },
		{ path: resolve("/assets"), text: "Assets" }
	];

	let mobileOpen = $state(false);

	const initials = $derived.by(() => {
		const source = $user?.displayName ?? $user?.email ?? "";
		const parts = source.split(/[\s@._-]+/).filter(Boolean);
		return ((parts[0]?.[0] ?? "") + (parts[1]?.[0] ?? parts[0]?.[1] ?? "")).toUpperCase() || "?";
	});
</script>

<header class="bg-background/80 sticky top-0 z-40 border-b backdrop-blur">
	<div class="container mx-auto flex h-16 items-center gap-6 px-6">
		<Sheet.Root bind:open={mobileOpen}>
			<Sheet.Trigger
				class={cn(buttonVariants({ variant: "ghost", size: "icon" }), "md:hidden")}
				aria-label="Open navigation"
			>
				<MenuIcon class="size-5" />
			</Sheet.Trigger>
			<Sheet.Content side="left" class="w-64">
				<!-- close the sheet when any link inside is clicked (event delegation) -->
				<!-- svelte-ignore a11y_no_noninteractive_element_interactions, a11y_click_events_have_key_events -->
				<nav
					class="mt-10 flex flex-col items-start gap-5 px-6"
					onclick={() => (mobileOpen = false)}
				>
					{#each links as link (link.path)}
						<NavItem path={link.path} text={link.text} />
					{/each}
				</nav>
			</Sheet.Content>
		</Sheet.Root>

		<a href={resolve("/")} class="flex items-center gap-2 font-semibold">
			<ChartPieIcon class="text-primary size-5" />
			CryptoTracker
		</a>

		<nav class="hidden items-center gap-6 md:flex">
			{#each links as link (link.path)}
				<NavItem path={link.path} text={link.text} />
			{/each}
		</nav>

		<div class="ml-auto flex items-center gap-1">
			<ThemeToggle />
			{#if $user}
				<DropdownMenu.Root>
					<DropdownMenu.Trigger class="ml-1 rounded-full" aria-label="Account menu">
						<Avatar.Root class="size-8">
							<Avatar.Fallback class="text-xs font-medium">{initials}</Avatar.Fallback>
						</Avatar.Root>
					</DropdownMenu.Trigger>
					<DropdownMenu.Content align="end">
						<DropdownMenu.Label>
							<div class="text-sm font-medium">{$user.displayName ?? $user.email}</div>
							{#if $user.displayName && $user.email}
								<div class="text-muted-foreground text-xs font-normal">{$user.email}</div>
							{/if}
						</DropdownMenu.Label>
						<DropdownMenu.Separator />
						<DropdownMenu.Item onclick={() => goto(resolve("/auth/logout"))}>
							<LogOutIcon class="size-4" />
							Logout
						</DropdownMenu.Item>
					</DropdownMenu.Content>
				</DropdownMenu.Root>
			{:else}
				<NavItem path={resolve("/auth/login")} text="Login" />
			{/if}
		</div>
	</div>
</header>
