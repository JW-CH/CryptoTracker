import * as api from "$lib/cryptotrackerApi";
import type { PageLoad } from "./$types";

export const load: PageLoad = () => {
	return {
		integrations: api
			.getIntegrations()
			.then((res) => (res.status === 200 && res.data ? res.data : []))
			.catch(() => [] as api.IntegrationDto[])
	};
};
