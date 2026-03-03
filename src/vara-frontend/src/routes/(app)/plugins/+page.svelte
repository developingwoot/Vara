<script lang="ts">
	import { onMount } from 'svelte';
	import { fetchApi } from '$lib/api/client';
	import { isCreator } from '$lib/stores/auth';
	import Badge from '$lib/components/Badge.svelte';
	import TierGate from '$lib/components/TierGate.svelte';
	import EmptyState from '$lib/components/EmptyState.svelte';

	interface PluginItem {
		id: string;
		name: string;
		version: string;
		description: string;
		tier: 'free' | 'creator';
		unitsPerRun: number;
		enabled: boolean;
	}

	interface OutlierResult {
		summary: {
			totalAnalyzed: number;
			outliersFound: number;
			strongOutliers: number;
			avgOutlierRatio: number;
		};
		outliers: Array<{
			videoId: string;
			title: string;
			channelName: string;
			subscriberCount: number;
			viewCount: number;
			outlierRatio: number;
			outlierScore: number;
			outlierStrength: string;
			llmInsight?: string;
		}>;
	}

	let plugins: PluginItem[] = $state([]);
	let loadingPlugins = $state(true);

	// Outlier detection form
	let odKeyword = $state('');
	let odMaxResults = $state(10);
	let odInsights = $state(false);
	let odRunning = $state(false);
	let odResult: OutlierResult | null = $state(null);
	let odError = $state('');

	async function loadPlugins() {
		try {
			plugins = await fetchApi<PluginItem[]>('/plugins');
		} finally {
			loadingPlugins = false;
		}
	}

	async function runOutlierDetection(e: SubmitEvent) {
		e.preventDefault();
		odRunning = true;
		odError = '';
		odResult = null;
		try {
			const response = await fetchApi<{ result: OutlierResult }>('/plugins/outlier-detection/execute', {
				method: 'POST',
				body: JSON.stringify({
					keyword: odKeyword.trim(),
					maxResults: odMaxResults,
					includeLlmInsights: odInsights
				})
			});
			odResult = response.result;
		} catch (err: any) {
			odError = err.message ?? 'Plugin execution failed';
		} finally {
			odRunning = false;
		}
	}

	function strengthVariant(s: string) {
		if (s === 'Strong') return 'success';
		if (s === 'Moderate') return 'warning';
		return 'info';
	}

	function formatNum(n: number) {
		if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
		if (n >= 1_000) return `${(n / 1_000).toFixed(0)}K`;
		return n.toString();
	}

	onMount(loadPlugins);
</script>

<div class="page-header">
	<h1 class="page-title">Plugins</h1>
	<p class="page-subtitle">Extend VARA with specialized analysis tools</p>
</div>

