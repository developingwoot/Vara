<script lang="ts">
	import { page } from '$app/stores';
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { fetchApi } from '$lib/api/client';
	import { isCreator } from '$lib/stores/auth';
	import Badge from '$lib/components/Badge.svelte';
	import TierGate from '$lib/components/TierGate.svelte';
	import ErrorAlert from '$lib/components/ErrorAlert.svelte';

	interface MetricComparison {
		recentAvg: number;
		topAvg: number;
		gapPercent: number;
		trend: 'above' | 'on-par' | 'below';
	}

	interface PostingStats {
		postsPerMonth: number;
		lastUploadDate?: string;
		daysSinceLastUpload: number;
		consistency: 'regular' | 'irregular' | 'inactive';
	}

	interface Priority {
		rank: number;
		severity: 'critical' | 'improve' | 'maintain';
		title: string;
		description: string;
	}

	interface Badge_ {
		id: string;
		name: string;
		description: string;
		category: 'achievement' | 'performance';
		tier: 'bronze' | 'silver' | 'gold';
		icon: string;
		earned: boolean;
	}

	interface VideoSnapshot {
		youtubeId: string;
		title: string;
		viewCount: number;
		likeCount: number;
		commentCount: number;
		durationSeconds?: number;
		uploadDate?: string;
		thumbnailUrl?: string;
		engagementRate: number;
	}

	interface QuickScan {
		channelId: string;
		channelName: string;
		hasVideos: boolean;
		isSynced: boolean;
		totalVideos: number;
		overallScore: number;
		varaAssessment: string;
		viewsComparison: MetricComparison;
		engagementComparison: MetricComparison;
		postingStats: PostingStats;
		priorities: Priority[];
		badges: Badge_[];
		recentVideos: VideoSnapshot[];
		topVideos: VideoSnapshot[];
		generatedAt: string;
	}

	interface DeepAuditResult {
		channelId: string;
		recentVideoTitle?: string;
		topVideoTitle?: string;
		transcriptsAvailable: boolean;
		llmAnalysis?: string;
		generatedAt: string;
	}

	const channelId = $page.params.id;

	let scan: QuickScan | null = $state(null);
	let loading = $state(true);
	let error = $state('');

	let deepAudit: DeepAuditResult | null = $state(null);
	let deepAuditLoading = $state(false);
	let deepAuditError = $state('');

	let channelName = $state('');
	let failedThumbnails = $state(new Set<string>());

	async function loadQuickScan() {
		loading = true;
		error = '';
		try {
			scan = await fetchApi<QuickScan>(`/channels/${channelId}/quick-scan`);
			channelName = scan.channelName ?? 'Channel';
		} catch (e: any) {
			error = e.message ?? 'Failed to load channel audit';
		} finally {
			loading = false;
		}
	}

	async function runDeepAudit() {
		deepAuditLoading = true;
		deepAuditError = '';
		try {
			deepAudit = await fetchApi<DeepAuditResult>(`/channels/${channelId}/deep-audit`, { method: 'POST' });
		} catch (e: any) {
			deepAuditError = e.message ?? 'Deep audit failed';
		} finally {
			deepAuditLoading = false;
		}
	}

	function scoreColor(score: number) {
		if (score >= 75) return 'var(--success)';
		if (score >= 50) return 'var(--warning)';
		return 'var(--danger)';
	}

	function scoreBadgeTier(score: number) {
		if (score >= 75) return 'gold';
		if (score >= 50) return 'silver';
		return 'bronze';
	}

	function trendIcon(trend: string) {
		if (trend === 'above') return '✓';
		if (trend === 'on-par') return '~';
		return '▼';
	}

	function trendColor(trend: string) {
		if (trend === 'above') return 'var(--success)';
		if (trend === 'on-par') return 'var(--warning)';
		return 'var(--danger)';
	}

	function severityColor(severity: string) {
		if (severity === 'critical') return 'var(--danger)';
		if (severity === 'improve') return 'var(--warning)';
		return 'var(--success)';
	}

	function severityIcon(severity: string) {
		if (severity === 'critical') return '🔴';
		if (severity === 'improve') return '🟡';
		return '🟢';
	}

	function badgeTierColor(tier: string) {
		if (tier === 'gold') return '#f59e0b';
		if (tier === 'silver') return '#9ca3af';
		return '#cd7c2e';
	}

	function formatNum(n: number) {
		if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
		if (n >= 1_000) return `${(n / 1_000).toFixed(0)}K`;
		return Math.round(n).toLocaleString();
	}

	function formatDate(dateStr?: string) {
		if (!dateStr) return '—';
		return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
	}

	function formatDuration(secs?: number) {
		if (!secs) return '—';
		const m = Math.floor(secs / 60);
		const s = secs % 60;
		return `${m}:${s.toString().padStart(2, '0')}`;
	}

	onMount(loadQuickScan);
