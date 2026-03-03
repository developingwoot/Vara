<script lang="ts">
	let { children } = $props();
</script>

<div style="min-height: 100vh; display: flex; align-items: center; justify-content: center; background: var(--background); padding: 1rem;">
	<div style="width: 100%; max-width: 420px;">
		<div style="text-align: center; margin-bottom: 2rem;">
			<div style="font-size: 1.5rem; font-weight: 700; letter-spacing: -0.03em;">▲ VARA</div>
			<p style="font-size: 0.875rem; color: var(--text-muted); margin: 0.5rem 0 0;">YouTube intelligence platform</p>
		</div>
		{@render children()}
	</div>
</div>
