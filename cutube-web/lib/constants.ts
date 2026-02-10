export const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

export const SIGNALR_HUB_URL = `${API_BASE_URL}/hubs/downloads`;

export const QUERY_KEYS = {
  downloads: ["downloads"] as const,
  download: (id: string) => ["downloads", id] as const,
  videoInfo: (url: string) => ["video-info", url] as const,
} as const;

export const POLL_INTERVAL = 5000;
export const DOWNLOAD_POLL_INTERVAL = 2000;
