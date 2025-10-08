<script lang="ts">
	import * as Breadcrumb from '$lib/components/ui/breadcrumb/index.js';
	import { page } from '$app/state';

	let list = page.url.pathname == '/' ? [''] : page.url.pathname.toLowerCase().split('/');

	function getUrl(index: number) {
		let url = ['/'];
		for (let i = 1; i <= index; i++) {
			url.push(list[i] + '/');
		}
		return url.join('');
	}
</script>

<Breadcrumb.Root class="my-4">
	<Breadcrumb.List>
		{#each list as item, index}
			{#if index == 0}
				<Breadcrumb.Item>
					<Breadcrumb.Link asChild let:attrs>
						<a href={getUrl(index)}>home</a>
					</Breadcrumb.Link>
				</Breadcrumb.Item>
			{:else}
				<Breadcrumb.Separator />
				<Breadcrumb.Item>
					<Breadcrumb.Link asChild let:attrs>
						<a href={getUrl(index)}>{item}</a>
					</Breadcrumb.Link>
				</Breadcrumb.Item>
			{/if}
		{/each}
	</Breadcrumb.List>
</Breadcrumb.Root>
