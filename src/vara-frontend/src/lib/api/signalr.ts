import { HubConnectionBuilder, type HubConnection, LogLevel } from '@microsoft/signalr';
import { get } from 'svelte/store';
import { auth } from '$lib/stores/auth';

let connection: HubConnection | null = null;

export function getHub(): HubConnection {
	if (!connection) {
		connection = new HubConnectionBuilder()
			.withUrl('/api/hub/analysis', {
				accessTokenFactory: () => get(auth).token ?? ''
			})
			.withAutomaticReconnect()
			.configureLogging(LogLevel.Warning)
			.build();
	}
	return connection;
}

export async function ensureConnected(): Promise<HubConnection> {
	const hub = getHub();
	if (hub.state === 'Disconnected') await hub.start();
	return hub;
}
