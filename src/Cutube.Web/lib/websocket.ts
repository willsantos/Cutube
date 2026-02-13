import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { SIGNALR_HUB_URL } from "./constants";
import type { DownloadCompletedEvent, DownloadFailedEvent, DownloadProgressEvent } from "@/types";

type ConnectionStateListener = (state: HubConnectionState) => void;
type ProgressCallback = (event: DownloadProgressEvent) => void;
type CompletedCallback = (event: DownloadCompletedEvent) => void;
type FailedCallback = (event: DownloadFailedEvent) => void;

type ListenerMap<T> = Map<string, Set<(event: T) => void>>;

function normalizeStatus(status: string): DownloadProgressEvent["status"] {
  const lower = status.toLowerCase();

  if (
    lower === "queued" ||
    lower === "downloading" ||
    lower === "processing" ||
    lower === "completed" ||
    lower === "failed" ||
    lower === "cancelled"
  ) {
    return lower;
  }

  return "downloading";
}

class WebSocketService {
  private connection: HubConnection;
  private isStarting = false;
  private readonly progressListeners: ListenerMap<DownloadProgressEvent> = new Map();
  private readonly completedListeners: ListenerMap<DownloadCompletedEvent> = new Map();
  private readonly failedListeners: ListenerMap<DownloadFailedEvent> = new Map();
  private readonly stateListeners = new Set<ConnectionStateListener>();

  constructor() {
    this.connection = new HubConnectionBuilder()
      .withUrl(SIGNALR_HUB_URL)
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();

    this.registerEventHandlers();

    this.connection.onreconnecting(() => {
      this.broadcastState(this.connection.state);
    });

    this.connection.onreconnected(() => {
      this.broadcastState(this.connection.state);
    });

    this.connection.onclose(() => {
      this.broadcastState(this.connection.state);
    });
  }

  get state(): HubConnectionState {
    return this.connection.state;
  }

  async connect(): Promise<void> {
    if (this.connection.state === HubConnectionState.Connected || this.isStarting) {
      return;
    }

    this.isStarting = true;
    try {
      await this.connection.start();
      this.broadcastState(this.connection.state);
    } finally {
      this.isStarting = false;
    }
  }

  async disconnect(): Promise<void> {
    if (this.connection.state === HubConnectionState.Disconnected) return;
    await this.connection.stop();
    this.broadcastState(this.connection.state);
  }

  async joinDownload(downloadId: string): Promise<void> {
    await this.connect();
    await this.connection.invoke("JoinDownloadGroup", downloadId);
  }

  async leaveDownload(downloadId: string): Promise<void> {
    if (this.connection.state !== HubConnectionState.Connected) return;
    await this.connection.invoke("LeaveDownloadGroup", downloadId);
  }

  onConnectionStateChange(callback: ConnectionStateListener): () => void {
    this.stateListeners.add(callback);
    callback(this.connection.state);

    return () => {
      this.stateListeners.delete(callback);
    };
  }

  onProgress(downloadId: string, callback: ProgressCallback): () => void {
    return this.subscribe(this.progressListeners, downloadId, callback);
  }

  onCompleted(downloadId: string, callback: CompletedCallback): () => void {
    return this.subscribe(this.completedListeners, downloadId, callback);
  }

  onFailed(downloadId: string, callback: FailedCallback): () => void {
    return this.subscribe(this.failedListeners, downloadId, callback);
  }

  private subscribe<T>(
    listeners: ListenerMap<T>,
    id: string,
    callback: (event: T) => void,
  ): () => void {
    const current = listeners.get(id) ?? new Set();
    current.add(callback);
    listeners.set(id, current);

    return () => {
      const active = listeners.get(id);
      if (!active) return;
      active.delete(callback);
      if (active.size === 0) {
        listeners.delete(id);
      }
    };
  }

  private emit<T>(listeners: ListenerMap<T>, id: string, event: T): void {
    const scoped = listeners.get(id);
    if (!scoped) return;

    for (const listener of scoped) {
      listener(event);
    }
  }

  private registerEventHandlers(): void {
    this.connection.on("DownloadProgress", (downloadId: string, event: DownloadProgressEvent) => {
      this.emit(this.progressListeners, downloadId, {
        ...event,
        status: normalizeStatus(event.status),
      });
    });

    this.connection.on("DownloadCompleted", (downloadId: string, event: DownloadCompletedEvent) => {
      this.emit(this.completedListeners, downloadId, event);
    });

    this.connection.on("DownloadFailed", (downloadId: string, event: DownloadFailedEvent) => {
      this.emit(this.failedListeners, downloadId, event);
    });
  }

  private broadcastState(state: HubConnectionState): void {
    for (const callback of this.stateListeners) {
      callback(state);
    }
  }
}

export const ws = new WebSocketService();
