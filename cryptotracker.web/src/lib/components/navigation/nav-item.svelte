<script lang="ts">
	export let text: string;
	export let path: string = '';
</script>

{#if path === ''}
	<span class="cursor-default text-gray-800">{text}</span>
{:else}
	<a class="text-gray-800 hover:text-blue-400" href={path}>{text}</a>
{/if}
