<script lang="ts">
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';

	let email = $state('');
	let password = $state('');
	let error = $state('');
	let loading = $state(false);

	async function submit(e: SubmitEvent) {
		e.preventDefault();
		error = '';
		loading = true;
		try {
			const res = await fetch('/api/auth/login', {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({ email, password })
			});
			if (!res.ok) {
				const data = await res.json().catch(() => ({}));
				error = data.message ?? 'Invalid credentials';
				return;
			}
			const data = await res.json();
			auth.login(data.token, data.refreshToken);
			goto('/');
		} catch {
			error = 'Unable to connect to server';
		} finally {
			loading = false;
		}
	}
</script>

<div class="card">
	<h1 style="font-size: 1.25rem; font-weight: 600; margin: 0 0 1.5rem;">Sign in</h1>

	{#if error}
		<div style="background: var(--danger-muted); border: 1px solid var(--danger); border-radius: var(--radius); padding: 0.75rem 1rem; margin-bottom: 1rem; font-size: 0.875rem; color: var(--danger);">
			{error}
		</div>
	{/if}

	<form onsubmit={submit} style="display: flex; flex-direction: column; gap: 1rem;">
		<div class="form-group">
			<label class="label" for="email">Email</label>
			<input
				id="email"
				class="input"
				type="email"
				bind:value={email}
				placeholder="you@example.com"
				required
				autocomplete="email"
			/>
		</div>
		<div class="form-group">
			<label class="label" for="password">Password</label>
			<input
				id="password"
				class="input"
				type="password"
				bind:value={password}
				placeholder="••••••••"
				required
				autocomplete="current-password"
			/>
		</div>
		<button class="btn btn-primary" type="submit" disabled={loading} style="width: 100%; justify-content: center; margin-top: 0.5rem;">
			{loading ? 'Signing in...' : 'Sign in'}
		</button>
	</form>

	<p style="margin: 1.25rem 0 0; text-align: center; font-size: 0.875rem; color: var(--text-muted);">
		Don't have an account?
		<a href="/register" style="color: var(--primary); text-decoration: none;">Register</a>
	</p>
</div>
