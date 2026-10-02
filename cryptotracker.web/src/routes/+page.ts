import { runInLoad } from "#lib/api/client.js";
import * as api from "#lib/cryptotrackerApi.js";
import type { PageLoad } from "./$types";

const RANGES = [7, 30, 90, 365];

export const load: PageLoad = ({ fetch, url }) => {
	const requested = Number(url.searchParams.get("range"));
	const range = RANGES.includes(requested) ? requested : 30;

	return runInLoad(fetch, () => ({
		range,
		ranges: RANGES,
		measurings: api.getMeasuringsByDays(range, {}).then((res) => {
			if (res.status !== 200 || !res.data) throw new Error("Could not load measurings");
			return res.data;
		})
	}));
};
