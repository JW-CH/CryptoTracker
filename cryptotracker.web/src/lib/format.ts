// Language (English UI) and number/date formatting are independent decisions.
// Fixed to de-CH on purpose: navigator.language reports en-US for most untuned
// browsers, which flips dates to mm/dd/yyyy. Swap for a user setting if one
// ever exists.
export const LOCALE = 'de-CH';

const currencyFormatters = new Map<string, Intl.NumberFormat>();

export function formatCurrency(value: number, currency: string): string {
	let fmt = currencyFormatters.get(currency);
	if (!fmt) {
		try {
			fmt = new Intl.NumberFormat(LOCALE, { style: 'currency', currency });
		} catch {
			// Not a valid ISO 4217 code — fall back to a plain number with suffix
			fmt = new Intl.NumberFormat(LOCALE, {
				minimumFractionDigits: 2,
				maximumFractionDigits: 2
			});
		}
		currencyFormatters.set(currency, fmt);
	}
	if (fmt.resolvedOptions().style !== 'currency') return `${fmt.format(value)} ${currency}`;
	return fmt.format(value);
}

export function formatAmount(value: number, assetType?: string | null): string {
	const maximumFractionDigits = assetType === 'Crypto' ? 8 : 2;
	return new Intl.NumberFormat(LOCALE, {
		minimumFractionDigits: 2,
		maximumFractionDigits
	}).format(value);
}

/** Formats a fraction (0.032 → "+3.2%"), signed — for deltas. */
export function formatPercent(value: number): string {
	return new Intl.NumberFormat(LOCALE, {
		style: 'percent',
		signDisplay: 'always',
		maximumFractionDigits: 2
	}).format(value);
}

export function formatDate(date: Date | string): string {
	return new Date(date).toLocaleDateString(LOCALE, {
		day: '2-digit',
		month: '2-digit',
		year: 'numeric'
	});
}
