import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel
} from '@microsoft/signalr';
import type {
  SessionSnapshotDto,
  SimulationConnectionStatus
} from '@/types/api';
import { getAccessToken } from '@/services/auth-storage';

type SnapshotHandler = (snapshot: SessionSnapshotDto) => void;
type StatusHandler = (status: SimulationConnectionStatus) => void;

export interface SimulationHubClient {
  joinSession(sessionId: string): Promise<void>;
  stop(): Promise<void>;
  onSnapshot(handler: SnapshotHandler): () => void;
  onStatusChange(handler: StatusHandler): () => void;
}

function resolveHubUrl(): string {
  const baseUrl = import.meta.env.VITE_SIGNALR_URL?.trim();

  if (!baseUrl) {
    return '/hubs/simulation';
  }

  return `${baseUrl.replace(/\/$/, '')}/hubs/simulation`;
}

export function createSimulationHub(): SimulationHubClient {
  let connection: HubConnection | null = null;
  let joinedSessionId: string | null = null;
  const snapshotHandlers = new Set<SnapshotHandler>();
  const statusHandlers = new Set<StatusHandler>();

  function notifyStatus(status: SimulationConnectionStatus): void {
    statusHandlers.forEach((handler) => handler(status));
  }

  function buildConnection(): HubConnection {
    const hubConnection = new HubConnectionBuilder()
      .withUrl(resolveHubUrl(), {
        accessTokenFactory: () => getAccessToken() ?? ''
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    hubConnection.on('snapshotUpdated', (snapshot: SessionSnapshotDto) => {
      snapshotHandlers.forEach((handler) => handler(snapshot));
    });

    hubConnection.onreconnecting(() => {
      notifyStatus('reconnecting');
    });

    hubConnection.onreconnected(async () => {
      try {
        if (joinedSessionId && hubConnection.state === HubConnectionState.Connected) {
          await hubConnection.invoke('JoinSession', joinedSessionId);
        }

        notifyStatus('connected');
      } catch {
        notifyStatus('disconnected');
      }
    });

    hubConnection.onclose(() => {
      notifyStatus('disconnected');
    });

    return hubConnection;
  }

  async function ensureStarted(): Promise<void> {
    if (!connection) {
      connection = buildConnection();
    }

    if (connection.state === HubConnectionState.Disconnected) {
      notifyStatus('connecting');

      try {
        await connection.start();
        notifyStatus('connected');
      } catch (error) {
        notifyStatus('disconnected');
        throw error;
      }
    }
  }

  async function joinSession(sessionId: string): Promise<void> {
    await ensureStarted();

    if (!connection) {
      return;
    }

    if (
      joinedSessionId &&
      joinedSessionId !== sessionId &&
      connection.state === HubConnectionState.Connected
    ) {
      await connection.invoke('LeaveSession', joinedSessionId);
    }

    joinedSessionId = sessionId;

    if (connection.state === HubConnectionState.Connected) {
      await connection.invoke('JoinSession', sessionId);
    }
  }

  async function stop(): Promise<void> {
    if (!connection) {
      notifyStatus('disconnected');
      return;
    }

    if (joinedSessionId && connection.state === HubConnectionState.Connected) {
      await connection.invoke('LeaveSession', joinedSessionId);
    }

    joinedSessionId = null;

    if (connection.state !== HubConnectionState.Disconnected) {
      await connection.stop();
    }

    notifyStatus('disconnected');
  }

  function onSnapshot(handler: SnapshotHandler): () => void {
    snapshotHandlers.add(handler);

    return () => {
      snapshotHandlers.delete(handler);
    };
  }

  function onStatusChange(handler: StatusHandler): () => void {
    statusHandlers.add(handler);

    return () => {
      statusHandlers.delete(handler);
    };
  }

  return {
    joinSession,
    stop,
    onSnapshot,
    onStatusChange
  };
}
