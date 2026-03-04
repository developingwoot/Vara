<script lang="ts">
	import { page } from '$app/stores';
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { fetchApi } from '$lib/api/client';
	import { isCreator } from '$lib/stores/auth';
	import Badge from '$lib/components/Badge.svelte';
	import TierGate from '$lib/components/TierGate.svelte';
	import ErrorAlert from '$lib/components/ErrorAlert.svelte';

	interface TranscriptAnalysis {
		videoId: string;
		title?: string;
		channelName?: string;
		wordCount: number;
		sentenceCount: number;
		estimatedTokens: number;
		readingTimeMinutes: number;
		transcriptAvailable: boolean;
		llmInsights?: string;
		llmEnhanced: boolean;
		analyzedAt: string;
	}

	const channelId = $page.params.id;
	const videoId = $page.params.videoId;

	let analysis: TranscriptAnalysis | null = $state(null);
	let loading = $state(false);
	let error = $state('');
	let insightsLoading = $state(false);
	let insightsError = $state('');

	// We first load without LLM insights (free), then optionally with insights (Creator)
	async function loadAnalysis(includeInsights = false) {
		if (includeInsights) {
			insightsLoading = true;
			insightsError = '';
		} else {
			loading = true;
			error = '';
		}

		try {
			const result = await fetchApi<TranscriptAnalysis>(
				`/analysis/videos/${videoId}/transcript`,
				{
					method: 'POST',
					body: JSON.stringify({ includeInsights })
				}
			);
			analysis = result;
		} catch (e: any) {
			if (includeInsights) insightsError = e.message ?? 'AI analysis failed';
			else error = e.message ?? 'Failed to load video analysis';
		} finally {
			if (includeInsights) insightsLoading = false;
			else loading = false;
		}
	}

	function formatNum(n: number) {
		return n.toLocaleString();
	}

	onMount(() => loadAnalysis(false));
</script>

<!-- Back nav -->
<div style="margin-bottom: 1.25rem;">
	<button class="btn btn-ghost" style="font-size: 0.8125rem; padding: 0.375rem 0.75rem;" onclick={() => goto(`/channels/${channelId}`)}>
		← Back to Audit
	</button>
</div>

{#if loading}
	<div style="display: flex; flex-direction: column; gap: 1rem;">
		{#each [1, 2, 3] as _}
			<div class="skeleton" style="height: 80px; border-radius: var(--radius);"></div>
		{/each}
	</div>
{:else if error}
	<ErrorAlert message={error} onRetry={() => loadAnalysis(false)} />
{:else if analysis}
	<div class="page-header">
		<h1 class="page-title" style="font-size: 1.25rem;">{analysis.title ?? videoId}</h1>
		<p class="page-subtitle" style="font-family: var(--font-mono); font-size: 0.75rem; margin-top: 0.25rem;">{videoId}</p>
	</div>

	<!-- Transcript stats -->
	<div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(140px, 1fr)); gap: 0.875rem; margin-bottom: 1.25rem;">
		<div class="card" style="text-align: center; padding: 1rem;">
			<div style="font-size: 1.5rem; font-weight: 700; font-family: var(--font-mono); color: var(--text);">{formatNum(analysis.wordCount)}</div>
			<div style="font-size: 0.75rem; color: var(--text-muted); margin-top: 0.25rem; text-transform: uppercase; letter-spacing: 0.04em;">Words</div>
		</div>
		<div class="card" style="text-align: center; padding: 1rem;">
			<div style="font-size: 1.5rem; font-weight: 700; font-family: var(--font-mono); color: var(--text);">{formatNum(analysis.sentenceCount)}</div>
			<div style="font-size: 0.75rem; color: var(--text-muted); margin-top: 0.25rem; text-transform: uppercase; letter-spacing: 0.04em;">Sentences</div>
		</div>
		<div class="card" style="text-align: center; padding: 1rem;">
			<div style="font-size: 1.5rem; font-weight: 700; font-family: var(--font-mono); color: var(--text);">{analysis.readingTimeMinutes.toFixed(1)}m</div>
			<div style="font-size: 0.75rem; color: var(--text-muted); margin-top: 0.25rem; text-transform: uppercase; letter-spacing: 0.04em;">Read Time</div>
		</div>
		<div class="card" style="text-align: center; padding: 1rem;">
			<div style="font-size: 1.5rem; font-weight: 700; font-family: var(--font-mono); color: {analysis.transcriptAvailable ? 'var(--success)' : 'var(--danger)'};">
				{analysis.transcriptAvailable ? '✓' : '✗'}
			</div>
			<div style="font-size: 0.75rem; color: var(--text-muted); margin-top: 0.25rem; text-transform: uppercase; letter-spacing: 0.04em;">Transcript</div>
		</div>
	</div>

	{#if !analysis.transcriptAvailable}
		<div class="card" style="text-align: center; padding: 1.5rem;">
			<p style="font-size: 0.875rem; color: var(--text-muted); margin: 0;">
				No transcript is available for this video. Captions may be disabled on YouTube, or the video is too recent to have auto-generated captions.
			</p>
		</div>
	{:else}
		<!-- AI Insights -->
		<div class="card">
			<div style="display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 1rem; margin-bottom: {analysis.llmInsights || insightsLoading ? '1rem' : '0'};">
				<div>
					<div style="display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.375rem;">
						<span style="font-size: 0.75rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.06em; color: var(--text-muted);">AI Insights</span>
						<Badge variant="creator">Creator</Badge>
					</div>
					<p style="font-size: 0.875rem; color: var(--text-muted); margin: 0;">
						Hooks, pacing, content gaps, CTAs, and what makes this video stand out.
					</p>
				</div>
				{#if $isCreator && !analysis.llmInsights}
					<button
						class="btn btn-primary"
						onclick={() => loadAnalysis(true)}
						disabled={insightsLoading}
					>
						{insightsLoading ? 'Analysing...' : 'Run AI Analysis'}
					</button>
				{:else if !$isCreator}
					<TierGate />
				{/if}
			</div>

			{#if insightsError}
				<div style="background: var(--danger-muted); border: 1px solid var(--danger); border-radius: var(--radius); padding: 0.75rem 1rem; font-size: 0.875rem; color: var(--danger);">
					{insightsError}
				</div>
			{/if}

			{#if insightsLoading}
				<div style="display: flex; flex-direction: column; gap: 0.5rem;">
					{#each [1, 2, 3, 4] as _}
						<div class="skeleton" style="height: 1rem; border-radius: 4px;"></div>
					{/each}
				</div>
			{/if}

			{#if analysis.llmInsights}
				<div style="border-top: 1px solid var(--border-subtle); padding-top: 1rem; font-size: 0.875rem; line-height: 1.75; white-space: pre-wrap; color: var(--text);">
					{analysis.llmInsights}
				</div>
			{/if}
		</div>
	{/if}
{/if}
