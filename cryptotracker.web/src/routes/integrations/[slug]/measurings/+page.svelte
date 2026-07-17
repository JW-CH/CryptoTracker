<script lang="ts">
	import { onMount } from "svelte";
	import { page } from "$app/state";
	import * as api from "$lib/cryptotrackerApi";
	import { mutate } from "$lib/api/mutate";
	import { formatDate } from "$lib/format";
	import PageHeader from "$lib/components/page-header.svelte";
	import SearchCombobox from "$lib/components/search-combobox.svelte";
	import { Button } from "$lib/components/ui/button";
	import { Input } from "$lib/components/ui/input";
	import { Label } from "$lib/components/ui/label";
	import * as Table from "$lib/components/ui/table";
	import * as Dialog from "$lib/components/ui/dialog";
	import * as AlertDialog from "$lib/components/ui/alert-dialog";
	import { Skeleton } from "$lib/components/ui/skeleton";
	import Loader2Icon from "@lucide/svelte/icons/loader-2";
	import PencilIcon from "@lucide/svelte/icons/pencil";
	import Trash2Icon from "@lucide/svelte/icons/trash-2";

	const slug = $derived(page.params.slug ?? "");

	let measurings = $state<api.DailyHoldingDto[] | null>(null);

	async function load() {
		try {
			const res = await api.getMeasuringsByIntegration(slug);
			measurings = res.status === 200 ? res.data : [];
		} catch {
			measurings = [];
		}
	}
	onMount(load);

	// Sort in script, on a copy — never in the template, never in place
	const sorted = $derived(
		[...(measurings ?? [])].sort(
			(a, b) =>
				(b.date ?? "").localeCompare(a.date ?? "") || (a.symbol ?? "").localeCompare(b.symbol ?? "")
		)
	);

	const keyOf = (m: api.DailyHoldingDto) => `${m.symbol}|${m.date}`;

	// ── Inline edit: same endpoint, upsert on (integration, symbol, date) ──
	let editKey = $state<string | null>(null);
	let editValue = $state("");
	let savingEdit = $state(false);

	function startEdit(m: api.DailyHoldingDto) {
		editKey = keyOf(m);
		editValue = String(m.amount ?? 0);
	}

	async function saveEdit(m: api.DailyHoldingDto) {
		const amount = Number(editValue);
		if (!Number.isFinite(amount)) return;
		savingEdit = true;
		const result = await mutate(
			() => api.addIntegrationMeasuring(slug, { symbol: m.symbol, date: m.date, amount }),
			{ success: `${m.symbol} on ${formatDate(m.date ?? "")} updated.` }
		);
		savingEdit = false;
		if (result !== null) {
			editKey = null;
			await load();
		}
	}

	// ── Delete with confirmation ──
	let deleteTarget = $state<api.DailyHoldingDto | null>(null);

	async function confirmDelete() {
		const m = deleteTarget;
		if (!m) return;
		const result = await mutate(
			() => api.deleteIntegrationMeasuring(m.integrationId, m.symbol ?? "", m.date ?? ""),
			{ success: `${m.symbol} on ${formatDate(m.date ?? "")} deleted.` }
		);
		if (result !== null) await load();
	}

	// ── Add as dialog: stay on the page, see the new row ──
	let addOpen = $state(false);
	let assets = $state<api.AssetDto[] | null>(null);
	let addSymbol = $state("");
	let addDate = $state(new Date().toISOString().split("T")[0]);
	let addAmount = $state(0);
	let savingAdd = $state(false);

	$effect(() => {
		if (addOpen && assets === null) {
			api
				.getAssets()
				.then((r) => (assets = r.status === 200 ? r.data : []))
				.catch(() => (assets = []));
		}
	});

	const assetOptions = $derived(
		(assets ?? []).map((a) => ({
			value: a.symbol ?? "",
			label: a.name ? `${a.name} (${(a.symbol ?? "").toUpperCase()})` : (a.symbol ?? "")
		}))
	);

	async function saveAdd() {
		if (!addDate || !addSymbol) return;
		savingAdd = true;
		const result = await mutate(
			() =>
				api.addIntegrationMeasuring(slug, { symbol: addSymbol, date: addDate, amount: addAmount }),
			{ success: `Measurement for ${addSymbol.toUpperCase()} saved.` }
		);
		savingAdd = false;
		if (result !== null) {
			addOpen = false;
			addAmount = 0;
			await load();
		}
	}
</script>

<svelte:head>
	<title>Measurements · CryptoTracker</title>
</svelte:head>

