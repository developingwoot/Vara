import { writable } from 'svelte/store';

export interface RecentAnalysis {
	id: string;
	type: string;
	query: string;
	score?: number;
	completedAt: Date;
}

export const recentAnalyses = writable<RecentAnalysis[]>([]);

export function addRecent(entry: RecentAnalysis) {
	recentAnalyses.update((list) => [entry, ...list].slice(0, 5));
}
