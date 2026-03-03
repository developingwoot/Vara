<script lang="ts">
	import { auth } from '$lib/stores/auth';
	import { recentAnalyses } from '$lib/stores/analysis';
	import Badge from '$lib/components/Badge.svelte';
	import EmptyState from '$lib/components/EmptyState.svelte';

	const cards = [
		{
			href: '/analyze/keyword',
			title: 'Keyword Analysis',
			description: 'Discover keyword competition scores and content opportunities.',
			icon: '🔍',
			available: true
		},
		{
			href: '/analyze/trends',
			title: 'Trend Detection',
			description: 'Find rising, declining, and emerging keywords in any niche.',
			icon: '📈',
			available: true
		},
		{
			href: null,
			title: 'Video Analysis',
			description: 'Deep-dive transcript analysis with AI insights.',
			icon: '🎬',
			available: false
		},
		{
			href: null,
			title: 'Niche Compare',
			description: 'Compare opportunities across multiple niches side-by-side.',
			icon: '⚖️',
			available: false
		}
	];

	function timeAgo(date: Date) {
		const diffMs = Date.now() - new Date(date).getTime();
		const diffMin = Math.floor(diffMs / 60000);
		if (diffMin < 1) return 'just now';
		if (diffMin < 60) return `${diffMin}m ago`;
		const diffHr = Math.floor(diffMin / 60);
		if (diffHr < 24) return `${diffHr}h ago`;
		return `${Math.floor(diffHr / 24)}d ago`;
	}
</script>

<div class="page-header">
	<h1 class="page-title">
		{#if $auth.user}
			Good day, {$auth.user.fullName?.split(' ')[0] || $auth.user.email}
		{:else}
			Dashboard
		{/if}
	</h1>
	<p class="page-subtitle">What would you like to analyze today?</p>
</div>

<!-- Analysis cards -->
<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-bottom: 2.5rem;">
	{#each cards as card}
		{#if card.available && card.href}
			<a
				href={card.href}
				class="card"
				style="text-decoration: none; color: inherit; display: block; transition: border-color 0.15s ease; cursor: pointer;"
			>
				<div style="display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 0.75rem;">
					<span style="font-size: 1.5rem;">{card.icon}</span>
				</div>
				<h2 style="font-size: 0.9375rem; font-weight: 600; margin: 0 0 0.375rem;">{card.title}</h2>
				<p style="font-size: 0.8125rem; color: var(--text-muted); margin: 0; line-height: 1.5;">{card.description}</p>
			</a>
		{:else}
			<div class="card" style="opacity: 0.5;">
				<div style="display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 0.75rem;">
					<span style="font-size: 1.5rem;">{card.icon}</span>
					<Badge variant="default">Coming soon</Badge>
				</div>
				<h2 style="font-size: 0.9375rem; font-weight: 600; margin: 0 0 0.375rem;">{card.title}</h2>
				<p style="font-size: 0.8125rem; color: var(--text-muted); margin: 0; line-height: 1.5;">{card.description}</p>
			</div>
		{/if}
	{/each}
</div>

<!-- Recent analyses -->
<div class="card" style="padding: 0;">
	<div style="padding: 1.25rem 1.5rem; border-bottom: 1px solid var(--border);">
		<h2 style="font-size: 0.9375rem; font-weight: 600; margin: 0;">Recent Analyses</h2>
	</div>

	{#if $recentAnalyses.length === 0}
		<EmptyState icon="📊" headline="No analyses yet — run your first one above" />
	{:else}
		<table>
			<thead>
				<tr>
					<th>Query</th>
					<th>Type</th>
					<th>Score</th>
					<th>Time</th>
				</tr>
			</thead>
			<tbody>
				{#each $recentAnalyses as item}
					<tr>
						<td style="font-weight: 500;">{item.query}</td>
						<td>
							<Badge variant="info">{item.type}</Badge>
						</td>
						<td>
							{#if item.score !== undefined}
								<span style="font-family: var(--font-mono);">{item.score}</span>
							{:else}
								<span style="color: var(--text-subtle);">—</span>
							{/if}
						</td>
						<td style="color: var(--text-muted); font-size: 0.8125rem;">{timeAgo(item.completedAt)}</td>
					</tr>
				{/each}
			</tbody>
		</table>
	{/if}
</div>
