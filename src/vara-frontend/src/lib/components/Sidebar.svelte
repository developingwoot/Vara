<script lang="ts">
	import { page } from '$app/stores';
	import { goto } from '$app/navigation';
	import { auth, isAdmin } from '$lib/stores/auth';
	import Badge from './Badge.svelte';

	function logout() {
		auth.logout();
		goto('/login');
	}

	const navItems = [
		{ href: '/', label: 'Dashboard', icon: '⊞' },
		{ href: '/analyze/keyword', label: 'Keyword Analysis', icon: '🔍' },
		{ href: '/analyze/trends', label: 'Trends', icon: '📈' },
		{ href: '/analyze/video', label: 'Video Analysis', icon: '🎬' },
		{ href: '/analyze/niche', label: 'Niche Compare', icon: '⚖️' },
		{ href: '/plugins', label: 'Plugins', icon: '🧩' },
		{ href: '/settings', label: 'Settings', icon: '⚙️' }
	];

	function isActive(href: string) {
		if (href === '/') return $page.url.pathname === '/';
		return $page.url.pathname.startsWith(href);
	}
</script>

<aside
	style="width: 240px; min-width: 240px; background: var(--surface); border-right: 1px solid var(--border); display: flex; flex-direction: column; height: 100vh; overflow-y: auto;"
>
	<!-- Logo -->
	<div style="padding: 1.5rem 1rem 1rem; border-bottom: 1px solid var(--border-subtle);">
		<div style="display: flex; align-items: center; gap: 0.5rem;">
			<span style="font-size: 1.25rem;">▲</span>
			<span style="font-size: 1.125rem; font-weight: 600; letter-spacing: -0.02em;">VARA</span>
		</div>
	</div>

	<!-- Nav -->
	<nav style="flex: 1; padding: 0.75rem 0.5rem; display: flex; flex-direction: column; gap: 0.25rem;">
		{#each navItems as item}
			<a
				href={item.href}
				class="nav-link"
				class:active={isActive(item.href)}
			>
				<span style="font-size: 1rem; width: 1.25rem; text-align: center;">{item.icon}</span>
				{item.label}
			</a>
		{/each}

		{#if $isAdmin}
			<div style="margin-top: 0.5rem; padding-top: 0.5rem; border-top: 1px solid var(--border-subtle);">
				<a
					href="/admin/niches"
					class="nav-link"
					class:active={isActive('/admin')}
				>
					<span style="font-size: 1rem; width: 1.25rem; text-align: center;">🛠</span>
					Admin
				</a>
			</div>
		{/if}
	</nav>

	<!-- User footer -->
	<div style="padding: 1rem; border-top: 1px solid var(--border-subtle);">
		{#if $auth.user}
			<div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 0.75rem;">
				<div>
					<div style="font-size: 0.8125rem; font-weight: 500; color: var(--text); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; max-width: 130px;">
						{$auth.user.fullName || $auth.user.email}
					</div>
					<div style="margin-top: 0.25rem;">
						<Badge variant={$auth.user.subscriptionTier === 'creator' ? 'creator' : 'free'}>
							{$auth.user.subscriptionTier === 'creator' ? 'Creator' : 'Free'}
						</Badge>
					</div>
					{#if $auth.user.isAdmin}
					<div style="margin-top: 0.25rem;">
						<Badge>
							Admin
						</Badge>
					</div>
					{/if}
				</div>
			</div>
		{/if}
		<button class="btn btn-ghost" style="width: 100%; justify-content: center;" onclick={logout}>
			Sign out
		</button>
	</div>
</aside>
