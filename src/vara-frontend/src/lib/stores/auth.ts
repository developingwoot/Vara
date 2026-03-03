import { writable, derived } from 'svelte/store';
import { browser } from '$app/environment';

export interface User {
	id: string;
	email: string;
	fullName?: string;
	tier: 'free' | 'creator';
}

interface AuthState {
	token: string | null;
	refreshToken: string | null;
	user: User | null;
}

function createAuth() {
	const { subscribe, set, update } = writable<AuthState>({
		token: browser ? localStorage.getItem('vara_token') : null,
		refreshToken: browser ? localStorage.getItem('vara_refresh_token') : null,
		user: null
	});

	return {
		subscribe,
		login(token: string, refreshToken: string) {
			localStorage.setItem('vara_token', token);
			localStorage.setItem('vara_refresh_token', refreshToken);
			update((s) => ({ ...s, token, refreshToken }));
		},
		setUser(user: User) {
			update((s) => ({ ...s, user }));
		},
		logout() {
			localStorage.removeItem('vara_token');
			localStorage.removeItem('vara_refresh_token');
			set({ token: null, refreshToken: null, user: null });
		}
	};
}

export const auth = createAuth();
export const isAuthenticated = derived(auth, ($a) => !!$a.token);
export const isCreator = derived(auth, ($a) => $a.user?.tier === 'creator');
