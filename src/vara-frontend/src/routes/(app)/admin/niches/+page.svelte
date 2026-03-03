<script lang="ts">
	import { onMount } from 'svelte';
	import { fetchApi } from '$lib/api/client';
	import { isAdmin } from '$lib/stores/auth';
	import { goto } from '$app/navigation';
	import Badge from '$lib/components/Badge.svelte';
	import EmptyState from '$lib/components/EmptyState.svelte';

	interface CanonicalNiche {
		id: number;
		name: string;
		slug: string;
		aliases: string[];
		isActive: boolean;
	}

	let niches: CanonicalNiche[] = $state([]);
	let loading = $state(true);
	let error = $state('');

	// Add form
	let addName = $state('');
	let addSlug = $state('');
	let addAliases = $state('');
	let addError = $state('');
	let addSaving = $state(false);
	let addSuccess = $state(false);

	// Edit state
	let editId: number | null = $state(null);
	let editName = $state('');
	let editSlug = $state('');
	let editAliases = $state('');
	let editSaving = $state(false);
	let editError = $state('');

	onMount(async () => {
		if (!$isAdmin) {
			goto('/');
			return;
		}
		await loadNiches();
	});

	async function loadNiches() {
		loading = true;
		error = '';
		try {
			niches = await fetchApi<CanonicalNiche[]>('/admin/niches');
		} catch (err: any) {
			error = err.message ?? 'Failed to load niches';
		} finally {
			loading = false;
		}
	}

	function slugify(name: string) {
		return name.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
	}

	function onAddNameInput() {
		if (!addSlug || addSlug === slugify(addName.slice(0, -1))) {
			addSlug = slugify(addName);
		}
	}

	async function addNiche(e: SubmitEvent) {
		e.preventDefault();
		addSaving = true;
		addError = '';
		addSuccess = false;
		try {
			const created = await fetchApi<CanonicalNiche>('/admin/niches', {
				method: 'POST',
				body: JSON.stringify({
					name: addName.trim(),
					slug: addSlug.trim(),
					aliases: addAliases
						.split(',')
						.map((a) => a.trim())
						.filter(Boolean)
				})
			});
			niches = [...niches, created].sort((a, b) => a.name.localeCompare(b.name));
			addName = '';
			addSlug = '';
			addAliases = '';
			addSuccess = true;
			setTimeout(() => (addSuccess = false), 3000);
		} catch (err: any) {
			addError = err.message ?? 'Failed to create niche';
		} finally {
			addSaving = false;
		}
	}

	function startEdit(n: CanonicalNiche) {
		editId = n.id;
		editName = n.name;
		editSlug = n.slug;
		editAliases = n.aliases.join(', ');
		editError = '';
	}

	function cancelEdit() {
		editId = null;
	}

	async function saveEdit(id: number) {
		editSaving = true;
		editError = '';
		try {
			const updated = await fetchApi<CanonicalNiche>(`/admin/niches/${id}`, {
				method: 'PUT',
				body: JSON.stringify({
					name: editName.trim(),
					slug: editSlug.trim(),
					aliases: editAliases
						.split(',')
						.map((a) => a.trim())
						.filter(Boolean)
				})
			});
			niches = niches.map((n) => (n.id === id ? updated : n));
			editId = null;
		} catch (err: any) {
			editError = err.message ?? 'Failed to save';
		} finally {
			editSaving = false;
		}
	}

	async function deactivate(id: number) {
		try {
			await fetchApi(`/admin/niches/${id}`, { method: 'DELETE' });
			niches = niches.map((n) => (n.id === id ? { ...n, isActive: false } : n));
		} catch (err: any) {
			alert(err.message ?? 'Failed to deactivate');
		}
	}
</script>

<div class="page-header">
	<h1 class="page-title">Canonical Niches</h1>
	<p class="page-subtitle">Manage the reference list used for niche normalization</p>
</div>

