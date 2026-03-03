<script lang="ts">
	import { addRecent } from '$lib/stores/analysis';
	import { ensureConnected } from '$lib/api/signalr';
	import AnalysisProgress from '$lib/components/AnalysisProgress.svelte';
	import ErrorAlert from '$lib/components/ErrorAlert.svelte';
	import Badge from '$lib/components/Badge.svelte';
	import EmptyState from '$lib/components/EmptyState.svelte';

	type State = 'idle' | 'running' | 'complete' | 'error';

	let analysisState: State = $state('idle');
	let niche = $state('');
	let step = $state(1);
	let stage = $state('');
	let percent = $state(0);
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	let result: any = $state(null);
	let errorMsg = $state('');

	async function runAnalysis() {
		analysisState = 'running';
		step = 1;
		stage = 'Starting...';
		percent = 5;
		errorMsg = '';

		try {
			const hub = await ensureConnected();

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
					type: 'trends',
					query: niche || 'All niches',
					completedAt: new Date()
				});
			});

			hub.on('AnalysisError', (err: any) => {
				errorMsg = err.message;
				analysisState = 'error';
			});

			await hub.invoke('StartAnalysis', {
				type: 'trend',
				keyword: null,
				niche: niche.trim() || null
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
</script>

<div class="page-header">
	<h1 class="page-title">Trend Detection</h1>
	<p class="page-subtitle">Find rising, declining, and emerging keywords in any niche</p>
</div>

{#if analysisState === 'idle' || analysisState === 'complete' || analysisState === 'error'}
	<div class="card" style="margin-bottom: 1.5rem;">
		<form
			onsubmit={(e) => { e.preventDefault(); runAnalysis(); }}
			style="display: flex; align-items: flex-end; gap: 1rem;"
		>
			<div class="form-group" style="flex: 1;">
				<label class="label" for="niche">Niche <span style="color: var(--text-subtle);">(optional)</span></label>
				<input
					id="niche"
					class="input"
					type="text"
					bind:value={niche}
					placeholder="e.g. Personal Finance"
				/>
			</div>
			<button class="btn btn-primary" type="submit" style="flex-shrink: 0;">
				Detect Trends
			</button>
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
	<div style="display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 1.5rem;">
		<!-- Rising -->
		<div class="card" style="padding: 0;">
			<div style="padding: 1rem 1.25rem; border-bottom: 1px solid var(--border); display: flex; align-items: center; gap: 0.5rem;">
				<span>📈</span>
				<h2 style="font-size: 0.875rem; font-weight: 600; margin: 0;">Rising</h2>
				<Badge variant="success">{result.rising?.length ?? 0}</Badge>
			</div>
			{#if !result.rising?.length}
				<EmptyState icon="📊" headline="No rising trends" />
			{:else}
				<div style="padding: 0.5rem 0;">
					{#each result.rising as kw}
						<div style="padding: 0.625rem 1.25rem; display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid var(--border-subtle);">
							<span style="font-size: 0.8125rem; font-weight: 500;">{kw.keyword}</span>
							<div style="display: flex; flex-direction: column; align-items: flex-end; gap: 0.25rem;">
								<Badge variant={growthVariant(kw.growthRate)}>{formatGrowth(kw.growthRate)}</Badge>
								<span style="font-family: var(--font-mono); font-size: 0.6875rem; color: var(--text-muted);">{kw.momentumScore ?? ''}</span>
							</div>
						</div>
					{/each}
				</div>
			{/if}
		</div>

		<!-- Declining -->
		<div class="card" style="padding: 0;">
			<div style="padding: 1rem 1.25rem; border-bottom: 1px solid var(--border); display: flex; align-items: center; gap: 0.5rem;">
				<span>📉</span>
				<h2 style="font-size: 0.875rem; font-weight: 600; margin: 0;">Declining</h2>
				<Badge variant="danger">{result.declining?.length ?? 0}</Badge>
			</div>
			{#if !result.declining?.length}
				<EmptyState icon="📊" headline="No declining trends" />
			{:else}
				<div style="padding: 0.5rem 0;">
					{#each result.declining as kw}
						<div style="padding: 0.625rem 1.25rem; display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid var(--border-subtle);">
							<span style="font-size: 0.8125rem; font-weight: 500;">{kw.keyword}</span>
							<div style="display: flex; flex-direction: column; align-items: flex-end; gap: 0.25rem;">
								<Badge variant={growthVariant(kw.growthRate)}>{formatGrowth(kw.growthRate)}</Badge>
								<span style="font-family: var(--font-mono); font-size: 0.6875rem; color: var(--text-muted);">{kw.momentumScore ?? ''}</span>
							</div>
						</div>
					{/each}
				</div>
			{/if}
		</div>

		<!-- Emerging / New -->
		<div class="card" style="padding: 0;">
			<div style="padding: 1rem 1.25rem; border-bottom: 1px solid var(--border); display: flex; align-items: center; gap: 0.5rem;">
				<span>✨</span>
				<h2 style="font-size: 0.875rem; font-weight: 600; margin: 0;">Emerging</h2>
				<Badge variant="info">{result.emerging?.length ?? result.newKeywords?.length ?? 0}</Badge>
			</div>
			{#if !(result.emerging?.length || result.newKeywords?.length)}
				<EmptyState icon="📊" headline="No emerging trends" />
			{:else}
				<div style="padding: 0.5rem 0;">
					{#each (result.emerging ?? result.newKeywords ?? []) as kw}
						<div style="padding: 0.625rem 1.25rem; display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid var(--border-subtle);">
							<span style="font-size: 0.8125rem; font-weight: 500;">{kw.keyword}</span>
							<div style="display: flex; flex-direction: column; align-items: flex-end; gap: 0.25rem;">
								<Badge variant={growthVariant(kw.growthRate)}>{formatGrowth(kw.growthRate)}</Badge>
								<span style="font-family: var(--font-mono); font-size: 0.6875rem; color: var(--text-muted);">{kw.momentumScore ?? ''}</span>
							</div>
						</div>
					{/each}
				</div>
			{/if}
		</div>
	</div>

	<div style="display: flex; justify-content: flex-end; margin-top: 1.5rem;">
		<button class="btn btn-ghost" onclick={reset}>New Search</button>
	</div>
{/if}
