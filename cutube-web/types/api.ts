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

export interface CreateDownloadResponse {
  downloadId: string;
  status: string;
  message?: string;
}

export interface GetDownloadsResponse {
  downloads: DownloadSummary[];
  totalCount: number;
}
