<script lang="ts">
	export let text: string;
	export let path: string | undefined = undefined;
</script>

{#if path === ''}
	<!-- svelte-ignore a11y_missing_attribute -->
	<a class="text-gray-800 hover:text-blue-400">{text}</a>
{:else}
	<a class="text-gray-800 hover:text-blue-400" href={path}>{text}</a>
{/if}
