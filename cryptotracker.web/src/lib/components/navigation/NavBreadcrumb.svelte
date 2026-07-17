<script lang="ts">
	import * as Breadcrumb from "$lib/components/ui/breadcrumb/index.js";
	import { page } from "$app/state";

	const segments = $derived(page.url.pathname.toLowerCase().split("/").filter(Boolean));
	const crumbs = $derived([
		{ label: "Home", href: "/" },
		...segments.map((segment, i) => ({
			label: segment,
			href: "/" + segments.slice(0, i + 1).join("/")
		}))
	]);
</script>

<!-- On the top-level pages the breadcrumb adds nothing — only show from depth 2 -->
{#if segments.length >= 2}
	<Breadcrumb.Root>
		<Breadcrumb.List>
			{#each crumbs as crumb, index (crumb.href)}
				{#if index > 0}
					<Breadcrumb.Separator />
				{/if}
				<Breadcrumb.Item>
					{#if index === crumbs.length - 1}
						<Breadcrumb.Page>{crumb.label}</Breadcrumb.Page>
					{:else}
						<Breadcrumb.Link href={crumb.href}>{crumb.label}</Breadcrumb.Link>
					{/if}
				</Breadcrumb.Item>
			{/each}
		</Breadcrumb.List>
	</Breadcrumb.Root>
{/if}
