<script lang="ts">
	import { onMount } from 'svelte';
	import { fetchApi } from '$lib/api/client';
	import EmptyState from '$lib/components/EmptyState.svelte';
	import Badge from '$lib/components/Badge.svelte';

	interface Channel {
		id: string;
		displayName: string;
		handle: string;
		subscriberCount: number;
		thumbnailUrl?: string;
		isOwner: boolean;
		nicheRaw?: string;
		nicheName?: string;
	}

	interface NicheMatch {
		id: number;
		name: string;
		slug: string;
	}

	let channels: Channel[] = $state([]);
	let loading = $state(true);
	let adding = $state(false);
	let confirmDeleteId: string | null = $state(null);

	let youtubeUrl = $state('');
	let nicheInput = $state('');
	let nicheMatch: NicheMatch | null = $state(null);
	let nicheResolving = $state(false);
	let nicheSuggestions: { name: string; slug: string; confidence: number }[] = $state([]);
	let addError = $state('');
	let failedThumbnails = $state(new Set<string>());

	let nicheTimer: ReturnType<typeof setTimeout>;

	async function loadChannels() {
		try {
			channels = await fetchApi<Channel[]>('/channels');
		} finally {
			loading = false;
		}
	}

	async function resolveNiche(raw: string) {
		if (!raw.trim()) {
			nicheMatch = null;
			nicheSuggestions = [];
			return;
		}
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
		} catch {
			// resolve errors are non-blocking
		} finally {
			nicheResolving = false;
		}
	}

	function onNicheInput() {
		clearTimeout(nicheTimer);
		nicheMatch = null;
		nicheSuggestions = [];
		if (nicheInput.trim().length >= 2) {
			nicheTimer = setTimeout(() => resolveNiche(nicheInput), 400);
		}
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
					isOwner: false,
					niche: nicheInput.trim() || null
				})
			});
			youtubeUrl = '';
			nicheInput = '';
			nicheMatch = null;
			nicheSuggestions = [];
			await loadChannels();
		} catch (err: any) {
			addError = err.message ?? 'Failed to add channel';
		} finally {
			adding = false;
		}
	}

	async function deleteChannel(id: string) {
		try {
			await fetchApi(`/channels/${id}`, { method: 'DELETE' });
			channels = channels.filter((c) => c.id !== id);
			confirmDeleteId = null;
		} catch (err: any) {
			alert(err.message ?? 'Failed to delete channel');
		}
	}

	function formatSubs(n: number) {
		if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
		if (n >= 1_000) return `${(n / 1_000).toFixed(0)}K`;
		return n.toString();
	}

	onMount(loadChannels);
</script>

<div class="page-header">
	<h1 class="page-title">Channels</h1>
	<p class="page-subtitle">Manage the YouTube channels you track</p>
</div>

<!-- Add channel -->
<div class="card" style="margin-bottom: 1.5rem;">
	<h2 style="font-size: 0.9375rem; font-weight: 600; margin: 0 0 1rem;">Add Channel</h2>

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
				<!-- Resolve feedback -->
				{#if nicheResolving}
					<span class="helper-text">Matching niche...</span>
				{:else if nicheMatch}
					<span class="helper-text" style="color: var(--success);">✓ Matched: <strong>{nicheMatch.name}</strong></span>
				{:else if nicheSuggestions.length > 0}
					<span class="helper-text" style="color: var(--warning);">No exact match. Closest: {nicheSuggestions.slice(0, 3).map((s) => s.name).join(', ')}</span>
				{/if}
			</div>
		</div>
		<div style="display: flex; justify-content: flex-end;">
			<button class="btn btn-primary" type="submit" disabled={adding}>
				{adding ? 'Adding...' : 'Add Channel'}
			</button>
		</div>
	</form>
</div>

<!-- Channel list -->
<div class="card" style="padding: 0;">
	<div style="padding: 1.25rem 1.5rem; border-bottom: 1px solid var(--border);">
		<h2 style="font-size: 0.9375rem; font-weight: 600; margin: 0;">Your Channels</h2>
	</div>

	{#if loading}
		<div style="padding: 1.5rem; display: flex; flex-direction: column; gap: 0.75rem;">
			{#each [1, 2, 3] as _}
				<div class="skeleton" style="height: 64px; border-radius: var(--radius);"></div>
			{/each}
		</div>
	{:else if channels.length === 0}
		<EmptyState icon="📺" headline="No channels yet" cta="Scroll up to add your first channel" />
	{:else}
		<div>
			{#each channels as channel}
				<div style="display: flex; align-items: center; gap: 1rem; padding: 1rem 1.5rem; border-bottom: 1px solid var(--border-subtle);">
					{#if channel.thumbnailUrl && !failedThumbnails.has(channel.id)}
						<img
							src={channel.thumbnailUrl}
							alt={channel.displayName}
							style="width: 40px; height: 40px; border-radius: 50%; object-fit: cover; flex-shrink: 0;"
							onerror={() => { failedThumbnails = new Set([...failedThumbnails, channel.id]); }}
						/>
					{:else}
						<div style="width: 40px; height: 40px; border-radius: 50%; background: var(--surface-3); flex-shrink: 0;"></div>
					{/if}

					<div style="flex: 1; min-width: 0;">
						<div style="display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap;">
							<span style="font-size: 0.875rem; font-weight: 500; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">
								{channel.displayName}
							</span>
							{#if channel.isOwner}
								<Badge variant="creator">Owner</Badge>
							{/if}
							{#if channel.nicheName}
								<Badge variant="info">{channel.nicheName}</Badge>
							{/if}
						</div>
						<div style="font-size: 0.8125rem; color: var(--text-muted);">
							{channel.handle}
							<span style="margin: 0 0.375rem; color: var(--text-subtle);">·</span>
							<span style="font-family: var(--font-mono);">{formatSubs(channel.subscriberCount)}</span> subs
						</div>
					</div>

					{#if confirmDeleteId === channel.id}
						<div style="display: flex; align-items: center; gap: 0.5rem;">
							<span style="font-size: 0.8125rem; color: var(--text-muted);">Remove?</span>
							<button class="btn btn-danger" style="padding: 0.375rem 0.75rem; font-size: 0.75rem;" onclick={() => deleteChannel(channel.id)}>
								Confirm
							</button>
							<button class="btn btn-ghost" style="padding: 0.375rem 0.75rem; font-size: 0.75rem;" onclick={() => confirmDeleteId = null}>
								Cancel
							</button>
						</div>
					{:else}
						<button class="btn btn-ghost" style="font-size: 0.75rem; padding: 0.375rem 0.75rem;" onclick={() => confirmDeleteId = channel.id}>
							Remove
						</button>
					{/if}
				</div>
			{/each}
		</div>
	{/if}
</div>
