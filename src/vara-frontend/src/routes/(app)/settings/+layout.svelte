<script lang="ts">
	import { page } from '$app/stores';

	const tabs = [
		{ href: '/settings/account', label: 'Account' },
		{ href: '/settings/channels', label: 'Channels' }
	];

	let { children } = $props();
</script>

<div class="page-header">
	<h1 class="page-title">Settings</h1>
</div>

<div style="display: flex; gap: 1.5rem; align-items: flex-start;">
	<!-- Left nav tabs -->
	<nav style="width: 180px; flex-shrink: 0; display: flex; flex-direction: column; gap: 0.25rem;">
		{#each tabs as tab}
			<a
				href={tab.href}
				class="nav-link"
				class:active={$page.url.pathname === tab.href}
			>
				{tab.label}
			</a>
		{/each}
	</nav>

	<!-- Tab content -->
	<div style="flex: 1; min-width: 0;">
		{@render children()}
	</div>
</div>
