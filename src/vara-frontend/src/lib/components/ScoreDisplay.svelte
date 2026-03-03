<script lang="ts">
	import Badge from './Badge.svelte';

	let { score }: { score: number } = $props();

	const variant = $derived(score >= 80 ? 'success' : score >= 50 ? 'warning' : 'danger');
	const label = $derived(score >= 80 ? 'Strong' : score >= 50 ? 'Moderate' : 'Competitive');
</script>

<div style="display: flex; align-items: center; gap: 1rem;">
	<span style="font-family: var(--font-mono); font-size: 2.5rem; font-weight: 500; color: var(--text);">
		{score}
		<span style="font-size: 1.25rem; color: var(--text-muted);">/ 100</span>
	</span>
	<Badge {variant}>{label}</Badge>
</div>