<!-- Plugin list -->
{#if loadingPlugins}
	<div style="display: flex; flex-direction: column; gap: 1rem; margin-bottom: 2rem;">
		{#each [1, 2] as _}
			<div class="skeleton" style="height: 80px; border-radius: var(--radius-lg);"></div>
		{/each}
	</div>
{:else if plugins.length === 0}
	<EmptyState icon="🧩" headline="No plugins discovered yet" />
{:else}
	<div style="display: flex; flex-direction: column; gap: 1rem; margin-bottom: 2rem;">
		{#each plugins as plugin}
			<div class="card" style="display: flex; align-items: center; gap: 1rem;">
				<div style="flex: 1;">
					<div style="display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.375rem;">
						<span style="font-size: 0.9375rem; font-weight: 600;">{plugin.name}</span>
						<Badge variant={plugin.tier === 'creator' ? 'creator' : 'free'}>{plugin.tier}</Badge>
						<span style="font-size: 0.75rem; color: var(--text-subtle);">v{plugin.version}</span>
					</div>
					<p style="font-size: 0.8125rem; color: var(--text-muted); margin: 0;">{plugin.description}</p>
				</div>
				<div style="text-align: right; flex-shrink: 0;">
					<span style="font-family: var(--font-mono); font-size: 0.8125rem; color: var(--text-subtle);">{plugin.unitsPerRun} quota units/run</span>
				</div>
			</div>
		{/each}
	</div>
{/if}

<!-- Outlier Detection execute panel -->
<div class="card">
	<h2 style="font-size: 0.9375rem; font-weight: 600; margin: 0 0 1.25rem;">Run Outlier Detection</h2>

	{#if odError}
		<div style="background: var(--danger-muted); border: 1px solid var(--danger); border-radius: var(--radius); padding: 0.75rem 1rem; margin-bottom: 1rem; font-size: 0.875rem; color: var(--danger);">
			{odError}
		</div>
	{/if}

	<form onsubmit={runOutlierDetection} style="display: flex; flex-direction: column; gap: 1rem;">
		<div class="form-group">
			<label class="label" for="od-keyword">Keyword <span style="color: var(--danger);">*</span></label>
			<input
				id="od-keyword"
				class="input"
				type="text"
				bind:value={odKeyword}
				placeholder="e.g. react hooks tutorial"
				required
			/>
		</div>

		<div class="form-group">
			<label class="label" for="od-maxresults">Max Results: <span style="font-family: var(--font-mono);">{odMaxResults}</span></label>
			<input
				id="od-maxresults"
				type="range"
				min="5"
				max="20"
				step="1"
				bind:value={odMaxResults}
				style="width: 100%; accent-color: var(--primary);"
			/>
			<div style="display: flex; justify-content: space-between; font-size: 0.75rem; color: var(--text-subtle);">
				<span>5</span><span>20</span>
			</div>
		</div>

		<div style="display: flex; align-items: center; justify-content: space-between;">
			<TierGate feature="AI Insights" locked={!$isCreator}>
				<label style="display: flex; align-items: center; gap: 0.75rem; cursor: pointer;">
					<button
						type="button"
						class="toggle-track"
						class:on={odInsights}
						onclick={() => $isCreator && (odInsights = !odInsights)}
						aria-pressed={odInsights}
						aria-label="Toggle AI Insights"
					>
						<span class="toggle-thumb"></span>
					</button>
					<span style="font-size: 0.875rem;">AI Insights per video</span>
				</label>
			</TierGate>

			<button class="btn btn-primary" type="submit" disabled={odRunning}>
				{odRunning ? 'Running...' : 'Run Outlier Detection'}
			</button>
		</div>
	</form>
</div>

{#if odResult}
	<div style="margin-top: 1.5rem; display: flex; flex-direction: column; gap: 1.5rem;">
		<!-- Summary -->
		<div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 1rem;">
			<div class="card" style="text-align: center;">
				<div style="font-family: var(--font-mono); font-size: 1.5rem; font-weight: 500;">{odResult.summary.totalAnalyzed}</div>
				<div style="font-size: 0.75rem; color: var(--text-muted); margin-top: 0.25rem;">Analyzed</div>
			</div>
			<div class="card" style="text-align: center;">
				<div style="font-family: var(--font-mono); font-size: 1.5rem; font-weight: 500;">{odResult.summary.outliersFound}</div>
				<div style="font-size: 0.75rem; color: var(--text-muted); margin-top: 0.25rem;">Outliers Found</div>
			</div>
			<div class="card" style="text-align: center;">
				<div style="font-family: var(--font-mono); font-size: 1.5rem; font-weight: 500; color: var(--success);">{odResult.summary.strongOutliers}</div>
				<div style="font-size: 0.75rem; color: var(--text-muted); margin-top: 0.25rem;">Strong Outliers</div>
			</div>
			<div class="card" style="text-align: center;">
				<div style="font-family: var(--font-mono); font-size: 1.5rem; font-weight: 500;">{odResult.summary.avgOutlierRatio.toFixed(1)}×</div>
				<div style="font-size: 0.75rem; color: var(--text-muted); margin-top: 0.25rem;">Avg Ratio</div>
			</div>
		</div>

		<!-- Outlier table -->
		{#if odResult.outliers.length === 0}
			<EmptyState icon="🔍" headline="No outliers found — try a different keyword or lower the threshold" />
		{:else}
			<div class="card" style="padding: 0; overflow: hidden;">
				<div style="padding: 1.25rem 1.5rem; border-bottom: 1px solid var(--border);">
					<h2 style="font-size: 0.9375rem; font-weight: 600; margin: 0;">Outlier Videos</h2>
				</div>
				<div style="overflow-x: auto;">
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
							{#each odResult.outliers as video}
								<tr>
									<td style="max-width: 240px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-weight: 500;">{video.title}</td>
									<td style="color: var(--text-muted);">{video.channelName}</td>
									<td style="font-family: var(--font-mono);">{formatNum(video.subscriberCount)}</td>
									<td style="font-family: var(--font-mono);">{formatNum(video.viewCount)}</td>
									<td style="font-family: var(--font-mono);">{video.outlierRatio.toFixed(1)}×</td>
									<td>
										<Badge variant={strengthVariant(video.outlierStrength)}>
											{video.outlierScore} {video.outlierStrength}
										</Badge>
									</td>
								</tr>
								{#if video.llmInsight}
									<tr>
										<td colspan={6} style="background: var(--surface-2); padding: 0.75rem 1rem; font-size: 0.8125rem; color: var(--text-muted); line-height: 1.5; border-bottom: 1px solid var(--border);">
											💡 {video.llmInsight}
										</td>
									</tr>
								{/if}
							{/each}
						</tbody>
					</table>
				</div>
			</div>
		{/if}
	</div>
{/if}
