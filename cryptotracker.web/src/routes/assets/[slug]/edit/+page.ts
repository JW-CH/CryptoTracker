import { redirect } from "@sveltejs/kit";
import { resolve } from "$app/paths";
import type { PageLoad } from "./$types";

export const load: PageLoad = async ({ parent, params }) => {
	const { asset } = await parent();
	if (!asset.asset.externalId || !asset.asset.symbol) {
		throw redirect(307, resolve("/assets/[slug]", { slug: params.slug }));
	}
};
