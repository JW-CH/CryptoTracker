<script lang="ts">
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";
	import * as api from "$lib/cryptotrackerApi";
	import { baseCurrency } from "$lib/stores/config";
	import { formatAmount, formatCurrency, formatShare } from "$lib/format";
	import PageHeader from "$lib/components/page-header.svelte";
	import { Button } from "$lib/components/ui/button";
	import { Input } from "$lib/components/ui/input";
	import { Skeleton } from "$lib/components/ui/skeleton";
	import * as Table from "$lib/components/ui/table";
	import ChevronLeftIcon from "@lucide/svelte/icons/chevron-left";
	import ChevronRightIcon from "@lucide/svelte/icons/chevron-right";

	let { data } = $props();

	const today = new Date().toISOString().split("T")[0];

	function gotoDate(date: string) {
		if (!date || date === data.date) return;
		// eslint-disable-next-line svelte/no-navigation-without-resolve -- query-only navigation on the current route
		goto(`${resolve("/report")}?date=${date}`, { keepFocus: true, noScroll: true });
	}

	function shiftDay(delta: number) {
		const shifted = new Date(new Date(data.date).getTime() + delta * 86_400_000);
		gotoDate(shifted.toISOString().split("T")[0]);
	}

	function analyze(holdings: api.AssetHoldingDto[]) {
		const sorted = [...holdings].sort((a, b) => (b.totalValue ?? 0) - (a.totalValue ?? 0));
		const total = sorted.reduce((acc, h) => acc + (h.totalValue ?? 0), 0);
		return { sorted, total };
	}
</script>

<svelte:head>
	<title>Report · CryptoTracker</title>
</svelte:head>

<div class="space-y-6">
	<PageHeader title="Report">
		{#snippet actions()}
			<Button
				variant="outline"
				size="icon-sm"
				aria-label="Previous day"
				onclick={() => shiftDay(-1)}
			>
				<ChevronLeftIcon class="size-4" />
			</Button>
			<Input
				type="date"
				class="w-40"
				max={today}
				value={data.date}
				onchange={(e) => gotoDate(e.currentTarget.value)}
			/>
			<Button
				variant="outline"
				size="icon-sm"
				aria-label="Next day"
				disabled={data.date >= today}
				onclick={() => shiftDay(1)}
			>
				<ChevronRightIcon class="size-4" />
			</Button>
		{/snippet}
	</PageHeader>

	{#await data.holdings}
		<Skeleton class="h-96 w-full rounded-4xl" />
	{:then holdings}
		{@const report = analyze(holdings)}
		{#if report.sorted.length === 0}
			<p class="text-muted-foreground py-16 text-center">No data for this day.</p>
		{:else}
			<Table.Root>
				<Table.Header>
					<Table.Row>
						<Table.Head>Asset</Table.Head>
						<Table.Head class="text-right">Amount</Table.Head>
						<Table.Head class="text-right">Price</Table.Head>
						<Table.Head class="text-right">Value</Table.Head>
						<Table.Head class="text-right">Share</Table.Head>
					</Table.Row>
				</Table.Header>
				<Table.Body>
					{#each report.sorted as holding (holding.asset.symbol)}
						<Table.Row>
							<Table.Cell class="font-medium">
								<a
									class="hover:underline"
									href={resolve("/assets/[slug]", { slug: holding.asset.symbol ?? "" })}
								>
									{holding.asset.name ?? holding.asset.symbol}
								</a>
							</Table.Cell>
							<Table.Cell class="text-right tabular-nums">
								{formatAmount(holding.totalAmount ?? 0, holding.asset.assetType)}
								{holding.asset.symbol}
							</Table.Cell>
							<Table.Cell class="text-right tabular-nums">
								{formatCurrency(holding.price ?? 0, $baseCurrency)}
							</Table.Cell>
							<Table.Cell class="text-right tabular-nums">
								{formatCurrency(holding.totalValue ?? 0, $baseCurrency)}
							</Table.Cell>
							<Table.Cell class="text-right tabular-nums">
								{report.total > 0 ? formatShare((holding.totalValue ?? 0) / report.total) : "—"}
							</Table.Cell>
						</Table.Row>
					{/each}
				</Table.Body>
				<Table.Footer>
					<Table.Row>
						<Table.Cell class="font-semibold">Total</Table.Cell>
						<Table.Cell colspan={2}></Table.Cell>
						<Table.Cell class="text-right font-semibold tabular-nums">
							{formatCurrency(report.total, $baseCurrency)}
						</Table.Cell>
						<Table.Cell class="text-right tabular-nums">100%</Table.Cell>
					</Table.Row>
				</Table.Footer>
			</Table.Root>
		{/if}
	{/await}
</div>
