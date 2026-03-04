<script lang="ts">
	import { page } from '$app/stores';
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { fetchApi } from '$lib/api/client';
	import { isCreator } from '$lib/stores/auth';
	import Badge from '$lib/components/Badge.svelte';
	import TierGate from '$lib/components/TierGate.svelte';
	import ErrorAlert from '$lib/components/ErrorAlert.svelte';

	interface ChannelVideo {
		youtubeId: string;
		title: string;
		viewCount: number;
		uploadDate?: string;
		thumbnailUrl?: string;
	}

	interface Channel {
		id: string;
		displayName: string;
		isOwner: boolean;
		lastSyncedAt?: string;
	}

	interface ComparisonResult {
		video1Id: string;
		video1Title?: string;
		video2Id: string;
		video2Title?: string;
		transcriptsAvailable: boolean;
		llmAnalysis?: string;
		generatedAt: string;
	}

	const channelId = $page.params.id;

	let myChannel: Channel | null = $state(null);
	let myVideos: ChannelVideo[] = $state([]);
	let allChannels: Channel[] = $state([]);
	let competitorVideos: ChannelVideo[] = $state([]);

	let selectedMyVideo = $state('');
	let selectedCompetitorChannel = $state('');
	let selectedCompetitorVideo = $state('');

	let loadingMyVideos = $state(true);
	let loadingCompetitorVideos = $state(false);
	let running = $state(false);

	let error = $state('');
	let result: ComparisonResult | null = $state(null);
	let resultError = $state('');

	async function load() {
		try {
			const [channels, videos] = await Promise.all([
				fetchApi<Channel[]>('/channels'),
				fetchApi<ChannelVideo[]>(`/channels/${channelId}/videos`)
			]);
			allChannels = channels;
			myChannel = channels.find((c) => c.id === channelId) ?? null;
			myVideos = videos;
		} catch (e: any) {
			error = e.message ?? 'Failed to load';
		} finally {
			loadingMyVideos = false;
		}
	}

	async function loadCompetitorVideos(channelId_: string) {
		if (!channelId_) { competitorVideos = []; return; }
		loadingCompetitorVideos = true;
		selectedCompetitorVideo = '';
		try {
			competitorVideos = await fetchApi<ChannelVideo[]>(`/channels/${channelId_}/videos`);
		} catch (e: any) {
			error = e.message ?? 'Failed to load competitor videos';
		} finally {
			loadingCompetitorVideos = false;
		}
	}

	async function runComparison() {
		if (!selectedMyVideo || !selectedCompetitorVideo) return;
		running = true;
		resultError = '';
		result = null;
		try {
			result = await fetchApi<ComparisonResult>('/channels/videos/compare', {
				method: 'POST',
				body: JSON.stringify({ video1Id: selectedMyVideo, video2Id: selectedCompetitorVideo })
			});
		} catch (e: any) {
			resultError = e.message ?? 'Comparison failed';
		} finally {
			running = false;
		}
	}

	function formatNum(n: number) {
		if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
		if (n >= 1_000) return `${(n / 1_000).toFixed(0)}K`;
		return n.toLocaleString();
	}

	function formatDate(d?: string) {
		if (!d) return '';
		return new Date(d).toLocaleDateString('en-US', { month: 'short', year: 'numeric' });
	}

	const competitorChannels = $derived(allChannels.filter((c) => !c.isOwner));

	$effect(() => {
		if (selectedCompetitorChannel) loadCompetitorVideos(selectedCompetitorChannel);
	});

	onMount(load);
</script>

<!-- Back nav -->
<div style="margin-bottom: 1.25rem;">
	<button class="btn btn-ghost" style="font-size: 0.8125rem; padding: 0.375rem 0.75rem;" onclick={() => goto(`/channels/${channelId}`)}>
		← Back to Audit
	</button>
</div>

<div class="page-header">
	<h1 class="page-title">Compare Videos</h1>
	<p class="page-subtitle">
		AI analyses both transcripts side-by-side to show differences in pacing, hooks, and storytelling.
		<Badge variant="creator" style="margin-left: 0.25rem;">Creator</Badge>
	</p>
</div>

