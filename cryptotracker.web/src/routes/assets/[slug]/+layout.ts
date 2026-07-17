import * as api from "$lib/cryptotrackerApi";
import { error } from "@sveltejs/kit";
import type { LayoutLoad } from "./$types";

export const load: LayoutLoad = async ({ params }) => {
	const res = await api.getAsset(params.slug ?? "");

	const status = res.status as number;
	if (status !== 200 || !res.data) {
		throw error(status === 404 ? 404 : 500, "Asset konnte nicht geladen werden");
	}

	return { asset: res.data as api.AssetWithPriceDto };
};
