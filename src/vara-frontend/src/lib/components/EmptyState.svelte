<script lang="ts">
	let {
		icon,
		headline,
		cta,
		onAction
	}: { icon: string; headline: string; cta?: string; onAction?: () => void } = $props();
</script>

<div
	style="display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 4rem 2rem; gap: 1rem; text-align: center;"
>
	<div style="font-size: 3rem; line-height: 1;">{icon}</div>
	<p style="font-size: 1rem; font-weight: 500; color: var(--text-muted); margin: 0;">{headline}</p>
	{#if cta && onAction}
		<button class="btn btn-primary" onclick={onAction}>{cta}</button>
	{/if}
</div>
