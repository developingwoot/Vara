<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { fetchApi } from '$lib/api/client';
	import EmptyState from '$lib/components/EmptyState.svelte';
	import Badge from '$lib/components/Badge.svelte';
	import ErrorAlert from '$lib/components/ErrorAlert.svelte';

	interface Channel {
		id: string;
		displayName: string;
		handle: string;
		subscriberCount: number;
		thumbnailUrl?: string;
		isOwner: boolean;
		nicheRaw?: string;
		nicheName?: string;
		lastSyncedAt?: string;
		videoCount?: number;
	}

	interface NicheMatch {
		id: number;
		name: string;
		slug: string;
	}

	let channels: Channel[] = $state([]);
	let loading = $state(true);
	let error = $state('');
	let ytConnected = $state(false);
	let ytConnectSuccess = $state(false);
	let disconnecting = $state(false);
	let adding = $state(false);
	let addError = $state('');
	let confirmDeleteId: string | null = $state(null);
	let syncingId: string | null = $state(null);
	let showAddForm = $state(false);
	let failedThumbnails = $state(new Set<string>());

	let youtubeUrl = $state('');
	let isOwner = $state(true);
	let nicheInput = $state('');
	let nicheMatch: NicheMatch | null = $state(null);
	let nicheResolving = $state(false);
	let nicheSuggestions: { name: string; slug: string; confidence: number }[] = $state([]);
	let nicheTimer: ReturnType<typeof setTimeout>;

	async function loadChannels() {
		try {
			channels = await fetchApi<Channel[]>('/channels');
		} catch (e: any) {
			error = e.message ?? 'Failed to load channels';
		} finally {
			loading = false;
		}
	}

	async function syncChannel(id: string) {
		syncingId = id;
		try {
			await fetchApi(`/channels/${id}/sync`, { method: 'POST' });
			await loadChannels();
		} catch (e: any) {
			alert(e.message ?? 'Sync failed');
		} finally {
			syncingId = null;
		}
	}

	async function deleteChannel(id: string) {
		try {
			await fetchApi(`/channels/${id}`, { method: 'DELETE' });
			channels = channels.filter((c) => c.id !== id);
			confirmDeleteId = null;
		} catch (e: any) {
			alert(e.message ?? 'Failed to remove channel');
		}
	}

	async function resolveNiche(raw: string) {
		if (!raw.trim()) { nicheMatch = null; nicheSuggestions = []; return; }
		nicheResolving = true;
		nicheMatch = null;
		nicheSuggestions = [];
		try {
			const res = await fetchApi<any>('/niches/resolve', {
				method: 'POST',
				body: JSON.stringify({ niche: raw.trim() })
			});
			if (res.matched) {
				nicheMatch = res.niche;
				nicheSuggestions = [];
			} else {
				nicheSuggestions = res.suggestions ?? [];
			}
		} catch { /* non-blocking */ } finally {
			nicheResolving = false;
		}
	}

	function onNicheInput() {
		clearTimeout(nicheTimer);
		nicheMatch = null;
		nicheSuggestions = [];
		if (nicheInput.trim().length >= 2)
			nicheTimer = setTimeout(() => resolveNiche(nicheInput), 400);
	}

	async function addChannel(e: SubmitEvent) {
		e.preventDefault();
		adding = true;
		addError = '';
		try {
			await fetchApi('/channels', {
				method: 'POST',
				body: JSON.stringify({
					handleOrUrl: youtubeUrl.trim(),
					isOwner,
					niche: nicheInput.trim() || null
				})
			});
			youtubeUrl = '';
			nicheInput = '';
			nicheMatch = null;
			nicheSuggestions = [];
			showAddForm = false;
			await loadChannels();
		} catch (err: any) {
			addError = err.message ?? 'Failed to add channel';
		} finally {
			adding = false;
		}
	}

	function formatSubs(n: number) {
		if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
		if (n >= 1_000) return `${(n / 1_000).toFixed(0)}K`;
		return n.toString();
	}

	function timeSince(dateStr?: string) {
		if (!dateStr) return null;
		const diff = Date.now() - new Date(dateStr).getTime();
		const days = Math.floor(diff / 86_400_000);
		if (days === 0) return 'Today';
		if (days === 1) return 'Yesterday';
		if (days < 30) return `${days}d ago`;
		const months = Math.floor(days / 30);
		return `${months}mo ago`;
	}

	async function checkYtStatus() {
		try {
			const res = await fetchApi<{ connected: boolean }>('/youtube/oauth/status');
			ytConnected = res.connected;
		} catch {
			// non-blocking
		}
	}

	async function disconnectYt() {
		disconnecting = true;
		try {
			await fetchApi('/youtube/oauth/disconnect', { method: 'DELETE' });
			ytConnected = false;
		} catch { /* ignore */ } finally {
			disconnecting = false;
		}
	}

	async function connectYt() {
		const { url } = await fetchApi<{ url: string }>('/youtube/oauth/connect');
		window.location.href = url;
	}

	const ownedChannels = $derived(channels.filter((c) => c.isOwner));
	const trackedChannels = $derived(channels.filter((c) => !c.isOwner));

	onMount(async () => {
		// Check if returning from successful OAuth flow
		const params = new URLSearchParams($page.url.search);
		if (params.get('yt_connected') === 'true') {
			ytConnectSuccess = true;
			ytConnected = true;
			// Clean up the URL
			history.replaceState({}, '', '/channels');
		}
		await Promise.all([loadChannels(), checkYtStatus()]);
	});
