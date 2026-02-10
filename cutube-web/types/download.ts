export type DownloadStatus =
  | "queued"
  | "downloading"
  | "processing"
  | "completed"
  | "failed"
  | "cancelled";

export interface DownloadRequest {
  url: string;
  outputPath: string;
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
