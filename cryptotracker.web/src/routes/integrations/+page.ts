import { runInLoad } from "#lib/api/client.js";
import * as api from "#lib/cryptotrackerApi.js";
import type { PageLoad } from "./$types";

export const load: PageLoad = ({ fetch }) =>
	runInLoad(fetch, () => ({
		integrations: api.getIntegrations().then((res) => {
			if (res.status !== 200 || !res.data) throw new Error("Could not load integrations");
			return res.data;
		})
	}));
