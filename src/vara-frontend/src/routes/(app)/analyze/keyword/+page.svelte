<script lang="ts">
	import { isCreator } from '$lib/stores/auth';
	import { addRecent } from '$lib/stores/analysis';
	import { ensureConnected } from '$lib/api/signalr';
	import ScoreDisplay from '$lib/components/ScoreDisplay.svelte';
	import AnalysisProgress from '$lib/components/AnalysisProgress.svelte';
	import ErrorAlert from '$lib/components/ErrorAlert.svelte';
	import TierGate from '$lib/components/TierGate.svelte';
	import Badge from '$lib/components/Badge.svelte';

	type State = 'idle' | 'running' | 'complete' | 'error';

	let analysisState: State = $state('idle');
	let keyword = $state('');
	let niche = $state('');
	let includeInsights = $state(false);

	let step = $state(1);
	let stage = $state('');
	let percent = $state(0);

	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	let result: any = $state(null);
	let errorMsg = $state('');

	async function runAnalysis() {
		if (!keyword.trim()) return;
		analysisState = 'running';
		step = 1;
		stage = 'Starting...';
		percent = 5;
		errorMsg = '';

		try {
			const hub = await ensureConnected();

			// Clean up any previous handlers
			hub.off('AnalysisProgress');
			hub.off('AnalysisComplete');
			hub.off('AnalysisError');

			hub.on('AnalysisProgress', (msg: any) => {
				step = msg.step;
				stage = msg.stage;
				percent = msg.percent;
			});

			hub.on('AnalysisComplete', (msg: any) => {
				result = msg.data;
				analysisState = 'complete';
				addRecent({
					id: msg.analysisId,
					type: 'keyword',
					query: keyword,
					score: result?.competitionScore,
					completedAt: new Date()
				});
			});

			hub.on('AnalysisError', (err: any) => {
				errorMsg = err.message;
				analysisState = 'error';
			});

			await hub.invoke('StartAnalysis', {
				type: 'keyword',
				keyword: keyword.trim(),
				niche: niche.trim() || null,
				includeInsights
			});
		} catch (err: any) {
			errorMsg = err.message ?? 'Connection failed';
			analysisState = 'error';
		}
	}

	function reset() {
		analysisState = 'idle';
		result = null;
		errorMsg = '';
		keyword = '';
		niche = '';
	}

	const trendVariant = (dir: string) =>
		dir === 'rising' ? 'success' : dir === 'declining' ? 'danger' : 'info';
</script>

<div class="page-header">
	<h1 class="page-title">Keyword Analysis</h1>
	<p class="page-subtitle">Discover competition scores and content opportunities</p>
</div>

{#if analysisState === 'idle' || analysisState === 'complete' || analysisState === 'error'}
	<div class="card" style="margin-bottom: 1.5rem;">
		<form
			onsubmit={(e) => { e.preventDefault(); runAnalysis(); }}
			style="display: flex; flex-direction: column; gap: 1rem;"
		>
			<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 1rem;">
				<div class="form-group">
					<label class="label" for="keyword">Keyword <span style="color: var(--danger);">*</span></label>
					<input
						id="keyword"
						class="input"
						type="text"
						bind:value={keyword}
						placeholder="e.g. react tutorial"
						required
					/>
				</div>
				<div class="form-group">
					<label class="label" for="niche">Niche <span style="color: var(--text-subtle);">(optional)</span></label>
					<input
						id="niche"
						class="input"
						type="text"
						bind:value={niche}
						placeholder="e.g. Web Dev"
					/>
				</div>
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

				<button class="btn btn-primary" type="submit">
					Run Analysis
				</button>
			</div>
		</form>
	</div>
{/if}

{#if analysisState === 'running'}
	<div style="display: flex; flex-direction: column; gap: 1rem;">
		<AnalysisProgress {step} {stage} {percent} />
		<div style="display: flex; justify-content: flex-end;">
			<button class="btn btn-ghost" onclick={reset}>Cancel</button>
		</div>
	</div>
{/if}

{#if analysisState === 'error'}
	<ErrorAlert message={errorMsg} onRetry={runAnalysis} />
{/if}

{#if analysisState === 'complete' && result}
	<div style="display: flex; flex-direction: column; gap: 1.5rem;">
		<!-- Score card -->
		<div class="card">
			<p style="font-size: 0.8125rem; font-weight: 500; color: var(--text-muted); margin: 0 0 0.75rem; text-transform: uppercase; letter-spacing: 0.05em;">Competition Score</p>
			<ScoreDisplay score={result.competitionScore} />
			<div style="display: flex; gap: 0.5rem; margin-top: 1rem; flex-wrap: wrap;">
				{#if result.trendDirection}
					<Badge variant={trendVariant(result.trendDirection)}>{result.trendDirection}</Badge>
				{/if}
				{#if result.keywordIntent}
					<Badge variant="info">{result.keywordIntent}</Badge>
				{/if}
			</div>
		</div>

		<!-- LLM insights -->
		{#if result.llmInsights}
			<TierGate feature="AI Insights" locked={!$isCreator}>
				<div class="card">
					<p style="font-size: 0.8125rem; font-weight: 500; color: var(--text-muted); margin: 0 0 0.75rem; text-transform: uppercase; letter-spacing: 0.05em;">AI Insights</p>
					<p style="font-size: 0.875rem; line-height: 1.6; margin: 0; white-space: pre-wrap;">{result.llmInsights}</p>
				</div>
			</TierGate>
		{/if}

		<div style="display: flex; justify-content: flex-end;">
			<button class="btn btn-ghost" onclick={reset}>New Analysis</button>
		</div>
	</div>
{/if}
