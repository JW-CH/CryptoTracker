<script lang="ts">
	import SunIcon from '@lucide/svelte/icons/sun';
	import MoonIcon from '@lucide/svelte/icons/moon';
	import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
	import { buttonVariants } from '$lib/components/ui/button';
	import { theme, type Theme } from '$lib/stores/theme.svelte';
</script>

<DropdownMenu.Root>
	<DropdownMenu.Trigger
		class={[buttonVariants({ variant: 'ghost', size: 'icon' }), 'relative']}
		aria-label="Toggle theme"
	>
		<SunIcon class="size-5 scale-100 rotate-0 transition-all dark:scale-0 dark:-rotate-90" />
		<MoonIcon
			class="absolute size-5 scale-0 rotate-90 transition-all dark:scale-100 dark:rotate-0"
		/>
	</DropdownMenu.Trigger>
	<DropdownMenu.Content align="end">
		<DropdownMenu.RadioGroup
			value={theme.value}
			onValueChange={(value) => (theme.value = value as Theme)}
		>
			<DropdownMenu.RadioItem value="light">Light</DropdownMenu.RadioItem>
			<DropdownMenu.RadioItem value="dark">Dark</DropdownMenu.RadioItem>
			<DropdownMenu.RadioItem value="system">System</DropdownMenu.RadioItem>
		</DropdownMenu.RadioGroup>
	</DropdownMenu.Content>
</DropdownMenu.Root>