</script>

<div class="page-header" style="display: flex; align-items: flex-start; justify-content: space-between; flex-wrap: wrap; gap: 1rem;">
	<div>
		<h1 class="page-title">Channels</h1>
		<p class="page-subtitle">Your channels and the competitors you're watching</p>
	</div>
	<button class="btn btn-primary" onclick={() => (showAddForm = !showAddForm)}>
		{showAddForm ? 'Cancel' : '+ Add Channel'}
	</button>
</div>

{#if error}
	<ErrorAlert message={error} onRetry={loadChannels} />
{/if}

<!-- YouTube Analytics banner -->
{#if ytConnectSuccess}
	<div style="background: #dcfce7; border: 1px solid #86efac; border-radius: var(--radius); padding: 0.875rem 1.25rem; margin-bottom: 1rem; font-size: 0.875rem; color: #166534; display: flex; align-items: center; gap: 0.75rem;">
		<span>✓</span>
		<span>YouTube Analytics connected! Your next audit will include CTR, watch time, and view percentage data.</span>
	</div>
{/if}

{#if !ytConnected}
	<div style="background: var(--surface-2); border: 1px solid var(--border); border-radius: var(--radius); padding: 0.875rem 1.25rem; margin-bottom: 1rem; display: flex; align-items: center; justify-content: space-between; gap: 1rem; flex-wrap: wrap;">
		<div>
			<div style="font-size: 0.875rem; font-weight: 600; margin-bottom: 0.2rem;">Unlock richer metrics</div>
			<div style="font-size: 0.8125rem; color: var(--text-muted);">Connect your YouTube account to add CTR, avg watch time, and view percentage to your channel audits.</div>
		</div>
		<button class="btn btn-ghost" style="font-size: 0.8125rem; white-space: nowrap; flex-shrink: 0;" onclick={connectYt}>
			Connect YouTube →
		</button>
	</div>
{:else}
	<div style="background: var(--surface-2); border: 1px solid var(--border); border-radius: var(--radius); padding: 0.875rem 1.25rem; margin-bottom: 1rem; display: flex; align-items: center; justify-content: space-between; gap: 1rem;">
		<div style="font-size: 0.875rem; display: flex; align-items: center; gap: 0.5rem;">
			<span style="color: var(--success);">✓</span>
			<span>YouTube Analytics connected</span>
		</div>
		<button class="btn btn-ghost" style="font-size: 0.75rem; color: var(--text-muted);" onclick={disconnectYt} disabled={disconnecting}>
			{disconnecting ? 'Disconnecting...' : 'Disconnect'}
		</button>
	</div>
{/if}

<!-- Add channel form -->
{#if showAddForm}
	<div class="card" style="margin-bottom: 1.5rem;">
		<h2 style="font-size: 0.9375rem; font-weight: 600; margin: 0 0 1rem;">Add a Channel</h2>

		{#if addError}
			<div style="background: var(--danger-muted); border: 1px solid var(--danger); border-radius: var(--radius); padding: 0.75rem 1rem; margin-bottom: 1rem; font-size: 0.875rem; color: var(--danger);">
				{addError}
			</div>
		{/if}

		<form onsubmit={addChannel} style="display: flex; flex-direction: column; gap: 1rem;">
			<div style="display: flex; gap: 1rem; flex-wrap: wrap;">
				<div class="form-group" style="flex: 2; min-width: 200px;">
					<label class="label" for="youtubeUrl">YouTube URL <span style="color: var(--danger);">*</span></label>
					<input
						id="youtubeUrl"
						class="input"
						type="url"
						bind:value={youtubeUrl}
						placeholder="https://youtube.com/@channelname"
						required
					/>
				</div>
				<div class="form-group" style="flex: 1; min-width: 160px;">
					<label class="label" for="nicheInput">Niche <span style="color: var(--text-muted); font-weight: 400;">(optional)</span></label>
					<input
						id="nicheInput"
						class="input"
						type="text"
						bind:value={nicheInput}
						oninput={onNicheInput}
						placeholder="e.g. personal finance"
					/>
					{#if nicheResolving}
						<span class="helper-text">Matching niche...</span>
					{:else if nicheMatch}
						<span class="helper-text" style="color: var(--success);">✓ Matched: <strong>{nicheMatch.name}</strong></span>
					{:else if nicheSuggestions.length > 0}
						<span class="helper-text" style="color: var(--warning);">No exact match. Closest: {nicheSuggestions.slice(0, 3).map((s) => s.name).join(', ')}</span>
					{/if}
				</div>
			</div>

			<div style="display: flex; align-items: center; gap: 1.5rem; flex-wrap: wrap;">
				<label style="display: flex; align-items: center; gap: 0.5rem; font-size: 0.875rem; cursor: pointer;">
					<input type="radio" name="channelType" bind:group={isOwner} value={true} />
					<span>This is my channel</span>
				</label>
				<label style="display: flex; align-items: center; gap: 0.5rem; font-size: 0.875rem; cursor: pointer;">
					<input type="radio" name="channelType" bind:group={isOwner} value={false} />
					<span>Tracking a competitor</span>
				</label>
			</div>

			<div style="display: flex; justify-content: flex-end;">
				<button class="btn btn-primary" type="submit" disabled={adding}>
					{adding ? 'Adding...' : 'Add Channel'}
				</button>
			</div>
		</form>
	</div>
{/if}

{#if loading}
	<div style="display: flex; flex-direction: column; gap: 1rem;">
		{#each [1, 2, 3] as _}
			<div class="skeleton" style="height: 80px; border-radius: var(--radius);"></div>
		{/each}
	</div>
{:else if channels.length === 0}
	<EmptyState
		icon="📺"
		headline="No channels added yet"
		cta="Add your first channel to start your performance audit"
	/>
{:else}
	<!-- My Channels -->
	{#if ownedChannels.length > 0}
		<div style="margin-bottom: 2rem;">
			<h2 style="font-size: 0.8125rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.05em; color: var(--text-muted); margin: 0 0 0.75rem;">
				My Channels
			</h2>
			<div style="display: flex; flex-direction: column; gap: 0.75rem;">
				{#each ownedChannels as channel}
					<div class="card" style="display: flex; align-items: center; gap: 1rem; padding: 1rem 1.25rem;">
						<!-- Thumbnail -->
						{#if channel.thumbnailUrl && !failedThumbnails.has(channel.id)}
							<img
								src={channel.thumbnailUrl}
								alt={channel.displayName}
								style="width: 44px; height: 44px; border-radius: 50%; object-fit: cover; flex-shrink: 0;"
								onerror={() => { failedThumbnails = new Set([...failedThumbnails, channel.id]); }}
							/>
						{:else}
							<div style="width: 44px; height: 44px; border-radius: 50%; background: var(--surface-3); flex-shrink: 0; display: flex; align-items: center; justify-content: center; font-size: 1.25rem;">📺</div>
						{/if}

						<!-- Info -->
						<div style="flex: 1; min-width: 0;">
							<div style="display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap; margin-bottom: 0.25rem;">
								<span style="font-size: 0.9375rem; font-weight: 600;">{channel.displayName}</span>
								<Badge variant="creator">Owner</Badge>
								{#if channel.nicheName}
									<Badge variant="info">{channel.nicheName}</Badge>
								{/if}
							</div>
							<div style="font-size: 0.8125rem; color: var(--text-muted); display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap;">
								<span>{channel.handle}</span>
								<span style="color: var(--text-subtle);">·</span>
								<span style="font-family: var(--font-mono);">{formatSubs(channel.subscriberCount)} subs</span>
								{#if channel.lastSyncedAt}
									<span style="color: var(--text-subtle);">·</span>
									<span>Synced {timeSince(channel.lastSyncedAt)}</span>
								{:else}
									<span style="color: var(--text-subtle);">·</span>
									<span style="color: var(--warning);">Not yet synced</span>
								{/if}
							</div>
						</div>

						<!-- Actions -->
						<div style="display: flex; align-items: center; gap: 0.5rem; flex-shrink: 0;">
							{#if !channel.lastSyncedAt}
								<button
									class="btn btn-primary"
									style="font-size: 0.75rem; padding: 0.375rem 0.875rem;"
									onclick={() => syncChannel(channel.id)}
									disabled={syncingId === channel.id}
								>
									{syncingId === channel.id ? 'Syncing...' : 'Sync'}
								</button>
							{:else}
								<button
									class="btn btn-ghost"
									style="font-size: 0.75rem; padding: 0.375rem 0.75rem;"
									onclick={() => syncChannel(channel.id)}
									disabled={syncingId === channel.id}
								>
									{syncingId === channel.id ? '...' : '↺ Sync'}
								</button>
								<button
									class="btn btn-primary"
									style="font-size: 0.75rem; padding: 0.375rem 0.875rem;"
									onclick={() => goto(`/channels/${channel.id}`)}
								>
									View Audit →
								</button>
							{/if}

							{#if confirmDeleteId === channel.id}
								<span style="font-size: 0.8125rem; color: var(--text-muted);">Remove?</span>
								<button class="btn btn-danger" style="padding: 0.375rem 0.75rem; font-size: 0.75rem;" onclick={() => deleteChannel(channel.id)}>Confirm</button>
								<button class="btn btn-ghost" style="padding: 0.375rem 0.75rem; font-size: 0.75rem;" onclick={() => confirmDeleteId = null}>Cancel</button>
							{:else}
								<button class="btn btn-ghost" style="font-size: 0.75rem; padding: 0.375rem 0.5rem; color: var(--text-muted);" onclick={() => confirmDeleteId = channel.id}>✕</button>
							{/if}
						</div>
					</div>
				{/each}
			</div>
		</div>
	{/if}

	<!-- Tracked Channels -->
	{#if trackedChannels.length > 0}
		<div>
			<h2 style="font-size: 0.8125rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.05em; color: var(--text-muted); margin: 0 0 0.75rem;">
				Tracked Channels
			</h2>
			<div style="display: flex; flex-direction: column; gap: 0.75rem;">
				{#each trackedChannels as channel}
					<div class="card" style="display: flex; align-items: center; gap: 1rem; padding: 1rem 1.25rem;">
						{#if channel.thumbnailUrl && !failedThumbnails.has(channel.id)}
							<img
								src={channel.thumbnailUrl}
								alt={channel.displayName}
								style="width: 44px; height: 44px; border-radius: 50%; object-fit: cover; flex-shrink: 0;"
								onerror={() => { failedThumbnails = new Set([...failedThumbnails, channel.id]); }}
							/>
						{:else}
							<div style="width: 44px; height: 44px; border-radius: 50%; background: var(--surface-3); flex-shrink: 0; display: flex; align-items: center; justify-content: center; font-size: 1.25rem;">📺</div>
						{/if}

						<div style="flex: 1; min-width: 0;">
							<div style="display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap; margin-bottom: 0.25rem;">
								<span style="font-size: 0.9375rem; font-weight: 600;">{channel.displayName}</span>
								{#if channel.nicheName}
									<Badge variant="info">{channel.nicheName}</Badge>
								{/if}
							</div>
							<div style="font-size: 0.8125rem; color: var(--text-muted); display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap;">
								<span>{channel.handle}</span>
								<span style="color: var(--text-subtle);">·</span>
								<span style="font-family: var(--font-mono);">{formatSubs(channel.subscriberCount)} subs</span>
								{#if channel.lastSyncedAt}
									<span style="color: var(--text-subtle);">·</span>
									<span>Synced {timeSince(channel.lastSyncedAt)}</span>
								{:else}
									<span style="color: var(--text-subtle);">·</span>
									<span style="color: var(--warning);">Not yet synced</span>
								{/if}
							</div>
						</div>

						<div style="display: flex; align-items: center; gap: 0.5rem; flex-shrink: 0;">
							{#if !channel.lastSyncedAt}
								<button
									class="btn btn-primary"
									style="font-size: 0.75rem; padding: 0.375rem 0.875rem;"
									onclick={() => syncChannel(channel.id)}
									disabled={syncingId === channel.id}
								>
									{syncingId === channel.id ? 'Syncing...' : 'Sync'}
								</button>
							{:else}
								<button
									class="btn btn-ghost"
									style="font-size: 0.75rem; padding: 0.375rem 0.75rem;"
									onclick={() => syncChannel(channel.id)}
									disabled={syncingId === channel.id}
								>
									{syncingId === channel.id ? '...' : '↺ Sync'}
								</button>
							{/if}

							{#if confirmDeleteId === channel.id}
								<span style="font-size: 0.8125rem; color: var(--text-muted);">Remove?</span>
								<button class="btn btn-danger" style="padding: 0.375rem 0.75rem; font-size: 0.75rem;" onclick={() => deleteChannel(channel.id)}>Confirm</button>
								<button class="btn btn-ghost" style="padding: 0.375rem 0.75rem; font-size: 0.75rem;" onclick={() => confirmDeleteId = null}>Cancel</button>
							{:else}
								<button class="btn btn-ghost" style="font-size: 0.75rem; padding: 0.375rem 0.5rem; color: var(--text-muted);" onclick={() => confirmDeleteId = channel.id}>✕</button>
							{/if}
						</div>
					</div>
				{/each}
			</div>
		</div>
	{/if}
{/if}
