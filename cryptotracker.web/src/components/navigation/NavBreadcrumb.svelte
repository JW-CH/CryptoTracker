<script lang="ts">
	import * as Breadcrumb from '$lib/components/ui/breadcrumb/index.js';
	import { page } from '$app/state';

	let list = page.url.pathname == '/' ? [''] : page.url.pathname.toLowerCase().split('/');

	let url = ['/'];

	function addItemToUrl(item: string) {
		url.push(item + '/');
	}
</script>

<Breadcrumb.Root class="my-4">
	<Breadcrumb.List>
		{#each list as item}
			{#if item == ''}
				<Breadcrumb.Item>
					<Breadcrumb.Link asChild let:attrs>
						<a href={url.join('')}>home</a>
					</Breadcrumb.Link>
				</Breadcrumb.Item>
			{:else}
				{addItemToUrl(item)}
				<Breadcrumb.Separator />
				<Breadcrumb.Item>
					<Breadcrumb.Link asChild let:attrs>
						<a href={url.join('')}>{item}</a>
					</Breadcrumb.Link>
				</Breadcrumb.Item>
			{/if}
		{/each}
	</Breadcrumb.List>
</Breadcrumb.Root>
