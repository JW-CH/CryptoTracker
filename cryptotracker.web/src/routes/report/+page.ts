import * as api from "$lib/cryptotrackerApi";
import type { PageLoad } from "./$types";

function todayIso(): string {
	return new Date().toISOString().split("T")[0];
}

export const load: PageLoad = ({ url }) => {
	const requested = url.searchParams.get("date");
	const date = requested && /^\d{4}-\d{2}-\d{2}$/.test(requested) ? requested : todayIso();

	return {
		date,
		holdings: api
			.getMeasuringsByDate(new Date(date).toISOString())
			.then((res) => (res.status === 200 && Array.isArray(res.data) ? res.data : []))
			.catch(() => [] as api.AssetHoldingDto[])
	};
};
