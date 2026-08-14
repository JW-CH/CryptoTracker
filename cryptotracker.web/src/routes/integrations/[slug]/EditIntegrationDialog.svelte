<script lang="ts">
	import * as api from "$lib/cryptotrackerApi";
	import { mutate } from "$lib/api/mutate";
	import { Button } from "$lib/components/ui/button";
	import { Input } from "$lib/components/ui/input";
	import { Label } from "$lib/components/ui/label";
	import * as Dialog from "$lib/components/ui/dialog";
	import Loader2Icon from "@lucide/svelte/icons/loader-2";

	let {
		integration,
		open = $bindable(false),
		onSaved
	}: {
		integration: api.IntegrationDto;
		open?: boolean;
		onSaved?: () => void | Promise<void>;
	} = $props();

	let name = $state("");
	let description = $state("");
	let saving = $state(false);

	// Re-seed the form each time the dialog opens
	$effect(() => {
		if (open) {
			name = integration.name ?? "";
			description = integration.description ?? "";
		}
	});

	async function save() {
		if (!name.trim()) return;
		saving = true;
		const result = await mutate(
			() => api.updateIntegration(integration.id, { name: name.trim(), description }),
			{ success: "Integration updated." }
		);
		saving = false;
		if (result !== null) {
			open = false;
			await onSaved?.();
		}
	}
</script>

<Dialog.Root bind:open>
	<Dialog.Content>
		<Dialog.Header>
			<Dialog.Title>Edit integration</Dialog.Title>
		</Dialog.Header>
		<form
			class="space-y-4"
			onsubmit={(event) => {
				event.preventDefault();
				save();
			}}
		>
			<div class="space-y-2">
				<Label for="edit-name">Name</Label>
				<Input id="edit-name" bind:value={name} disabled={!integration.isManual} />
				{#if !integration.isManual}
					<p class="text-muted-foreground text-xs">
						Automatic integrations are named by the server configuration.
					</p>
				{/if}
			</div>
			<div class="space-y-2">
				<Label for="edit-description">Description</Label>
				<Input id="edit-description" bind:value={description} />
			</div>
			<Dialog.Footer>
				<Button type="submit" disabled={saving}>
					{#if saving}
						<Loader2Icon class="size-4 animate-spin" />
					{/if}
					Save
				</Button>
			</Dialog.Footer>
		</form>
	</Dialog.Content>
</Dialog.Root>
