<script lang="ts">
	import * as api from "$lib/cryptotrackerApi";
	import { mutate } from "$lib/api/mutate";
	import * as AlertDialog from "$lib/components/ui/alert-dialog";

	let {
		integration,
		open = $bindable(false),
		onDeleted
	}: {
		integration: api.IntegrationDto;
		open?: boolean;
		onDeleted?: () => void | Promise<void>;
	} = $props();

	async function confirmDelete() {
		const result = await mutate(() => api.deleteIntegration(integration.id), {
			success: "Integration deleted."
		});
		if (result !== null) await onDeleted?.();
	}
</script>

<AlertDialog.Root bind:open>
	<AlertDialog.Content>
		<AlertDialog.Header>
			<AlertDialog.Title>Delete integration?</AlertDialog.Title>
			<AlertDialog.Description>
				{integration.name} and all its measurements will be removed. This cannot be undone.
				{#if !integration.isManual}
					This integration comes from the server configuration — it will reappear (empty) after the
					next sync unless removed there.
				{/if}
			</AlertDialog.Description>
		</AlertDialog.Header>
		<AlertDialog.Footer>
			<AlertDialog.Cancel>Cancel</AlertDialog.Cancel>
			<AlertDialog.Action onclick={confirmDelete}>Delete</AlertDialog.Action>
		</AlertDialog.Footer>
	</AlertDialog.Content>
</AlertDialog.Root>
