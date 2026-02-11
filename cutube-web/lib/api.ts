import type {
  CreateDownloadResponse,
  DownloadDetails,
  DownloadRequest,
  GetDownloadsResponse,
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

    cancel: async (id: string): Promise<void> => {
      await fetchApi<void>(`/api/downloads/${id}`, {
        method: "DELETE",
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

  health: {
    check: async (): Promise<{ status: string }> => {
      return fetchApi<{ status: string }>("/health");
    },
  },
};
