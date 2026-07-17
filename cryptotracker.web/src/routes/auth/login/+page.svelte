<script lang="ts">
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";
	import { page } from "$app/state";
	import * as api from "$lib/cryptotrackerApi";
	import { refreshUser } from "$lib/api/client";
	import { Button } from "$lib/components/ui/button";
	import { Input } from "$lib/components/ui/input";
	import { Label } from "$lib/components/ui/label";
	import * as Alert from "$lib/components/ui/alert";
	import { Separator } from "$lib/components/ui/separator";
	import CircleAlertIcon from "@lucide/svelte/icons/circle-alert";
	import Loader2Icon from "@lucide/svelte/icons/loader-2";

	let email = $state("");
	let password = $state("");
	let error = $state<string | null>(null);
	let oidcEnabled = $state(false);
	let submitting = $state(false);

	async function checkOidc() {
		try {
			const response = await api.oidcEnabled();
			if (response.status === 200) {
				oidcEnabled = response.data === true;
			}
		} catch {
			// OIDC not available
		}
	}

	checkOidc();

	async function handleLogin() {
		error = null;
		submitting = true;
		try {
			const response = await api.login({ username: email, password });
			if (response.status === 200) {
				await refreshUser();
				const returnUrl = page.url.searchParams.get("returnUrl");
				// eslint-disable-next-line svelte/no-navigation-without-resolve -- returnUrl is an app-internal path, guarded below
				await goto(returnUrl?.startsWith("/") ? returnUrl : resolve("/"));
			} else {
				error = "Login failed. Please check your credentials.";
			}
		} catch {
			error = "An error occurred during login. Please try again.";
		} finally {
			submitting = false;
		}
	}
</script>

<svelte:head>
	<title>Login · CryptoTracker</title>
</svelte:head>

<div class="container mx-auto max-w-md pt-10">
	{#if error}
		<Alert.Root variant="destructive" class="mb-4">
			<CircleAlertIcon class="size-4" />
			<Alert.Title>{error}</Alert.Title>
		</Alert.Root>
	{/if}
	<form
		onsubmit={async (event) => {
			event.preventDefault();
			await handleLogin();
		}}
		class="space-y-4"
	>
		<div class="space-y-2">
			<Label for="email">Email</Label>
			<Input type="email" id="email" bind:value={email} required />
		</div>
		<div class="space-y-2">
			<Label for="password">Password</Label>
			<Input type="password" id="password" bind:value={password} required />
		</div>
		<Button type="submit" class="w-full" disabled={submitting}>
			{#if submitting}
				<Loader2Icon class="size-4 animate-spin" />
			{/if}
			Login
		</Button>
		{#if oidcEnabled}
			<Separator />
			<Button
				type="button"
				variant="secondary"
				class="w-full"
				onclick={() => {
					window.location.href = "/api/auth/oidc-login";
				}}
			>
				Login with OIDC
			</Button>
		{/if}
		<Separator />
		<p class="text-muted-foreground text-center text-sm">
			Don't have an account?
			<a href={resolve("/auth/register")} class="text-primary hover:underline">Register here</a>.
		</p>
	</form>
</div>
