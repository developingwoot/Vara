<script lang="ts">
	type Variant = 'success' | 'warning' | 'danger' | 'info' | 'creator' | 'free' | 'default';

	let { variant = 'default', children }: { variant?: Variant; children?: any } = $props();

	const styles: Record<Variant, string> = {
		success: 'color: var(--success); background: var(--success-muted);',
		warning: 'color: var(--warning); background: var(--warning-muted);',
		danger: 'color: var(--danger); background: var(--danger-muted);',
		info: 'color: var(--info); background: var(--info-muted);',
		creator: 'color: var(--tier-creator); background: var(--tier-creator-bg);',
		free: 'color: var(--tier-free); background: var(--tier-free-bg);',
		default: 'color: var(--text-muted); background: var(--surface-3);'
	};
</script>

<span
	style="display: inline-flex; align-items: center; padding: 0.125rem 0.5rem; border-radius: var(--radius-full); font-size: 0.75rem; font-weight: 500; {styles[variant]}"
>
	{@render children?.()}
</span>
