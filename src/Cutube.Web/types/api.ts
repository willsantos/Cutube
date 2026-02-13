import type { DownloadSummary } from "./download";

export interface ApiResponse<T> {
  data: T;
  error?: string;
}

export interface ApiError {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

export interface GetDownloadsResponse {
  downloads: DownloadSummary[];
  totalCount: number;
}

export interface MetricsResponse {
  totalDownloads: number;
  pendingCount: number;
  queuedCount: number;
  processingCount: number;
  completedCount: number;
  failedCount: number;
  deadLetterCount: number;
  averageProcessingTimeSeconds: number;
  errorRate: number;
  throughputPerMinute: number;
}
