export const CHART_SLOTS = 7;
export const OTHER_SYMBOL = 'Other';
export const OTHER_COLOR = 'var(--chart-other)';

/**
 * Stable color assignment: the slot is derived from the alphabetical position
 * of the symbol, never from its value rank — so an asset keeps its color when
 * rankings change. 'Other' is a bucket, not an entity: always the fixed gray,
 * as is any overflow beyond the 7 palette slots (a 9th series never gets a
 * generated color).
 */
export function colorForSymbol(symbol: string, allSymbols: string[]): string {
	if (symbol === OTHER_SYMBOL) return OTHER_COLOR;
	const ordered = [...new Set(allSymbols)]
		.filter((s) => s !== OTHER_SYMBOL)
		.sort((a, b) => a.localeCompare(b));
	const slot = ordered.indexOf(symbol);
	if (slot < 0 || slot >= CHART_SLOTS) return OTHER_COLOR;
	return `var(--chart-${slot + 1})`;
}
