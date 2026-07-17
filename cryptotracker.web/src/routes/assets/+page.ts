import * as api from "$lib/cryptotrackerApi";
import type { PageLoad } from "./$types";

export const load: PageLoad = () => {
	const assets = api
		.getAssets()
		.then((res) => (res.status === 200 && Array.isArray(res.data) ? res.data : []))
		.catch(() => [] as api.AssetDto[]);

	const holdings = api
		.getLatestMeasurings()
		.then((res) => (res.status === 200 && Array.isArray(res.data) ? res.data : []))
		.catch(() => [] as api.AssetHoldingDto[]);

	return {
		portfolio: Promise.all([assets, holdings]).then(([assetList, holdingList]) => ({
			assets: assetList,
			holdingsBySymbol: Object.fromEntries(
				holdingList.map((h) => [h.asset.symbol ?? "", h])
			) as Record<string, api.AssetHoldingDto>
		}))
	};
};
