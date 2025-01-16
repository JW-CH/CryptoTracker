<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import * as api from '$lib/cryptotrackerApi';

	let name: string = '';
	let description: string | null = null;

	async function AddIntegration() {
		if (!name) return;

		let request = await api.addIntegration({
			name: name,
			description: description
		});

		if (request.data) {
			window.location.href = '/integrations';
		}
	}
</script>

<Card.Root class="col-span-4">
	<Card.Header>
		<Card.Title>Messung hinzufügen</Card.Title>
	</Card.Header>
	<Card.Content>
		<input
			bind:value={name}
			type="text"
			class="rounded-lg border-2 border-solid border-gray-200 px-3 py-2 text-sm focus:border-blue-500 focus:ring-blue-500 disabled:pointer-events-none disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-400 dark:placeholder-neutral-500 dark:focus:ring-neutral-600"
		/>
		<input
			bind:value={description}
			type="text"
			class="rounded-lg border-2 border-solid border-gray-200 px-3 py-2 text-sm focus:border-blue-500 focus:ring-blue-500 disabled:pointer-events-none disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-400 dark:placeholder-neutral-500 dark:focus:ring-neutral-600"
		/>
		<Button on:click={AddIntegration}>Speichern</Button>
	</Card.Content>
</Card.Root>
