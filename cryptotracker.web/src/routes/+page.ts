import * as api from '$lib/cryptotrackerApi';
import type { PageLoad } from './$types';

const RANGES = [7, 30, 90, 365];

export const load: PageLoad = ({ url }) => {
	const requested = Number(url.searchParams.get('range'));
	const range = RANGES.includes(requested) ? requested : 30;
	return {
		range,
		ranges: RANGES,
		// One request for the whole dashboard: the portfolio standing per day is
		// just the sum of the measurings, no separate endpoint needed.
		measurings: api
			.getMeasuringsByDays(range)
			.then((res) => (res.status === 200 && res.data ? res.data : {}))
			.catch(() => ({}) as { [key: string]: api.AssetHoldingDto[] })
	};
};
