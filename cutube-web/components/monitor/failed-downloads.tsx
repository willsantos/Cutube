"use client";

import type { MonitorDownloadStatus } from "@/types";
import { QueueStatusCard } from "./queue-status-card";

interface FailedDownloadsProps {
  downloads: MonitorDownloadStatus[];
}

export function FailedDownloads({ downloads }: FailedDownloadsProps) {
  if (downloads.length === 0) {
    return null;
  }

  const failed = downloads.filter((d) => d.state === "failed");
  const deadLetter = downloads.filter((d) => d.state === "dead_letter");

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold flex items-center gap-2 text-red-600">
          <span>⚠️</span>
          Failed Downloads
        </h2>
        <div className="text-sm">
          <span className="inline-flex items-center px-2 py-1 bg-yellow-100 text-yellow-700 rounded mr-2">
            {failed.length} retryable
          </span>
          <span className="inline-flex items-center px-2 py-1 bg-orange-100 text-orange-700 rounded">
            {deadLetter.length} dead letter
          </span>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {downloads.map((download) => (
          <QueueStatusCard key={download.correlationId} download={download} />
        ))}
      </div>
    </div>
  );
}
