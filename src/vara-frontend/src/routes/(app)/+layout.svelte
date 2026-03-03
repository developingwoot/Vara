<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { isAuthenticated, auth } from '$lib/stores/auth';
	import { fetchApi } from '$lib/api/client';
	import Sidebar from '$lib/components/Sidebar.svelte';
	import type { User } from '$lib/stores/auth';

	let { children } = $props();

	onMount(async () => {
		if (!$isAuthenticated) {
			goto('/login');
			return;
		}
		try {
			const user = await fetchApi<User>('/users/me');
			auth.setUser(user);
		} catch {
			// handled by fetchApi (401 → /login)
		}
	});
</script>

<div style="display: flex; height: 100vh; overflow: hidden;">
	<Sidebar />
	<main style="flex: 1; overflow-y: auto; padding: 2rem;">
		{@render children()}
	</main>
</div>
