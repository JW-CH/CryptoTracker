// Language (English UI) and number/date formatting are independent decisions.
// Fixed to de-CH on purpose: navigator.language reports en-US for most untuned
// browsers, which flips dates to mm/dd/yyyy. Swap for a user setting if one
// ever exists.
export const LOCALE = "de-CH";

const currencyFormatters = new Map<string, Intl.NumberFormat>();

export function formatCurrency(value: number, currency: string): string {
	let fmt = currencyFormatters.get(currency);
	if (!fmt) {
		try {
			fmt = new Intl.NumberFormat(LOCALE, { style: "currency", currency });
		} catch {
			// Not a valid ISO 4217 code — fall back to a plain number with suffix
			fmt = new Intl.NumberFormat(LOCALE, {
				minimumFractionDigits: 2,
				maximumFractionDigits: 2
			});
		}
		currencyFormatters.set(currency, fmt);
	}
	if (fmt.resolvedOptions().style !== "currency") return `${fmt.format(value)} ${currency}`;
	return fmt.format(value);
}

export function formatAmount(value: number, assetType?: string | null): string {
	const maximumFractionDigits = assetType === "Crypto" ? 8 : 2;
	return new Intl.NumberFormat(LOCALE, {
		minimumFractionDigits: 2,
		maximumFractionDigits
	}).format(value);
}

/** Formats a fraction (0.032 → "+3.2%"), signed — for deltas. */
export function formatPercent(value: number): string {
	return new Intl.NumberFormat(LOCALE, {
		style: "percent",
		signDisplay: "always",
		maximumFractionDigits: 2
	}).format(value);
}

/** Unsigned share (0.32 → "32%") — for allocation columns. */
export function formatShare(value: number): string {
	return new Intl.NumberFormat(LOCALE, {
		style: "percent",
		maximumFractionDigits: 1
	}).format(value);
}

/** "2 hours ago" / "yesterday" — for sync timestamps. */
export function formatRelativeTime(date: Date | string): string {
	const diffSec = Math.round((new Date(date).getTime() - Date.now()) / 1000);
	const abs = Math.abs(diffSec);
	const rtf = new Intl.RelativeTimeFormat("en", { numeric: "auto" });
	if (abs < 60) return rtf.format(diffSec, "second");
	if (abs < 3600) return rtf.format(Math.round(diffSec / 60), "minute");
	if (abs < 86400) return rtf.format(Math.round(diffSec / 3600), "hour");
	return rtf.format(Math.round(diffSec / 86400), "day");
}

export function formatDate(date: Date | string): string {
	return new Date(date).toLocaleDateString(LOCALE, {
		day: "2-digit",
		month: "2-digit",
		year: "numeric"
	});
}
