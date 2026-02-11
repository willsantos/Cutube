import type {
  CreateDownloadResponse,
  DownloadDetails,
  DownloadListResponse,
  DownloadRequest,
  GetDownloadsResponse,
  MetricsResponse,
  VideoMetadata,
} from "@/types";
import { buildQueryString, fetchApi } from "./api-helpers";

export const api = {
  downloads: {
    create: async (request: DownloadRequest): Promise<CreateDownloadResponse> => {
      const response = await fetchApi<CreateDownloadResponse>("/api/downloads", {
        method: "POST",
        body: JSON.stringify(request),
      });

      return response;
    },

    list: async (params?: {
      status?: string;
      limit?: number;
      offset?: number;
    }): Promise<GetDownloadsResponse> => {
      const query = buildQueryString(params ?? {});
      return fetchApi<GetDownloadsResponse>(`/api/downloads${query}`);
    },

    get: async (id: string): Promise<DownloadDetails> => {
      return fetchApi<DownloadDetails>(`/api/downloads/${id}`);
    },

    getActive: async (): Promise<DownloadListResponse> => {
      return fetchApi<DownloadListResponse>("/api/downloads/queue/active");
    },

    getFailed: async (): Promise<DownloadListResponse> => {
      return fetchApi<DownloadListResponse>("/api/downloads/queue/failed");
    },

    cancel: async (id: string): Promise<void> => {
      await fetchApi<void>(`/api/downloads/${id}`, {
        method: "DELETE",
      });
    },

    updateStatus: async (correlationId: string, state: string, errorMessage?: string): Promise<void> => {
      await fetchApi<void>(`/api/downloads/${correlationId}/status`, {
        method: "PATCH",
        body: JSON.stringify({ state, errorMessage }),
      });
    },

    updateProgress: async (correlationId: string, progress: number, speed: number, downloadedBytes: number): Promise<void> => {
      await fetchApi<void>(`/api/downloads/${correlationId}/progress`, {
        method: "POST",
        body: JSON.stringify({ progress, speed, downloadedBytes }),
      });
    },
  },

  videos: {
    getInfo: async (url: string): Promise<VideoMetadata> => {
      const encodedUrl = encodeURIComponent(url);
      const response = await fetchApi<Omit<VideoMetadata, "formats">>(
        `/api/videos/info?url=${encodedUrl}`,
      );

      return {
        ...response,
        formats: [],
      };
    },
  },

  metrics: {
    get: async (): Promise<MetricsResponse> => {
      return fetchApi<MetricsResponse>("/api/metrics");
    },
  },

  health: {
    check: async (): Promise<{ status: string }> => {
      return fetchApi<{ status: string }>("/health");
    },
  },
};
