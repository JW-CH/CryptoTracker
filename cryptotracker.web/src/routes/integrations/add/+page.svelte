<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import * as api from '$lib/cryptotrackerApi';
	import { mutate } from '$lib/api/mutate';
	import Loader2Icon from '@lucide/svelte/icons/loader-2';

	let name = $state('');
	let description = $state('');
	let saving = $state(false);
	let validationError = $state<string | null>(null);

	async function save() {
		validationError = null;
		if (!name.trim()) {
			validationError = 'Please enter a name.';
			return;
		}
		saving = true;
		await mutate(() => api.addIntegration({ name, description: description || null }), {
			success: `Integration "${name}" added.`,
			onSuccess: () => goto(resolve('/integrations'))
		});
		saving = false;
	}
</script>

<svelte:head>
	<title>Add integration · CryptoTracker</title>
</svelte:head>

<Card.Root class="max-w-lg">
	<Card.Header>
		<Card.Title>Add integration</Card.Title>
	</Card.Header>
	<Card.Content>
		<form
			class="space-y-4"
			onsubmit={async (event) => {
				event.preventDefault();
				await save();
			}}
		>
			<div class="space-y-2">
				<Label for="name">Name</Label>
				<Input id="name" bind:value={name} placeholder="e.g. Hardware Wallet" />
				{#if validationError}
					<p class="text-destructive text-sm">{validationError}</p>
				{/if}
			</div>
			<div class="space-y-2">
				<Label for="description">Description (optional)</Label>
				<Input id="description" bind:value={description} />
			</div>
			<Button type="submit" disabled={saving}>
				{#if saving}
					<Loader2Icon class="size-4 animate-spin" />
				{/if}
				Save
			</Button>
		</form>
	</Card.Content>
</Card.Root>
