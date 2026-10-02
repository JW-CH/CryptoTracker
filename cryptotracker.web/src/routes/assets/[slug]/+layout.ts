import { runInLoad } from "#lib/api/client.js";
import * as api from "#lib/cryptotrackerApi.js";
import { error } from "@sveltejs/kit";
import type { LayoutLoad } from "./$types";

export const load: LayoutLoad = ({ fetch, params }) =>
	runInLoad(fetch, async () => {
		const res = await api.getAsset(params.slug ?? "");
		const status = res.status as number;
		if (status !== 200 || !res.data) {
			throw error(status === 404 ? 404 : 500, "Asset konnte nicht geladen werden");
		}

		return { asset: res.data as api.AssetWithPriceDto };
	});
