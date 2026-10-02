import { runInLoad } from "#lib/api/client.js";
import * as api from "#lib/cryptotrackerApi.js";
import type { PageLoad } from "./$types";

export const load: PageLoad = ({ fetch }) =>
	runInLoad(fetch, () => ({
		portfolio: Promise.all([
			api.getAssets().catch(() => null),
			api.getLatestMeasurings().catch(() => null)
		]).then(([assetRes, holdingRes]) => {
			const assets = assetRes?.status === 200 && Array.isArray(assetRes.data) ? assetRes.data : [];
			const holdings =
				holdingRes?.status === 200 && Array.isArray(holdingRes.data) ? holdingRes.data : [];

			return {
				assets,
				holdingsBySymbol: Object.fromEntries(
					holdings.map((h) => [h.asset.symbol ?? "", h])
				) as Record<string, api.AssetHoldingDto>
			};
		})
	}));
