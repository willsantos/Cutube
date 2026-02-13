"use client";

import { useState, useEffect, useCallback } from "react";
import type { MonitorDownloadStatus, MetricsResponse } from "@/types";
import { api } from "@/lib/api";

interface UseMonitorReturn {
  activeDownloads: MonitorDownloadStatus[];
  failedDownloads: MonitorDownloadStatus[];
  metrics: MetricsResponse | null;
  isLoading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
  lastUpdated: Date | null;
}

export function useMonitor(refreshInterval = 5000): UseMonitorReturn {
  const [activeDownloads, setActiveDownloads] = useState<MonitorDownloadStatus[]>([]);
  const [failedDownloads, setFailedDownloads] = useState<MonitorDownloadStatus[]>([]);
  const [metrics, setMetrics] = useState<MetricsResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setError(null);
      const [active, failed, metricsData] = await Promise.all([
        api.downloads.getActive(),
        api.downloads.getFailed(),
        api.metrics.get(),
      ]);

      setActiveDownloads(active.downloads);
      setFailedDownloads(failed.downloads);
      setMetrics(metricsData);
      setLastUpdated(new Date());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unknown error");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();

    const interval = setInterval(fetchData, refreshInterval);
    return () => clearInterval(interval);
  }, [fetchData, refreshInterval]);

  return {
    activeDownloads,
    failedDownloads,
    metrics,
    isLoading,
    error,
    refresh: fetchData,
    lastUpdated,
  };
}
