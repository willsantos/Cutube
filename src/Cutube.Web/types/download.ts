export type DownloadStatus =
  | "queued"
  | "downloading"
  | "processing"
  | "completed"
  | "failed"
  | "cancelled"
  | "dead_letter"
  | "expired";

export interface DownloadRequest {
  url: string;
  outputPath?: string;
  startTime?: string;
  endTime?: string;
  audioOnly?: boolean;
  customFilename?: string;
}

export interface DownloadProgress {
  downloadId: string;
  progress: number;
  speed: number;
  eta?: string;
  downloadedBytes: number;
  totalBytes: number;
  status: DownloadStatus;
}

export interface DownloadSummary {
  downloadId: string;
  url: string;
  status: DownloadStatus;
  progress: number;
  filePath?: string;
  createdAt: string;
  completedAt?: string;
  errorMessage?: string;
}

export interface DownloadDetails extends DownloadSummary {
  speed: number;
  eta?: string;
  downloadedBytes: number;
  totalBytes: number;
}

export interface DownloadStartedEvent {
  downloadId: string;
  url: string;
  startedAt: string;
}

export interface DownloadProgressEvent {
  downloadId: string;
  progress: number;
  speed: number;
  eta?: string;
  downloadedBytes: number;
  totalBytes: number;
  status: DownloadStatus;
}

export interface DownloadCompletedEvent {
  downloadId: string;
  filePath: string;
  size: number;
  duration: string;
  completedAt: string;
}

export interface DownloadFailedEvent {
  downloadId: string;
  error: string;
  failedAt: string;
}

// Monitor types
export interface MonitorDownloadStatus {
  id: string;
  correlationId: string;
  url: string;
  state: DownloadStatus;
  progress: number;
  speed: number;
  downloadedBytes: number;
  totalBytes?: number;
  eta?: string;
  outputPath?: string;
  outputFilename?: string;
  audioOnly: boolean;
  errorMessage?: string;
  retryCount: number;
  createdAt: string;
  updatedAt: string;
  startedAt?: string;
  completedAt?: string;
  duration?: string;
  metadata?: DownloadMetadata;
}

export interface DownloadMetadata {
  title?: string;
  duration?: string;
  thumbnail?: string;
  channel?: string;
}

export interface DownloadListResponse {
  downloads: MonitorDownloadStatus[];
  totalCount: number;
}

export interface CreateDownloadResponse {
  downloadId: string;
  correlationId: string;
  status: string;
  message: string;
  enqueuedAt: string;
}
