<script lang="ts">
	let {
		message,
		onRetry
	}: { message: string; onRetry?: () => void } = $props();
</script>

<div
	style="background: var(--danger-muted); border: 1px solid var(--danger); border-radius: var(--radius); padding: 1rem 1.25rem; display: flex; align-items: center; justify-content: space-between; gap: 1rem;"
>
	<div style="display: flex; align-items: center; gap: 0.75rem;">
		<span style="color: var(--danger); font-size: 1rem;">⚠️</span>
		<span style="font-size: 0.875rem; color: var(--text);">{message}</span>
	</div>
	{#if onRetry}
		<button class="btn btn-ghost" style="font-size: 0.75rem; padding: 0.375rem 0.75rem;" onclick={onRetry}>
			Try again
		</button>
	{/if}
</div>
