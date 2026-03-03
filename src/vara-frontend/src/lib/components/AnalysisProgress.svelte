<script lang="ts">
	let { step, stage, percent }: { step: number; stage: string; percent: number } = $props();
</script>

<div class="card" style="display: flex; flex-direction: column; gap: 1rem;">
	<div style="display: flex; justify-content: space-between; align-items: center;">
		<span style="font-size: 0.875rem; font-weight: 500;">
			Step {step} — {stage}
		</span>
		<span style="font-family: var(--font-mono); font-size: 0.875rem; color: var(--text-muted);">
			{percent}%
		</span>
	</div>
	<div style="height: 6px; background: var(--surface-3); border-radius: var(--radius-full); overflow: hidden;">
		<div
			style="height: 100%; background: var(--primary); border-radius: var(--radius-full); width: {percent}%; transition: width 0.5s ease;"
		></div>
	</div>
</div>
