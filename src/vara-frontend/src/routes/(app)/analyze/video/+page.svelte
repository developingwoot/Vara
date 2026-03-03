<script lang="ts">
	import { fetchApi } from '$lib/api/client';
	import { addRecent } from '$lib/stores/analysis';
	import { isCreator } from '$lib/stores/auth';
	import TierGate from '$lib/components/TierGate.svelte';
	import ErrorAlert from '$lib/components/ErrorAlert.svelte';
	import Badge from '$lib/components/Badge.svelte';

	type State = 'idle' | 'running' | 'complete' | 'error';

	interface VideoAnalysisResult {
		keyword: string;
		sampleSize: number;
		avgTitleLength: number;
		minTitleLength: number;
		maxTitleLength: number;
		titleLengthStdDev: number;
		avgDurationSeconds: number | null;
		minDurationSeconds: number | null;
		maxDurationSeconds: number | null;
		avgViewCount: number;
		avgEngagementRate: number;
		uploadsByDayOfWeek: Record<string, number>;
		patterns: string[];
		analyzedAt: string;
	}

	let analysisState: State = $state('idle');
	let keyword = $state('');
	let sampleSize = $state(20);
	let result: VideoAnalysisResult | null = $state(null);
	let errorMsg = $state('');

	async function runAnalysis() {
		if (!keyword.trim()) return;
		analysisState = 'running';
		errorMsg = '';
		try {
			result = await fetchApi<VideoAnalysisResult>('/analysis/videos', {
				method: 'POST',
				body: JSON.stringify({ keyword: keyword.trim(), sampleSize })
			});
			analysisState = 'complete';
			addRecent({
				id: crypto.randomUUID(),
				type: 'video',
				query: keyword,
				completedAt: new Date()
			});
		} catch (err: unknown) {
			errorMsg = err instanceof Error ? err.message : 'Analysis failed';
			analysisState = 'error';
		}
	}

	function reset() {
		analysisState = 'idle';
		result = null;
		errorMsg = '';
		keyword = '';
		sampleSize = 20;
	}

	function formatSeconds(s: number | null) {
		if (s === null) return '—';
		const m = Math.floor(s / 60);
		const sec = Math.round(s % 60);
		return `${m}:${String(sec).padStart(2, '0')}`;
	}

	const dayOrder = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
</script>

<div class="page-header">
	<h1 class="page-title">Video Analysis</h1>
	<p class="page-subtitle">Statistical patterns from top YouTube videos for any keyword</p>
</div>

