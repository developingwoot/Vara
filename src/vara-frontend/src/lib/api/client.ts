import { get } from 'svelte/store';
import { goto } from '$app/navigation';
import { auth } from '$lib/stores/auth';

export async function fetchApi<T>(path: string, init?: RequestInit): Promise<T> {
	const token = get(auth).token;
	const res = await fetch(`/api${path}`, {
		...init,
		headers: {
			'Content-Type': 'application/json',
			...(token ? { Authorization: `Bearer ${token}` } : {}),
			...init?.headers
		}
	});

	if (res.status === 401) {
		auth.logout();
		goto('/login');
		throw new Error('Unauthorized');
	}

	if (!res.ok) {
		const err = await res.json().catch(() => ({ message: 'Request failed' }));
		throw new Error(err.message ?? 'Request failed');
	}

	if (res.status === 204) return undefined as T;
	return res.json();
}
