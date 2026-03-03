<script lang="ts">
	let { feature, locked, children }: { feature: string; locked: boolean; children?: any } = $props();
</script>

<div style="position: relative;">
	<div style={locked ? 'opacity: 0.5; pointer-events: none; user-select: none;' : ''}>
		{@render children?.()}
	</div>
	{#if locked}
		<div
			style="position: absolute; inset: 0; display: flex; align-items: center; justify-content: center; opacity: 0; transition: opacity 0.15s ease; background: oklch(0 0 0 / 0.4); border-radius: var(--radius);"
			onmouseenter={(e) => ((e.currentTarget as HTMLElement).style.opacity = '1')}
			onmouseleave={(e) => ((e.currentTarget as HTMLElement).style.opacity = '0')}
			role="presentation"
		>
			<div style="text-align: center; color: white;">
				<div style="font-size: 1.25rem; margin-bottom: 0.25rem;">🔒</div>
				<div style="font-size: 0.75rem; font-weight: 500;">{feature} requires Creator tier</div>
			</div>
		</div>
	{/if}
</div>
