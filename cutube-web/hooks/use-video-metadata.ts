"use client";

import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { QUERY_KEYS } from "@/lib/constants";

export function useVideoMetadata(url: string) {
  return useQuery({
    queryKey: QUERY_KEYS.videoInfo(url),
    queryFn: () => api.videos.getInfo(url),
    enabled: Boolean(url),
    retry: 1,
  });
}
