<script lang="ts">
	import { fetchApi } from '$lib/api/client';
	import { auth } from '$lib/stores/auth';
	import type { User } from '$lib/stores/auth';
	import { onMount } from 'svelte';

	let fullName = $state('');
	let email = $state('');
	let profileSaving = $state(false);
	let profileSuccess = $state(false);
	let profileError = $state('');

	let currentPassword = $state('');
	let newPassword = $state('');
	let passwordSaving = $state(false);
	let passwordSuccess = $state(false);
	let passwordError = $state('');

	let ytConnected = $state(false);
	let disconnecting = $state(false);

	async function disconnectYt() {
		disconnecting = true;
		try {
			await fetchApi('/youtube/oauth/disconnect', { method: 'DELETE' });
			ytConnected = false;
		} catch { /* ignore */ } finally {
			disconnecting = false;
		}
	}

	onMount(async () => {
		try {
			const [user, ytStatus] = await Promise.all([
				fetchApi<User>('/users/me'),
				fetchApi<{ connected: boolean }>('/youtube/oauth/status').catch(() => ({ connected: false }))
			]);
			auth.setUser(user);
			fullName = user.fullName ?? '';
			email = user.email;
			ytConnected = ytStatus.connected;
		} catch { /* handled by fetchApi */ }
	});

	async function saveProfile(e: SubmitEvent) {
		e.preventDefault();
		profileSaving = true;
		profileSuccess = false;
		profileError = '';
		try {
			const updated = await fetchApi<User>('/users/me', {
				method: 'PATCH',
				body: JSON.stringify({ fullName: fullName || null, email: email || null })
			});
			auth.setUser(updated);
			profileSuccess = true;
			setTimeout(() => (profileSuccess = false), 3000);
		} catch (err: unknown) {
			profileError = err instanceof Error ? err.message : 'Failed to save';
		} finally {
			profileSaving = false;
		}
	}

	async function changePassword(e: SubmitEvent) {
		e.preventDefault();
		passwordSaving = true;
		passwordSuccess = false;
		passwordError = '';
		try {
			await fetchApi('/users/me/change-password', {
				method: 'POST',
				body: JSON.stringify({ currentPassword, newPassword })
			});
			passwordSuccess = true;
			currentPassword = '';
			newPassword = '';
			setTimeout(() => (passwordSuccess = false), 3000);
		} catch (err: unknown) {
			passwordError = err instanceof Error ? err.message : 'Failed to change password';
		} finally {
			passwordSaving = false;
		}
	}
</script>

<!-- Profile section -->
<div class="card" style="margin-bottom: 1.5rem;">
	<h2 style="font-size: 0.9375rem; font-weight: 600; margin: 0 0 1.25rem;">Profile</h2>

	{#if profileSuccess}
		<div style="background: var(--success-muted); border: 1px solid var(--success); border-radius: var(--radius); padding: 0.75rem 1rem; margin-bottom: 1rem; font-size: 0.875rem; color: var(--success);">
			Profile saved.
		</div>
	{/if}
	{#if profileError}
		<div style="background: var(--danger-muted); border: 1px solid var(--danger); border-radius: var(--radius); padding: 0.75rem 1rem; margin-bottom: 1rem; font-size: 0.875rem; color: var(--danger);">
			{profileError}
		</div>
	{/if}

	<form onsubmit={saveProfile} style="display: flex; flex-direction: column; gap: 1rem;">
		<div class="form-group">
			<label class="label" for="fullName">Full name</label>
			<input id="fullName" class="input" type="text" bind:value={fullName} placeholder="Jane Smith" autocomplete="name" />
		</div>
		<div class="form-group">
			<label class="label" for="email">Email</label>
			<input id="email" class="input" type="email" bind:value={email} placeholder="you@example.com" required autocomplete="email" />
		</div>
		<div style="display: flex; justify-content: flex-end;">
			<button class="btn btn-primary" type="submit" disabled={profileSaving}>
				{profileSaving ? 'Saving...' : 'Save changes'}
			</button>
		</div>
	</form>
</div>

<!-- YouTube Analytics section -->
<div class="card" style="margin-bottom: 1.5rem;">
	<h2 style="font-size: 0.9375rem; font-weight: 600; margin: 0 0 0.5rem;">YouTube Analytics</h2>
	<p style="font-size: 0.875rem; color: var(--text-muted); margin: 0 0 1rem;">Connect your Google account to unlock CTR, avg watch time, and view percentage in your channel audits.</p>
	{#if ytConnected}
		<div style="display: flex; align-items: center; gap: 1rem; flex-wrap: wrap;">
			<span style="font-size: 0.875rem; color: var(--success);">✓ Connected</span>
			<button class="btn btn-ghost" style="font-size: 0.8125rem;" onclick={disconnectYt} disabled={disconnecting}>
				{disconnecting ? 'Disconnecting...' : 'Disconnect'}
			</button>
		</div>
	{:else}
		<a href="/api/youtube/oauth/connect" class="btn btn-ghost" style="font-size: 0.875rem; display: inline-block;">
			Connect YouTube Account →
		</a>
	{/if}
</div>

<!-- Password section -->
<div class="card">
	<h2 style="font-size: 0.9375rem; font-weight: 600; margin: 0 0 1.25rem;">Change Password</h2>

	{#if passwordSuccess}
		<div style="background: var(--success-muted); border: 1px solid var(--success); border-radius: var(--radius); padding: 0.75rem 1rem; margin-bottom: 1rem; font-size: 0.875rem; color: var(--success);">
			Password changed successfully.
		</div>
	{/if}
	{#if passwordError}
		<div style="background: var(--danger-muted); border: 1px solid var(--danger); border-radius: var(--radius); padding: 0.75rem 1rem; margin-bottom: 1rem; font-size: 0.875rem; color: var(--danger);">
			{passwordError}
		</div>
	{/if}

	<form onsubmit={changePassword} style="display: flex; flex-direction: column; gap: 1rem;">
		<div class="form-group">
			<label class="label" for="currentPassword">Current password</label>
			<input id="currentPassword" class="input" type="password" bind:value={currentPassword} required autocomplete="current-password" />
		</div>
		<div class="form-group">
			<label class="label" for="newPassword">New password</label>
			<input id="newPassword" class="input" type="password" bind:value={newPassword} required minlength={8} autocomplete="new-password" />
			<span class="helper-text">At least 8 characters</span>
		</div>
		<div style="display: flex; justify-content: flex-end;">
			<button class="btn btn-primary" type="submit" disabled={passwordSaving}>
				{passwordSaving ? 'Saving...' : 'Change password'}
			</button>
		</div>
	</form>
</div>
