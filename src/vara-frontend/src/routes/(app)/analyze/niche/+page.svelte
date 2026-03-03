<script lang="ts">
	import { fetchApi } from '$lib/api/client';
	import { addRecent } from '$lib/stores/analysis';
	import { isCreator } from '$lib/stores/auth';
	import TierGate from '$lib/components/TierGate.svelte';
	import ErrorAlert from '$lib/components/ErrorAlert.svelte';
	import Badge from '$lib/components/Badge.svelte';
	import EmptyState from '$lib/components/EmptyState.svelte';

	type State = 'idle' | 'running' | 'complete' | 'error';

	interface TrendingKeyword {
		keyword: string;
		niche: string | null;
		currentVolume: number;
		previousVolume: number;
		growthRate: number;
		momentumScore: number;
		lifecycle: string;
		lastCaptured: string;
	}

	interface OutlierVideo {
		videoId: string;
		title: string;
		channelName: string | null;
		subscriberCount: number;
		viewCount: number;
		outlierRatio: number;
		outlierScore: number;
		outlierStrength: string;
		uploadDate: string | null;
		llmInsight: string | null;
	}

	interface OutlierSummary {
		totalAnalyzed: number;
		outliersFound: number;
		strongOutliers: number;
		avgOutlierRatio: number;
		topOpportunityTitle: string | null;
		commonPatterns: string[];
	}

	interface OutlierResult {
		outliers: OutlierVideo[];
		summary: OutlierSummary;
		quotaUsed: number;
	}

	interface NicheComparisonResult {
		niche: string;
		trendingKeywords: TrendingKeyword[];
		topOutliers: OutlierResult;
		generatedAt: string;
	}

	let analysisState: State = $state('idle');
	let niche = $state('');
	let includeInsights = $state(false);
	let result: NicheComparisonResult | null = $state(null);
	let errorMsg = $state('');

	async function runAnalysis() {
		if (!niche.trim()) return;
		analysisState = 'running';
		errorMsg = '';
		try {
			result = await fetchApi<NicheComparisonResult>('/analysis/niche/compare', {
				method: 'POST',
				body: JSON.stringify({ niche: niche.trim(), includeInsights })
			});
			analysisState = 'complete';
			addRecent({
				id: crypto.randomUUID(),
				type: 'niche',
				query: niche,
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
		niche = '';
	}

	function growthVariant(rate: number) {
		if (rate > 0.1) return 'success';
		if (rate < -0.05) return 'danger';
		return 'info';
	}

	function formatGrowth(rate: number) {
		const pct = (rate * 100).toFixed(1);
		return rate > 0 ? `+${pct}%` : `${pct}%`;
	}

	function strengthVariant(s: string) {
		if (s === 'strong') return 'success';
		if (s === 'moderate') return 'warning';
		return 'info';
	}
</script>

<div class="page-header">
	<h1 class="page-title">Niche Compare</h1>
	<p class="page-subtitle">Trending keywords and outlier video opportunities for a niche</p>
</div>

{#if analysisState === 'idle' || analysisState === 'complete' || analysisState === 'error'}
	<div class="card" style="margin-bottom: 1.5rem;">
		<form
			onsubmit={(e) => { e.preventDefault(); runAnalysis(); }}
			style="display: flex; flex-direction: column; gap: 1rem;"
		>
			<div class="form-group">
				<label class="label" for="niche">Niche <span style="color: var(--danger);">*</span></label>
				<input
					id="niche"
					class="input"
					type="text"
					bind:value={niche}
					placeholder="e.g. Personal Finance"
					required
				/>
			</div>

			<div style="display: flex; align-items: center; justify-content: space-between;">
				<TierGate feature="AI Insights" locked={!$isCreator}>
					<label style="display: flex; align-items: center; gap: 0.75rem; cursor: pointer;">
						<button
							type="button"
							class="toggle-track"
							class:on={includeInsights}
							onclick={() => $isCreator && (includeInsights = !includeInsights)}
							aria-pressed={includeInsights}
							aria-label="Toggle AI Insights"
						>
							<span class="toggle-thumb"></span>
						</button>
						<span style="font-size: 0.875rem;">AI Insights</span>
					</label>
				</TierGate>
				<button class="btn btn-primary" type="submit" disabled={analysisState === 'running'}>
					{analysisState === 'running' ? 'Analyzing...' : 'Compare Niche'}
				</button>
			</div>
		</form>
	</div>
{/if}

{#if analysisState === 'running'}
	<div class="card" style="display: flex; align-items: center; gap: 1rem; color: var(--text-muted);">
		<span style="animation: spin 1s linear infinite; display: inline-block;">⏳</span>
		Analyzing niche…
	</div>
{/if}

{#if analysisState === 'error'}
	<ErrorAlert message={errorMsg} onRetry={runAnalysis} />
{/if}

{#if analysisState === 'complete' && result}
	<div style="display: flex; flex-direction: column; gap: 1.5rem;">

		<!-- Trending keywords -->
		<div class="card" style="padding: 0;">
			<div style="padding: 1rem 1.25rem; border-bottom: 1px solid var(--border); display: flex; align-items: center; gap: 0.5rem;">
				<span>📈</span>
				<h2 style="font-size: 0.875rem; font-weight: 600; margin: 0;">Trending Keywords</h2>
				<Badge variant="success">{result.trendingKeywords.length}</Badge>
			</div>
			{#if result.trendingKeywords.length === 0}
				<EmptyState icon="📊" headline="No trending keywords found" />
			{:else}
				<table>
					<thead>
						<tr>
							<th>Keyword</th>
							<th>Growth</th>
							<th>Momentum</th>
							<th>Lifecycle</th>
						</tr>
					</thead>
					<tbody>
						{#each result.trendingKeywords as kw}
							<tr>
								<td style="font-weight: 500;">{kw.keyword}</td>
								<td><Badge variant={growthVariant(kw.growthRate)}>{formatGrowth(kw.growthRate)}</Badge></td>
								<td style="font-family: var(--font-mono); font-size: 0.8125rem;">{kw.momentumScore.toFixed(2)}</td>
								<td><Badge variant="default">{kw.lifecycle}</Badge></td>
							</tr>
						{/each}
					</tbody>
				</table>
			{/if}
		</div>

		<!-- Outlier summary -->
		{#if result.topOutliers.summary}
			{@const s = result.topOutliers.summary}
			<div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 1rem;">
				<div class="card" style="text-align: center;">
					<p style="font-size: 0.75rem; font-weight: 500; color: var(--text-muted); margin: 0 0 0.5rem; text-transform: uppercase; letter-spacing: 0.05em;">Analyzed</p>
					<p style="font-family: var(--font-mono); font-size: 1.25rem; font-weight: 600; margin: 0;">{s.totalAnalyzed}</p>
				</div>
				<div class="card" style="text-align: center;">
					<p style="font-size: 0.75rem; font-weight: 500; color: var(--text-muted); margin: 0 0 0.5rem; text-transform: uppercase; letter-spacing: 0.05em;">Outliers</p>
					<p style="font-family: var(--font-mono); font-size: 1.25rem; font-weight: 600; margin: 0;">{s.outliersFound}</p>
				</div>
				<div class="card" style="text-align: center;">
					<p style="font-size: 0.75rem; font-weight: 500; color: var(--text-muted); margin: 0 0 0.5rem; text-transform: uppercase; letter-spacing: 0.05em;">Strong</p>
					<p style="font-family: var(--font-mono); font-size: 1.25rem; font-weight: 600; margin: 0;">{s.strongOutliers}</p>
				</div>
				<div class="card" style="text-align: center;">
					<p style="font-size: 0.75rem; font-weight: 500; color: var(--text-muted); margin: 0 0 0.5rem; text-transform: uppercase; letter-spacing: 0.05em;">Avg Ratio</p>
					<p style="font-family: var(--font-mono); font-size: 1.25rem; font-weight: 600; margin: 0;">{s.avgOutlierRatio.toFixed(1)}×</p>
				</div>
			</div>
		{/if}

		<!-- Outlier videos -->
		{#if result.topOutliers.outliers.length > 0}
			<div class="card" style="padding: 0;">
				<div style="padding: 1rem 1.25rem; border-bottom: 1px solid var(--border); display: flex; align-items: center; gap: 0.5rem;">
					<span>🎯</span>
					<h2 style="font-size: 0.875rem; font-weight: 600; margin: 0;">Outlier Videos</h2>
					<Badge variant="warning">{result.topOutliers.outliers.length}</Badge>
				</div>
				<table>
					<thead>
						<tr>
							<th>Title</th>
							<th>Channel</th>
							<th>Subscribers</th>
							<th>Views</th>
							<th>Ratio</th>
							<th>Score</th>
						</tr>
					</thead>
					<tbody>
						{#each result.topOutliers.outliers as v}
							<tr>
								<td style="font-weight: 500; max-width: 220px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">
									<a href="https://youtube.com/watch?v={v.videoId}" target="_blank" rel="noopener" style="color: inherit; text-decoration: none;">{v.title}</a>
								</td>
								<td style="font-size: 0.8125rem; color: var(--text-muted);">{v.channelName ?? '—'}</td>
								<td style="font-family: var(--font-mono); font-size: 0.8125rem;">{v.subscriberCount.toLocaleString()}</td>
								<td style="font-family: var(--font-mono); font-size: 0.8125rem;">{v.viewCount.toLocaleString()}</td>
								<td style="font-family: var(--font-mono); font-size: 0.8125rem;">{v.outlierRatio.toFixed(1)}×</td>
								<td><Badge variant={strengthVariant(v.outlierStrength)}>{v.outlierScore}</Badge></td>
							</tr>
							{#if v.llmInsight}
								<tr>
									<td colspan="6" style="font-size: 0.8125rem; color: var(--text-muted); font-style: italic; padding-top: 0; padding-bottom: 0.75rem;">{v.llmInsight}</td>
								</tr>
							{/if}
						{/each}
					</tbody>
				</table>
			</div>
		{/if}

		<div style="display: flex; justify-content: flex-end;">
			<button class="btn btn-ghost" onclick={reset}>New Comparison</button>
		</div>
	</div>
{/if}
