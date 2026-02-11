"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { CreateDownloadResponse, DownloadRequest } from "@/types";
import { api } from "@/lib/api";
import { DOWNLOAD_POLL_INTERVAL, POLL_INTERVAL, QUERY_KEYS } from "@/lib/constants";

export function useDownloads(params?: { status?: string; limit?: number; offset?: number }) {
  return useQuery({
    queryKey: [...QUERY_KEYS.downloads, params],
    queryFn: () => api.downloads.list(params),
    refetchInterval: POLL_INTERVAL,
  });
}

export function useDownload(id: string) {
  return useQuery({
    queryKey: QUERY_KEYS.download(id),
    queryFn: () => api.downloads.get(id),
    refetchInterval: DOWNLOAD_POLL_INTERVAL,
    enabled: Boolean(id),
  });
}

export function useCreateDownload() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: DownloadRequest): Promise<CreateDownloadResponse> =>
      api.downloads.create(request),
    onSuccess: async (response) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: QUERY_KEYS.downloads }),
        queryClient.invalidateQueries({ queryKey: QUERY_KEYS.download(response.downloadId) }),
      ]);
    },
  });
}

export function useCancelDownload() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => api.downloads.cancel(id),
    onSuccess: async (_, id) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: QUERY_KEYS.downloads }),
        queryClient.invalidateQueries({ queryKey: QUERY_KEYS.download(id) }),
      ]);
    },
  });
}
