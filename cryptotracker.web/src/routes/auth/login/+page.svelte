<script lang="ts">
	import { goto } from '$app/navigation';
	import * as api from '$lib/cryptotrackerApi';

	let email: string = '';
	let password: string = '';
	let error: string | null = null;

	async function handleLogin() {
		error = null;

		try {
			const response = await api.login({ username: email, password });

			if (response.status === 200) {
				goto('/');
			} else {
				error = 'Login failed. Please check your credentials.';
			}
		} catch (err) {
			error = 'An error occurred during login. Please try again.';
			console.error(err);
		}
	}
</script>

<div class="container mx-auto max-w-md pt-10">
	{#if error}
		<div class="mb-4 rounded bg-red-100 p-4 text-red-700">{error}</div>
	{/if}
	<form
		on:submit|preventDefault={() => {
			handleLogin();
		}}
		class="space-y-4"
	>
		<div>
			<label for="email" class="mb-2 block font-medium">Email</label>
			<input
				type="email"
				id="email"
				bind:value={email}
				required
				class="w-full rounded border border-gray-300 p-2"
			/>
		</div>
		<div>
			<label for="password" class="mb-2 block font-medium">Password</label>
			<input
				type="password"
				id="password"
				bind:value={password}
				required
				class="w-full rounded border border-gray-300 p-2"
			/>
		</div>
		<button
			type="submit"
			class="w-full rounded bg-blue-600 px-4 py-2 font-bold text-white hover:bg-blue-700"
		>
			Login
		</button>
		<hr />
		<button
			type="button"
			class="w-full rounded bg-green-600 px-4 py-2 font-bold text-white hover:bg-green-700"
			on:click={() => {
				window.location.href = '/api/auth/oidc-login';
			}}
		>
			Login with OIDC
		</button>
		<hr />
		<p class="text-center text-sm text-gray-600">
			Don't have an account?
			<a href="/auth/register" class="text-blue-600 hover:underline">Register here</a>.
		</p>
	</form>
</div>