<!-- Add niche -->
<div class="card" style="margin-bottom: 1.5rem;">
	<h2 style="font-size: 0.9375rem; font-weight: 600; margin: 0 0 1rem;">Add Niche</h2>

	{#if addSuccess}
		<div style="background: var(--success-muted); border: 1px solid var(--success); border-radius: var(--radius); padding: 0.75rem 1rem; margin-bottom: 1rem; font-size: 0.875rem; color: var(--success);">
			Niche created.
		</div>
	{/if}
	{#if addError}
		<div style="background: var(--danger-muted); border: 1px solid var(--danger); border-radius: var(--radius); padding: 0.75rem 1rem; margin-bottom: 1rem; font-size: 0.875rem; color: var(--danger);">
			{addError}
		</div>
	{/if}

	<form onsubmit={addNiche} style="display: flex; flex-direction: column; gap: 1rem;">
		<div style="display: flex; gap: 1rem; flex-wrap: wrap;">
			<div class="form-group" style="flex: 1; min-width: 150px;">
				<label class="label" for="addName">Name <span style="color: var(--danger);">*</span></label>
				<input id="addName" class="input" type="text" bind:value={addName} oninput={onAddNameInput} placeholder="Personal Finance" required />
			</div>
			<div class="form-group" style="flex: 1; min-width: 150px;">
				<label class="label" for="addSlug">Slug <span style="color: var(--danger);">*</span></label>
				<input id="addSlug" class="input" type="text" bind:value={addSlug} placeholder="personal-finance" required />
				<span class="helper-text">Unique lowercase identifier</span>
			</div>
		</div>
		<div class="form-group">
			<label class="label" for="addAliases">Aliases</label>
			<input id="addAliases" class="input" type="text" bind:value={addAliases} placeholder="finance, money, budgeting, investing" />
			<span class="helper-text">Comma-separated alternative phrasings for fuzzy matching</span>
		</div>
		<div style="display: flex; justify-content: flex-end;">
			<button class="btn btn-primary" type="submit" disabled={addSaving}>
				{addSaving ? 'Creating...' : 'Create Niche'}
			</button>
		</div>
	</form>
</div>

<!-- Niche table -->
<div class="card" style="padding: 0;">
	<div style="padding: 1.25rem 1.5rem; border-bottom: 1px solid var(--border);">
		<h2 style="font-size: 0.9375rem; font-weight: 600; margin: 0;">All Niches</h2>
	</div>

	{#if loading}
		<div style="padding: 1.5rem; display: flex; flex-direction: column; gap: 0.75rem;">
			{#each [1, 2, 3] as _}
				<div class="skeleton" style="height: 48px; border-radius: var(--radius);"></div>
			{/each}
		</div>
	{:else if error}
		<div style="padding: 1.5rem; color: var(--danger); font-size: 0.875rem;">{error}</div>
	{:else if niches.length === 0}
		<EmptyState icon="🏷️" headline="No niches yet" cta="Use the form above to create the first one" />
	{:else}
		{#each niches as niche}
			<div style="padding: 1rem 1.5rem; border-bottom: 1px solid var(--border-subtle);">
				{#if editId === niche.id}
					<!-- Edit row -->
					<div style="display: flex; flex-direction: column; gap: 0.75rem;">
						{#if editError}
							<div style="font-size: 0.8125rem; color: var(--danger);">{editError}</div>
						{/if}
						<div style="display: flex; gap: 0.75rem; flex-wrap: wrap;">
							<input class="input" type="text" bind:value={editName} placeholder="Name" style="flex: 1; min-width: 120px;" />
							<input class="input" type="text" bind:value={editSlug} placeholder="slug" style="flex: 1; min-width: 120px;" />
						</div>
						<input class="input" type="text" bind:value={editAliases} placeholder="alias1, alias2, alias3" />
						<div style="display: flex; gap: 0.5rem; justify-content: flex-end;">
							<button class="btn btn-ghost" style="font-size: 0.8125rem;" onclick={cancelEdit}>Cancel</button>
							<button class="btn btn-primary" style="font-size: 0.8125rem;" disabled={editSaving} onclick={() => saveEdit(niche.id)}>
								{editSaving ? 'Saving...' : 'Save'}
							</button>
						</div>
					</div>
				{:else}
					<!-- Display row -->
					<div style="display: flex; align-items: flex-start; gap: 1rem;">
						<div style="flex: 1; min-width: 0;">
							<div style="display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap;">
								<span style="font-size: 0.875rem; font-weight: 500;">{niche.name}</span>
								<code style="font-size: 0.75rem; color: var(--text-muted); font-family: var(--font-mono);">{niche.slug}</code>
								{#if !niche.isActive}
									<Badge variant="warning">Inactive</Badge>
								{/if}
							</div>
							{#if niche.aliases.length > 0}
								<div style="font-size: 0.8125rem; color: var(--text-muted); margin-top: 0.25rem;">
									{niche.aliases.join(', ')}
								</div>
							{/if}
						</div>
						<div style="display: flex; gap: 0.5rem; flex-shrink: 0;">
							<button class="btn btn-ghost" style="font-size: 0.75rem; padding: 0.25rem 0.625rem;" onclick={() => startEdit(niche)}>Edit</button>
							{#if niche.isActive}
								<button class="btn btn-ghost" style="font-size: 0.75rem; padding: 0.25rem 0.625rem; color: var(--danger);" onclick={() => deactivate(niche.id)}>Deactivate</button>
							{/if}
						</div>
					</div>
				{/if}
			</div>
		{/each}
	{/if}
</div>
