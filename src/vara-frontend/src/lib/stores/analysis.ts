import { writable } from 'svelte/store';
import { browser } from '$app/environment';

export interface RecentAnalysis {
	id: string;
	type: string;
	query: string;
	score?: number;
	completedAt: Date;
}

const STORAGE_KEY = 'vara_recent_analyses';

function loadFromStorage(): RecentAnalysis[] {
	if (!browser) return [];
	try {
		const raw = localStorage.getItem(STORAGE_KEY);
		if (!raw) return [];
		const parsed = JSON.parse(raw) as Array<RecentAnalysis & { completedAt: string }>;
		return parsed.map((e) => ({ ...e, completedAt: new Date(e.completedAt) }));
	} catch {
		return [];
	}
}

function createRecentAnalyses() {
	const { subscribe, update } = writable<RecentAnalysis[]>(loadFromStorage());

	return {
		subscribe,
		add(entry: RecentAnalysis) {
			update((list) => {
				const next = [entry, ...list].slice(0, 5);
				if (browser) localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
				return next;
			});
		}
	};
}

export const recentAnalyses = createRecentAnalyses();

export function addRecent(entry: RecentAnalysis) {
	recentAnalyses.add(entry);
}