<div class="space-y-6">
	<PageHeader title="Measurements">
		{#snippet actions()}
			<Button size="sm" onclick={() => (addOpen = true)}>+ Measurement</Button>
		{/snippet}
	</PageHeader>

	{#if measurings === null}
		<Skeleton class="h-64 w-full" />
	{:else if sorted.length === 0}
		<p class="text-muted-foreground">No measurements yet — add the first one.</p>
	{:else}
		<Table.Root>
			<Table.Header>
				<Table.Row>
					<Table.Head>Date</Table.Head>
					<Table.Head>Asset</Table.Head>
					<Table.Head class="text-right">Amount</Table.Head>
					<Table.Head class="w-32"></Table.Head>
				</Table.Row>
			</Table.Header>
			<Table.Body>
				{#each sorted as m (keyOf(m))}
					<Table.Row>
						<Table.Cell>{formatDate(m.date ?? "")}</Table.Cell>
						<Table.Cell class="font-medium">{m.symbol}</Table.Cell>
						<Table.Cell class="text-right tabular-nums">
							{#if editKey === keyOf(m)}
								<Input
									type="number"
									step="any"
									class="ml-auto h-8 w-36 text-right"
									bind:value={editValue}
									disabled={savingEdit}
									onkeydown={(e) => {
										if (e.key === "Enter") saveEdit(m);
										if (e.key === "Escape") editKey = null;
									}}
								/>
							{:else}
								{m.amount}
							{/if}
						</Table.Cell>
						<Table.Cell>
							<div class="flex justify-end gap-1">
								{#if editKey === keyOf(m)}
									<Button
										variant="ghost"
										size="sm"
										disabled={savingEdit}
										onclick={() => saveEdit(m)}
									>
										{#if savingEdit}
											<Loader2Icon class="size-4 animate-spin" />
										{:else}
											Save
										{/if}
									</Button>
									<Button
										variant="ghost"
										size="sm"
										disabled={savingEdit}
										onclick={() => (editKey = null)}
									>
										Cancel
									</Button>
								{:else}
									<Button
										variant="ghost"
										size="icon-sm"
										aria-label="Edit amount"
										onclick={() => startEdit(m)}
									>
										<PencilIcon class="size-4" />
									</Button>
									<Button
										variant="ghost"
										size="icon-sm"
										aria-label="Delete measurement"
										onclick={() => (deleteTarget = m)}
									>
										<Trash2Icon class="size-4" />
									</Button>
								{/if}
							</div>
						</Table.Cell>
					</Table.Row>
				{/each}
			</Table.Body>
		</Table.Root>
	{/if}
</div>

<Dialog.Root bind:open={addOpen}>
	<Dialog.Content>
		<Dialog.Header>
			<Dialog.Title>Add measurement</Dialog.Title>
		</Dialog.Header>
		<form
			class="space-y-4"
			onsubmit={(event) => {
				event.preventDefault();
				saveAdd();
			}}
		>
			<div class="space-y-2">
				<Label for="add-date">Date</Label>
				<Input id="add-date" type="date" bind:value={addDate} />
			</div>
			<div class="space-y-2">
				<Label for="add-asset">Asset</Label>
				<SearchCombobox
					id="add-asset"
					items={assetOptions}
					bind:value={addSymbol}
					disabled={assets === null}
					placeholder={assets === null ? "Loading…" : "Select asset"}
					searchPlaceholder="Search by name or symbol…"
				/>
			</div>
			<div class="space-y-2">
				<Label for="add-amount">Amount</Label>
				<Input id="add-amount" type="number" step="any" bind:value={addAmount} />
			</div>
			<Dialog.Footer>
				<Button type="submit" disabled={savingAdd}>
					{#if savingAdd}
						<Loader2Icon class="size-4 animate-spin" />
					{/if}
					Save
				</Button>
			</Dialog.Footer>
		</form>
	</Dialog.Content>
</Dialog.Root>

<AlertDialog.Root
	open={deleteTarget !== null}
	onOpenChange={(open) => {
		if (!open) deleteTarget = null;
	}}
>
	<AlertDialog.Content>
		<AlertDialog.Header>
			<AlertDialog.Title>Delete measurement?</AlertDialog.Title>
			<AlertDialog.Description>
				{deleteTarget?.symbol} on {deleteTarget ? formatDate(deleteTarget.date ?? "") : ""} will be removed.
				This cannot be undone.
			</AlertDialog.Description>
		</AlertDialog.Header>
		<AlertDialog.Footer>
			<AlertDialog.Cancel>Cancel</AlertDialog.Cancel>
			<AlertDialog.Action onclick={confirmDelete}>Delete</AlertDialog.Action>
		</AlertDialog.Footer>
	</AlertDialog.Content>
</AlertDialog.Root>