</script>

<!-- Back nav -->
<div style="margin-bottom: 1.25rem;">
	<button class="btn btn-ghost" style="font-size: 0.8125rem; padding: 0.375rem 0.75rem;" onclick={() => goto('/channels')}>
		← Back to Channels
	</button>
</div>

{#if loading}
	<div style="display: flex; flex-direction: column; gap: 1rem;">
		{#each [1, 2, 3, 4] as _}
			<div class="skeleton" style="height: 100px; border-radius: var(--radius);"></div>
		{/each}
	</div>
{:else if error}
	<ErrorAlert message={error} onRetry={loadQuickScan} />
{:else if scan}
	<!-- Page header -->
	<div class="page-header" style="display: flex; align-items: flex-start; justify-content: space-between; flex-wrap: wrap; gap: 1rem;">
		<div>
			<h1 class="page-title">{scan.channelName ?? 'Channel Audit'}</h1>
			<p class="page-subtitle">{scan.totalVideos} videos · {scan.postingStats.postsPerMonth.toFixed(1)}/month</p>
		</div>
		{#if scan.hasVideos && scan.isSynced}
			<button class="btn btn-ghost" style="font-size: 0.8125rem;" onclick={() => goto(`/channels/${channelId}/compare`)}>
				Compare Videos →
			</button>
		{/if}
	</div>

	{#if !scan.isSynced || !scan.hasVideos}
		<!-- Not yet synced -->
		<div class="card" style="text-align: center; padding: 2.5rem 2rem;">
			<div style="font-size: 2rem; margin-bottom: 0.75rem;">📊</div>
			<h2 style="font-size: 1rem; font-weight: 600; margin: 0 0 0.5rem;">Sync your channel to unlock the audit</h2>
			<p style="font-size: 0.875rem; color: var(--text-muted); margin: 0 0 1.25rem;">VARA needs your video library to calculate your performance score, badges, and priorities.</p>
			<button class="btn btn-primary" onclick={() => goto('/channels')}>Go to Channels →</button>
		</div>
	{:else}
		<!-- ── VARA's Assessment ── -->
		<div class="card" style="margin-bottom: 1.25rem;">
			<div style="display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; flex-wrap: wrap;">
				<div style="flex: 1; min-width: 200px;">
					<div style="font-size: 0.75rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.06em; color: var(--text-muted); margin-bottom: 0.5rem;">
						VARA's Assessment
					</div>
					<p style="font-size: 0.9375rem; line-height: 1.6; color: var(--text); margin: 0;">
						{scan.varaAssessment}
					</p>
				</div>
				<div style="text-align: center; flex-shrink: 0;">
					<div style="font-size: 2.5rem; font-weight: 700; font-family: var(--font-mono); color: {scoreColor(scan.overallScore)}; line-height: 1;">
						{scan.overallScore}
					</div>
					<div style="font-size: 0.6875rem; text-transform: uppercase; letter-spacing: 0.06em; color: var(--text-muted); margin-top: 0.25rem;">
						/ 100
					</div>
					<div style="margin-top: 0.5rem;">
						<span style="font-size: 0.75rem; font-weight: 600; padding: 0.25rem 0.625rem; border-radius: 9999px; background: {badgeTierColor(scoreBadgeTier(scan.overallScore))}22; color: {badgeTierColor(scoreBadgeTier(scan.overallScore))}; border: 1px solid {badgeTierColor(scoreBadgeTier(scan.overallScore))}44; text-transform: capitalize;">
							{scoreBadgeTier(scan.overallScore)}
						</span>
					</div>
				</div>
			</div>
		</div>

		<!-- ── Badges ── -->
		{#if scan.badges.length > 0}
			<div class="card" style="margin-bottom: 1.25rem;">
				<div style="font-size: 0.75rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.06em; color: var(--text-muted); margin-bottom: 0.875rem;">
					Badges & Awards
				</div>
				<div style="display: flex; flex-wrap: wrap; gap: 0.75rem;">
					{#each scan.badges as badge}
						<div style="display: flex; align-items: center; gap: 0.5rem; padding: 0.5rem 0.875rem; border-radius: var(--radius); background: var(--surface-2); border: 1px solid var(--border-subtle);" title={badge.description}>
							<span style="font-size: 1.125rem;">{badge.icon}</span>
							<div>
								<div style="font-size: 0.8125rem; font-weight: 600; line-height: 1.2;">{badge.name}</div>
								<div style="font-size: 0.6875rem; color: {badgeTierColor(badge.tier)}; text-transform: capitalize; font-weight: 500;">{badge.tier}</div>
							</div>
						</div>
					{/each}
				</div>
			</div>
		{/if}

		<!-- ── Quick Scan metrics ── -->
		<div class="card" style="margin-bottom: 1.25rem; padding: 0;">
			<div style="padding: 1rem 1.25rem; border-bottom: 1px solid var(--border);">
				<div style="font-size: 0.75rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.06em; color: var(--text-muted);">
					Quick Scan — Recent vs Your Best
				</div>
			</div>
			<div style="overflow-x: auto;">
				<table style="width: 100%; border-collapse: collapse; font-size: 0.875rem;">
					<thead>
						<tr style="border-bottom: 1px solid var(--border-subtle);">
							<th style="padding: 0.75rem 1.25rem; text-align: left; font-size: 0.75rem; font-weight: 600; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.04em;">Metric</th>
							<th style="padding: 0.75rem 1rem; text-align: right; font-size: 0.75rem; font-weight: 600; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.04em;">Recent Avg (last 5)</th>
							<th style="padding: 0.75rem 1rem; text-align: right; font-size: 0.75rem; font-weight: 600; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.04em;">Your Best (top 5)</th>
							<th style="padding: 0.75rem 1.25rem; text-align: right; font-size: 0.75rem; font-weight: 600; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.04em;">Gap</th>
						</tr>
					</thead>
					<tbody>
						<tr style="border-bottom: 1px solid var(--border-subtle);">
							<td style="padding: 0.875rem 1.25rem; font-weight: 500;">Views</td>
							<td style="padding: 0.875rem 1rem; text-align: right; font-family: var(--font-mono);">{formatNum(scan.viewsComparison.recentAvg)}</td>
							<td style="padding: 0.875rem 1rem; text-align: right; font-family: var(--font-mono);">{formatNum(scan.viewsComparison.topAvg)}</td>
							<td style="padding: 0.875rem 1.25rem; text-align: right;">
								<span style="color: {trendColor(scan.viewsComparison.trend)}; font-weight: 600; font-family: var(--font-mono);">
									{trendIcon(scan.viewsComparison.trend)}
									{Math.abs(scan.viewsComparison.gapPercent).toFixed(0)}%
								</span>
							</td>
						</tr>
						<tr style="border-bottom: 1px solid var(--border-subtle);">
							<td style="padding: 0.875rem 1.25rem; font-weight: 500;">Engagement Rate</td>
							<td style="padding: 0.875rem 1rem; text-align: right; font-family: var(--font-mono);">{scan.engagementComparison.recentAvg.toFixed(2)}%</td>
							<td style="padding: 0.875rem 1rem; text-align: right; font-family: var(--font-mono);">{scan.engagementComparison.topAvg.toFixed(2)}%</td>
							<td style="padding: 0.875rem 1.25rem; text-align: right;">
								<span style="color: {trendColor(scan.engagementComparison.trend)}; font-weight: 600; font-family: var(--font-mono);">
									{trendIcon(scan.engagementComparison.trend)}
									{Math.abs(scan.engagementComparison.gapPercent).toFixed(0)}%
								</span>
							</td>
						</tr>
						<tr>
							<td style="padding: 0.875rem 1.25rem; font-weight: 500;">Upload Frequency</td>
							<td style="padding: 0.875rem 1rem; text-align: right; font-family: var(--font-mono);" colspan="2">
								{scan.postingStats.postsPerMonth.toFixed(1)}/month
								<span style="font-size: 0.75rem; color: var(--text-muted); margin-left: 0.5rem; font-family: inherit;">
									({scan.postingStats.consistency})
								</span>
							</td>
							<td style="padding: 0.875rem 1.25rem; text-align: right;">
								{#if scan.postingStats.daysSinceLastUpload > 60}
									<span style="color: var(--danger); font-weight: 600; font-size: 0.8125rem;">
										{scan.postingStats.daysSinceLastUpload}d since last post
									</span>
								{:else if scan.postingStats.daysSinceLastUpload > 0}
									<span style="color: var(--text-muted); font-size: 0.8125rem;">
										Last: {formatDate(scan.postingStats.lastUploadDate)}
									</span>
								{/if}
							</td>
						</tr>
					</tbody>
				</table>
			</div>
		</div>

		<!-- ── Priorities ── -->
		{#if scan.priorities.length > 0}
			<div class="card" style="margin-bottom: 1.25rem;">
				<div style="font-size: 0.75rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.06em; color: var(--text-muted); margin-bottom: 0.875rem;">
					What to Focus on First
				</div>
				<div style="display: flex; flex-direction: column; gap: 0.75rem;">
					{#each scan.priorities as priority}
						<div style="display: flex; gap: 0.875rem; padding: 0.875rem 1rem; border-radius: var(--radius); background: var(--surface-2); border-left: 3px solid {severityColor(priority.severity)};">
							<span style="font-size: 1rem; flex-shrink: 0;">{severityIcon(priority.severity)}</span>
							<div>
								<div style="font-size: 0.875rem; font-weight: 600; margin-bottom: 0.2rem;">{priority.title}</div>
								<div style="font-size: 0.8125rem; color: var(--text-muted); line-height: 1.5;">{priority.description}</div>
							</div>
						</div>
					{/each}
				</div>
			</div>
		{/if}

		<!-- ── Recent vs Top videos ── -->
		<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-bottom: 1.25rem;">
			<!-- Recent videos -->
			<div class="card" style="padding: 0;">
				<div style="padding: 0.875rem 1.25rem; border-bottom: 1px solid var(--border);">
					<div style="font-size: 0.75rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.06em; color: var(--text-muted);">Recent Videos</div>
				</div>
				{#each scan.recentVideos as video}
					<a
						href="/channels/{channelId}/videos/{video.youtubeId}"
						style="display: flex; gap: 0.75rem; padding: 0.75rem 1.25rem; border-bottom: 1px solid var(--border-subtle); text-decoration: none; color: inherit; transition: background 0.1s;"
						onmouseover={(e) => (e.currentTarget.style.background = 'var(--surface-2)')}
						onmouseleave={(e) => (e.currentTarget.style.background = '')}
					>
						{#if video.thumbnailUrl && !failedThumbnails.has(video.youtubeId)}
							<img
								src={video.thumbnailUrl}
								alt={video.title}
								style="width: 56px; height: 36px; object-fit: cover; border-radius: 4px; flex-shrink: 0;"
								onerror={() => { failedThumbnails = new Set([...failedThumbnails, video.youtubeId]); }}
							/>
						{:else}
							<div style="width: 56px; height: 36px; background: var(--surface-3); border-radius: 4px; flex-shrink: 0;"></div>
						{/if}
						<div style="flex: 1; min-width: 0;">
							<div style="font-size: 0.8125rem; font-weight: 500; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; margin-bottom: 0.2rem;">{video.title}</div>
							<div style="font-size: 0.75rem; color: var(--text-muted); font-family: var(--font-mono);">
								{formatNum(video.viewCount)} views · {video.engagementRate.toFixed(1)}% eng
							</div>
						</div>
					</a>
				{/each}
			</div>

			<!-- Top videos -->
			<div class="card" style="padding: 0;">
				<div style="padding: 0.875rem 1.25rem; border-bottom: 1px solid var(--border);">
					<div style="font-size: 0.75rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.06em; color: var(--text-muted);">Top Performing Videos</div>
				</div>
				{#each scan.topVideos as video}
					<a
						href="/channels/{channelId}/videos/{video.youtubeId}"
						style="display: flex; gap: 0.75rem; padding: 0.75rem 1.25rem; border-bottom: 1px solid var(--border-subtle); text-decoration: none; color: inherit; transition: background 0.1s;"
						onmouseover={(e) => (e.currentTarget.style.background = 'var(--surface-2)')}
						onmouseleave={(e) => (e.currentTarget.style.background = '')}
					>
						{#if video.thumbnailUrl && !failedThumbnails.has(video.youtubeId + '-top')}
							<img
								src={video.thumbnailUrl}
								alt={video.title}
								style="width: 56px; height: 36px; object-fit: cover; border-radius: 4px; flex-shrink: 0;"
								onerror={() => { failedThumbnails = new Set([...failedThumbnails, video.youtubeId + '-top']); }}
							/>
						{:else}
							<div style="width: 56px; height: 36px; background: var(--surface-3); border-radius: 4px; flex-shrink: 0;"></div>
						{/if}
						<div style="flex: 1; min-width: 0;">
							<div style="font-size: 0.8125rem; font-weight: 500; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; margin-bottom: 0.2rem;">{video.title}</div>
							<div style="font-size: 0.75rem; color: var(--text-muted); font-family: var(--font-mono);">
								{formatNum(video.viewCount)} views · {video.engagementRate.toFixed(1)}% eng
							</div>
						</div>
					</a>
				{/each}
			</div>
		</div>

		<!-- ── Deep Dive ── -->
		<div class="card" style="margin-bottom: 1.25rem;">
			<div style="display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; flex-wrap: wrap; margin-bottom: {deepAudit || deepAuditLoading ? '1rem' : '0'};">
				<div>
					<div style="display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.375rem;">
						<span style="font-size: 0.75rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.06em; color: var(--text-muted);">Deep Dive</span>
						<Badge variant="creator">Creator</Badge>
					</div>
					<p style="font-size: 0.875rem; color: var(--text-muted); margin: 0; line-height: 1.5;">
						AI compares your most recent video against your top outlier — pinpointing differences in pacing, storytelling, hooks, and CTAs.
					</p>
				</div>
				{#if $isCreator && !deepAudit}
					<button
						class="btn btn-primary"
						style="flex-shrink: 0;"
						onclick={runDeepAudit}
						disabled={deepAuditLoading}
					>
						{deepAuditLoading ? 'Analysing...' : 'Run Deep Dive'}
					</button>
				{:else if !$isCreator}
					<TierGate />
				{/if}
			</div>

			{#if deepAuditError}
				<div style="background: var(--danger-muted); border: 1px solid var(--danger); border-radius: var(--radius); padding: 0.75rem 1rem; font-size: 0.875rem; color: var(--danger);">
					{deepAuditError}
				</div>
			{/if}

			{#if deepAuditLoading}
				<div style="display: flex; flex-direction: column; gap: 0.5rem;">
					{#each [1, 2, 3] as _}
						<div class="skeleton" style="height: 1rem; border-radius: 4px;"></div>
					{/each}
				</div>
			{/if}

			{#if deepAudit}
				{#if !deepAudit.transcriptsAvailable}
					<div style="background: var(--warning-muted, var(--surface-2)); border: 1px solid var(--border); border-radius: var(--radius); padding: 0.875rem 1rem; font-size: 0.875rem; color: var(--text-muted);">
						Transcripts were not available for these videos. Try again later or check if the videos have captions enabled.
					</div>
				{:else if deepAudit.llmAnalysis}
					<div style="border-top: 1px solid var(--border-subtle); padding-top: 1rem;">
						<div style="font-size: 0.75rem; color: var(--text-muted); margin-bottom: 0.75rem;">
							Comparing: <strong>{deepAudit.recentVideoTitle ?? 'Recent video'}</strong>  vs  <strong>{deepAudit.topVideoTitle ?? 'Top video'}</strong>
						</div>
						<div style="font-size: 0.875rem; line-height: 1.7; white-space: pre-wrap; color: var(--text);">
							{deepAudit.llmAnalysis}
						</div>
					</div>
				{/if}
			{/if}
		</div>
	{/if}
{/if}
