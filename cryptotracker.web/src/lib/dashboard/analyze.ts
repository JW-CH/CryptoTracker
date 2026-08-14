import * as api from "$lib/cryptotrackerApi";
import { OTHER_SYMBOL } from "$lib/charts/palette";

export type Measurings = { [key: string]: api.AssetHoldingDto[] };

export type Delta = { value: number; pct: number | null };
export type Mover = { symbol: string; pct: number; value: number };

export type Kpis = {
	change24h: Delta | null;
	change7d: Delta | null;
	topGainer: Mover | null;
	topLoser: Mover | null;
	typeAllocation: { type: string; value: number; share: number }[];
};

const TOP_SLOTS = 7;

export function trimHoldings(holdings: api.AssetHoldingDto[]): api.AssetHoldingDto[] {
	const sorted = [...holdings].sort((a, b) => (b.totalValue ?? 0) - (a.totalValue ?? 0));
	if (sorted.length <= TOP_SLOTS) {
		return sorted;
	}

	const top = sorted.slice(0, TOP_SLOTS);
	const otherValue = sorted.slice(TOP_SLOTS).reduce((acc, curr) => acc + (curr.totalValue ?? 0), 0);
	return top.concat({
		asset: { symbol: OTHER_SYMBOL, assetType: "Crypto" },
		totalValue: otherValue,
		price: 0,
		totalAmount: 0,
		integrationValues: []
	});
}

function deltaBetween(current: number, baseline: number): Delta {
	const value = current - baseline;
	return { value, pct: baseline !== 0 ? value / baseline : null };
}

function computeKpis(
	measurings: Measurings,
	days: string[],
	totals: number[],
	latest: api.AssetHoldingDto[]
): Kpis {
	const current = totals.at(-1) ?? 0;
	const change24h = totals.length >= 2 ? deltaBetween(current, totals.at(-2)!) : null;
	const change7d = totals.length >= 2 ? deltaBetween(current, totals.at(-8) ?? totals[0]) : null;

	// 24h movers: price change only — amount changes are deposits/withdrawals,
	// not performance, and must not surface as gains/losses
	let topGainer: Mover | null = null;
	let topLoser: Mover | null = null;
	if (days.length >= 2) {
		const previous = measurings[days.at(-2)!];
		const movers: Mover[] = [];
		for (const m of latest) {
			const symbol = m.asset.symbol ?? "";
			const prevPrice = previous.find((p) => p.asset.symbol === symbol)?.price ?? 0;
			const price = m.price ?? 0;
			if (prevPrice <= 0 || price <= 0) continue;
			const value = price - prevPrice;
			movers.push({ symbol, pct: value / prevPrice, value });
		}
		movers.sort((a, b) => b.pct - a.pct);
		topGainer = movers.at(0) ?? null;
		const loser = movers.at(-1) ?? null;
		topLoser = movers.length >= 2 && loser?.symbol !== topGainer?.symbol ? loser : null;
	}

	const byType = new Map<string, number>();
	for (const m of latest) {
		const type = m.asset.assetType ?? "Crypto";
		byType.set(type, (byType.get(type) ?? 0) + (m.totalValue ?? 0));
	}
	const typeAllocation = [...byType]
		.map(([type, value]) => ({ type, value, share: current !== 0 ? value / current : 0 }))
		.sort((a, b) => b.value - a.value);

	return { change24h, change7d, topGainer, topLoser, typeAllocation };
}

export function analyze(measurings: Measurings) {
	const days = Object.keys(measurings).sort();
	const totals = days.map((d) => measurings[d].reduce((acc, m) => acc + (m.totalValue ?? 0), 0));
	const current = totals.at(-1) ?? 0;
	const first = totals[0] ?? 0;
	const delta = current - first;
	const deltaPct = first !== 0 ? delta / first : null;

	const latest = days.length ? measurings[days.at(-1)!] : [];
	const trimmedLatest = trimHoldings(latest);

	// Composition shares the pie's trim: same top symbols, same colors. Membership
	// is fixed from the latest day so bands never switch identity mid-chart.
	const topSymbols = trimmedLatest
		.map((x) => x.asset.symbol ?? "")
		.filter((s) => s !== OTHER_SYMBOL);
	const valueOn = (day: string, symbol: string) =>
		measurings[day].find((m) => m.asset.symbol === symbol)?.totalValue ?? 0;

	const composition = topSymbols.map((symbol) => ({
		name: symbol,
		data: days.map((d) => valueOn(d, symbol))
	}));
	// Other = day total minus the top symbols, so it also catches assets that
	// existed earlier in the range but are gone from the latest day
	const other = days.map((d, i) =>
		Math.max(0, totals[i] - topSymbols.reduce((acc, s) => acc + valueOn(d, s), 0))
	);
	if (other.some((v) => v > 0)) {
		composition.push({ name: OTHER_SYMBOL, data: other });
	}

	return {
		days,
		totals,
		current,
		delta,
		deltaPct,
		trimmedLatest,
		composition,
		kpis: computeKpis(measurings, days, totals, latest),
		empty: latest.length === 0
	};
}
