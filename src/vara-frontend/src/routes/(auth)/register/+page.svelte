<script lang="ts">
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';

	let fullName = $state('');
	let email = $state('');
	let password = $state('');
	let error = $state('');
	let emailTaken = $state(false);
	let loading = $state(false);

	async function submit(e: SubmitEvent) {
		e.preventDefault();
		error = '';
		emailTaken = false;
		loading = true;
		try {
			const res = await fetch('/api/auth/register', {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({ fullName, email, password })
			});
			if (!res.ok) {
				const data = await res.json().catch(() => ({}));
				if (res.status === 409) {
					emailTaken = true;
				} else {
					error = data.message ?? data.error ?? 'Registration failed';
				}
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
	<h1 style="font-size: 1.25rem; font-weight: 600; margin: 0 0 1.5rem;">Create account</h1>

	{#if emailTaken}
		<div style="background: var(--warning-muted); border: 1px solid var(--warning); border-radius: var(--radius); padding: 0.75rem 1rem; margin-bottom: 1rem; font-size: 0.875rem; color: var(--warning);">
			An account with that email already exists.
			<a href="/login" style="color: var(--warning); font-weight: 500; text-decoration: underline;">Sign in instead</a>
			or
			<a href="/login" style="color: var(--warning); font-weight: 500; text-decoration: underline;">reset your password</a>.
		</div>
	{/if}

	{#if error}
		<div style="background: var(--danger-muted); border: 1px solid var(--danger); border-radius: var(--radius); padding: 0.75rem 1rem; margin-bottom: 1rem; font-size: 0.875rem; color: var(--danger);">
			{error}
		</div>
	{/if}

	<form onsubmit={submit} style="display: flex; flex-direction: column; gap: 1rem;">
		<div class="form-group">
			<label class="label" for="fullName">Full name</label>
			<input
				id="fullName"
				class="input"
				type="text"
				bind:value={fullName}
				placeholder="Jane Smith"
				autocomplete="name"
			/>
		</div>
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
				minlength={8}
				autocomplete="new-password"
			/>
			<span class="helper-text">At least 8 characters</span>
		</div>
		<button class="btn btn-primary" type="submit" disabled={loading} style="width: 100%; justify-content: center; margin-top: 0.5rem;">
			{loading ? 'Creating account...' : 'Create account'}
		</button>
	</form>

	<p style="margin: 1.25rem 0 0; text-align: center; font-size: 0.875rem; color: var(--text-muted);">
		Already have an account?
		<a href="/login" style="color: var(--primary); text-decoration: none;">Sign in</a>
	</p>
</div>
