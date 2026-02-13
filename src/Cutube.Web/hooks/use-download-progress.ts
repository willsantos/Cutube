"use client";

import { useEffect, useState } from "react";
import { ws } from "@/lib/websocket";
import type { DownloadCompletedEvent, DownloadProgressEvent } from "@/types";

interface UseDownloadProgressResult {
  progress: DownloadProgressEvent | null;
  isCompleted: boolean;
  isFailed: boolean;
  error: string | null;
  completedData: DownloadCompletedEvent | null;
}

export function useDownloadProgress(downloadId: string): UseDownloadProgressResult {
  const [progress, setProgress] = useState<DownloadProgressEvent | null>(null);
  const [isCompleted, setIsCompleted] = useState(false);
  const [isFailed, setIsFailed] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [completedData, setCompletedData] = useState<DownloadCompletedEvent | null>(null);

  useEffect(() => {
    let mounted = true;

    ws.connect().catch(() => undefined);
    ws.joinDownload(downloadId).catch(() => undefined);

    const unsubProgress = ws.onProgress(downloadId, (data) => {
      if (!mounted) return;
      setProgress(data);
      setError(null);
    });

    const unsubCompleted = ws.onCompleted(downloadId, (data) => {
      if (!mounted) return;
      setIsCompleted(true);
      setCompletedData(data);
      setProgress((prev) =>
        prev
          ? {
              ...prev,
              progress: 100,
              status: "completed",
            }
          : null,
      );
    });

    const unsubFailed = ws.onFailed(downloadId, (data) => {
      if (!mounted) return;
      setIsFailed(true);
      setError(data.error);
    });

    return () => {
      mounted = false;
      unsubProgress();
      unsubCompleted();
      unsubFailed();
      ws.leaveDownload(downloadId).catch(() => undefined);
    };
  }, [downloadId]);

  return {
    progress,
    isCompleted,
    isFailed,
    error,
    completedData,
  };
}