{#if !$isCreator}
	<div class="card">
		<TierGate />
	</div>
{:else}
	{#if error}
		<ErrorAlert message={error} onRetry={load} />
	{/if}

	<!-- Selector cards -->
	<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 1.25rem; margin-bottom: 1.5rem;">
		<!-- My video -->
		<div class="card">
			<div style="font-size: 0.75rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.06em; color: var(--text-muted); margin-bottom: 0.875rem;">
				Your Video
			</div>
			{#if loadingMyVideos}
				<div class="skeleton" style="height: 36px; border-radius: var(--radius);"></div>
			{:else if myVideos.length === 0}
				<p style="font-size: 0.875rem; color: var(--text-muted);">No synced videos. Go back and sync your channel first.</p>
			{:else}
				<select class="input" bind:value={selectedMyVideo}>
					<option value="">— Select your video —</option>
					{#each myVideos as video}
						<option value={video.youtubeId}>
							{video.title ?? video.youtubeId} · {formatNum(video.viewCount)} views {formatDate(video.uploadDate) ? '· ' + formatDate(video.uploadDate) : ''}
						</option>
					{/each}
				</select>
			{/if}
		</div>

		<!-- Competitor video -->
		<div class="card">
			<div style="font-size: 0.75rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.06em; color: var(--text-muted); margin-bottom: 0.875rem;">
				Competitor Video
			</div>
			{#if competitorChannels.length === 0}
				<p style="font-size: 0.875rem; color: var(--text-muted);">
					No competitor channels tracked. <a href="/channels" style="color: var(--accent);">Add one →</a>
				</p>
			{:else}
				<div style="display: flex; flex-direction: column; gap: 0.625rem;">
					<select class="input" bind:value={selectedCompetitorChannel}>
						<option value="">— Select competitor channel —</option>
						{#each competitorChannels as ch}
							<option value={ch.id}>{ch.displayName}</option>
						{/each}
					</select>

					{#if selectedCompetitorChannel}
						{#if loadingCompetitorVideos}
							<div class="skeleton" style="height: 36px; border-radius: var(--radius);"></div>
						{:else if competitorVideos.length === 0}
							<p style="font-size: 0.8125rem; color: var(--text-muted);">No synced videos for this channel. Sync it first from the Channels page.</p>
						{:else}
							<select class="input" bind:value={selectedCompetitorVideo}>
								<option value="">— Select their video —</option>
								{#each competitorVideos as video}
									<option value={video.youtubeId}>
										{video.title ?? video.youtubeId} · {formatNum(video.viewCount)} views {formatDate(video.uploadDate) ? '· ' + formatDate(video.uploadDate) : ''}
									</option>
								{/each}
							</select>
						{/if}
					{/if}
				</div>
			{/if}
		</div>
	</div>

	<!-- Run button -->
	<div style="display: flex; justify-content: center; margin-bottom: 1.5rem;">
		<button
			class="btn btn-primary"
			style="padding: 0.625rem 2rem; font-size: 0.9375rem;"
			onclick={runComparison}
			disabled={!selectedMyVideo || !selectedCompetitorVideo || running}
		>
			{running ? 'Analysing transcripts...' : 'Run Comparison'}
		</button>
	</div>

	{#if running}
		<div class="card" style="display: flex; flex-direction: column; gap: 0.5rem;">
			{#each [1, 2, 3, 4, 5] as _}
				<div class="skeleton" style="height: 1rem; border-radius: 4px;"></div>
			{/each}
		</div>
	{/if}

	{#if resultError}
		<div style="background: var(--danger-muted); border: 1px solid var(--danger); border-radius: var(--radius); padding: 0.875rem 1rem; font-size: 0.875rem; color: var(--danger); margin-bottom: 1rem;">
			{resultError}
		</div>
	{/if}

	{#if result}
		<div class="card">
			{#if !result.transcriptsAvailable}
				<div style="text-align: center; padding: 1rem;">
					<div style="font-size: 1.5rem; margin-bottom: 0.5rem;">⚠️</div>
					<p style="font-size: 0.875rem; color: var(--text-muted);">
						Transcripts were not available for one or both videos. This usually means captions are disabled on YouTube.
						Try selecting different videos.
					</p>
				</div>
			{:else}
				<div style="margin-bottom: 1rem; padding-bottom: 1rem; border-bottom: 1px solid var(--border-subtle); display: flex; gap: 2rem; flex-wrap: wrap;">
					<div>
						<div style="font-size: 0.6875rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.06em; color: var(--text-muted);">Your Video</div>
						<div style="font-size: 0.875rem; font-weight: 500; margin-top: 0.25rem;">{result.video1Title ?? result.video1Id}</div>
					</div>
					<div style="color: var(--text-subtle); font-size: 1.25rem; align-self: center;">vs</div>
					<div>
						<div style="font-size: 0.6875rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.06em; color: var(--text-muted);">Competitor Video</div>
						<div style="font-size: 0.875rem; font-weight: 500; margin-top: 0.25rem;">{result.video2Title ?? result.video2Id}</div>
					</div>
				</div>
				<div style="font-size: 0.875rem; line-height: 1.75; white-space: pre-wrap; color: var(--text);">
					{result.llmAnalysis}
				</div>
			{/if}
		</div>
	{/if}
{/if}