{#if analysisState === 'idle' || analysisState === 'complete' || analysisState === 'error'}
	<div class="card" style="margin-bottom: 1.5rem;">
		<form
			onsubmit={(e) => { e.preventDefault(); runAnalysis(); }}
			style="display: flex; flex-direction: column; gap: 1rem;"
		>
			<div style="display: grid; grid-template-columns: 1fr auto; gap: 1rem; align-items: end;">
				<div class="form-group" style="margin: 0;">
					<label class="label" for="vkeyword">Keyword <span style="color: var(--danger);">*</span></label>
					<input
						id="vkeyword"
						class="input"
						type="text"
						bind:value={keyword}
						placeholder="e.g. python tutorial"
						required
					/>
				</div>
				<div class="form-group" style="margin: 0; width: 120px;">
					<label class="label" for="sampleSize">Sample size</label>
					<input
						id="sampleSize"
						class="input"
						type="number"
						bind:value={sampleSize}
						min="5"
						max="50"
					/>
				</div>
			</div>
			<div style="display: flex; justify-content: flex-end;">
				<button class="btn btn-primary" type="submit" disabled={analysisState === 'running'}>
					{analysisState === 'running' ? 'Analyzing...' : 'Analyze Videos'}
				</button>
			</div>
		</form>
	</div>
{/if}

{#if analysisState === 'running'}
	<div class="card" style="display: flex; align-items: center; gap: 1rem; color: var(--text-muted);">
		<span style="animation: spin 1s linear infinite; display: inline-block;">⏳</span>
		Fetching and analyzing videos…
	</div>
{/if}

{#if analysisState === 'error'}
	<ErrorAlert message={errorMsg} onRetry={runAnalysis} />
{/if}

{#if analysisState === 'complete' && result}
	<div style="display: flex; flex-direction: column; gap: 1.5rem;">

		<!-- Key metrics -->
		<div style="display: grid; grid-template-columns: repeat(3, 1fr); gap: 1rem;">
			<div class="card" style="text-align: center;">
				<p style="font-size: 0.75rem; font-weight: 500; color: var(--text-muted); margin: 0 0 0.5rem; text-transform: uppercase; letter-spacing: 0.05em;">Avg Views</p>
				<p style="font-family: var(--font-mono); font-size: 1.25rem; font-weight: 600; margin: 0;">{Math.round(result.avgViewCount).toLocaleString()}</p>
			</div>
			<div class="card" style="text-align: center;">
				<p style="font-size: 0.75rem; font-weight: 500; color: var(--text-muted); margin: 0 0 0.5rem; text-transform: uppercase; letter-spacing: 0.05em;">Avg Engagement</p>
				<p style="font-family: var(--font-mono); font-size: 1.25rem; font-weight: 600; margin: 0;">{(result.avgEngagementRate * 100).toFixed(2)}%</p>
			</div>
			<div class="card" style="text-align: center;">
				<p style="font-size: 0.75rem; font-weight: 500; color: var(--text-muted); margin: 0 0 0.5rem; text-transform: uppercase; letter-spacing: 0.05em;">Videos Analyzed</p>
				<p style="font-family: var(--font-mono); font-size: 1.25rem; font-weight: 600; margin: 0;">{result.sampleSize}</p>
			</div>
		</div>

		<!-- Title stats + Duration -->
		<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 1rem;">
			<div class="card">
				<h3 style="font-size: 0.8125rem; font-weight: 600; color: var(--text-muted); margin: 0 0 0.75rem; text-transform: uppercase; letter-spacing: 0.05em;">Title Length</h3>
				<div style="display: flex; flex-direction: column; gap: 0.5rem;">
					<div style="display: flex; justify-content: space-between; font-size: 0.875rem;">
						<span style="color: var(--text-muted);">Average</span>
						<span style="font-family: var(--font-mono);">{result.avgTitleLength.toFixed(1)} chars</span>
					</div>
					<div style="display: flex; justify-content: space-between; font-size: 0.875rem;">
						<span style="color: var(--text-muted);">Range</span>
						<span style="font-family: var(--font-mono);">{result.minTitleLength} – {result.maxTitleLength}</span>
					</div>
					<div style="display: flex; justify-content: space-between; font-size: 0.875rem;">
						<span style="color: var(--text-muted);">Std Dev</span>
						<span style="font-family: var(--font-mono);">{result.titleLengthStdDev.toFixed(1)}</span>
					</div>
				</div>
			</div>
			<div class="card">
				<h3 style="font-size: 0.8125rem; font-weight: 600; color: var(--text-muted); margin: 0 0 0.75rem; text-transform: uppercase; letter-spacing: 0.05em;">Duration</h3>
				<div style="display: flex; flex-direction: column; gap: 0.5rem;">
					<div style="display: flex; justify-content: space-between; font-size: 0.875rem;">
						<span style="color: var(--text-muted);">Average</span>
						<span style="font-family: var(--font-mono);">{formatSeconds(result.avgDurationSeconds)}</span>
					</div>
					<div style="display: flex; justify-content: space-between; font-size: 0.875rem;">
						<span style="color: var(--text-muted);">Shortest</span>
						<span style="font-family: var(--font-mono);">{formatSeconds(result.minDurationSeconds)}</span>
					</div>
					<div style="display: flex; justify-content: space-between; font-size: 0.875rem;">
						<span style="color: var(--text-muted);">Longest</span>
						<span style="font-family: var(--font-mono);">{formatSeconds(result.maxDurationSeconds)}</span>
					</div>
				</div>
			</div>
		</div>

		<!-- Upload day distribution -->
		{#if Object.keys(result.uploadsByDayOfWeek).length > 0}
			<div class="card">
				<h3 style="font-size: 0.8125rem; font-weight: 600; color: var(--text-muted); margin: 0 0 0.75rem; text-transform: uppercase; letter-spacing: 0.05em;">Best Day to Upload</h3>
				<div style="display: flex; gap: 0.5rem; flex-wrap: wrap;">
					{#each dayOrder as day}
						{@const count = result.uploadsByDayOfWeek[day] ?? 0}
						{#if count > 0}
							<div style="display: flex; flex-direction: column; align-items: center; gap: 0.25rem;">
								<div style="height: {Math.round((count / result.sampleSize) * 60)}px; min-height: 4px; width: 32px; background: var(--primary); border-radius: var(--radius-sm);"></div>
								<span style="font-size: 0.6875rem; color: var(--text-muted);">{day.slice(0, 3)}</span>
								<span style="font-family: var(--font-mono); font-size: 0.6875rem;">{count}</span>
							</div>
						{/if}
					{/each}
				</div>
			</div>
		{/if}

		<!-- Patterns -->
		{#if result.patterns.length > 0}
			<div class="card">
				<h3 style="font-size: 0.8125rem; font-weight: 600; color: var(--text-muted); margin: 0 0 0.75rem; text-transform: uppercase; letter-spacing: 0.05em;">Content Patterns</h3>
				<div style="display: flex; flex-wrap: wrap; gap: 0.5rem;">
					{#each result.patterns as pattern}
						<Badge variant="info">{pattern}</Badge>
					{/each}
				</div>
			</div>
		{/if}

		<div style="display: flex; justify-content: flex-end;">
			<button class="btn btn-ghost" onclick={reset}>New Analysis</button>
		</div>
	</div>
{/if}
