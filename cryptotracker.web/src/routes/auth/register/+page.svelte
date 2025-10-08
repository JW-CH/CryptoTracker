<script lang="ts">
	import { goto } from '$app/navigation';
	import * as api from '$lib/cryptotrackerApi';

	let email: string = '';
	let password: string = '';
	let confirmPassword: string = '';
	let error: string | null = null;
</script>

<div class="container mx-auto max-w-md pt-10">
	{#if error}
		<div class="mb-4 rounded bg-red-100 p-4 text-red-700">{error}</div>
	{/if}
	<form
		on:submit|preventDefault={async () => {
			try {
				if (password !== confirmPassword) {
					error = 'Passwords do not match.';
					return;
				}

				const response = await api.register({ email, username: email, password });
				if (response.status === 200) {
					goto('/');
				} else {
					error = 'Registration failed. Please try again.';
				}
			} catch (err) {
				error = 'An error occurred during registration. Please try again.';
			}
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
		<div>
			<label for="confirmPassword" class="mb-2 block font-medium">Confirm Password</label>
			<input
				type="password"
				id="confirmPassword"
				bind:value={confirmPassword}
				required
				class="w-full rounded border border-gray-300 p-2"
			/>
		</div>
		<button
			type="submit"
			class="w-full rounded bg-blue-600 px-4 py-2 font-bold text-white hover:bg-blue-700"
		>
			Register
		</button>
		<hr />
		<p class="text-center text-sm text-gray-600">
			Already have an account?
			<a href="/auth/login" class="text-blue-600 hover:underline">Login here</a>.
		</p>
	</form>
</div>
