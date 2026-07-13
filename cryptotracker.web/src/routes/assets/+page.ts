import * as api from '$lib/cryptotrackerApi';
import type { PageLoad } from './$types';

export const load: PageLoad = () => {
	return { assets: api.getAssets().then((res) => res.data ?? []) };
};