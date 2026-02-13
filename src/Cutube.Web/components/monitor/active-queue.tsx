"use client";

import type { MonitorDownloadStatus } from "@/types";
import { QueueStatusCard } from "./queue-status-card";

interface ActiveQueueProps {
  downloads: MonitorDownloadStatus[];
}

export function ActiveQueue({ downloads }: ActiveQueueProps) {
  const downloading = downloads.filter((d) => d.state === "downloading" || d.state === "processing");
  const queued = downloads.filter((d) => d.state === "queued");

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold flex items-center gap-2">
          <span className="text-blue-500">⚡</span>
          Active Queue
        </h2>
        <div className="text-sm text-gray-500">
          <span className="inline-flex items-center px-2 py-1 bg-blue-100 text-blue-700 rounded mr-2">
            {downloading.length} processing
          </span>
          <span className="inline-flex items-center px-2 py-1 bg-gray-100 text-gray-700 rounded">
            {queued.length} queued
          </span>
        </div>
      </div>

      {downloads.length === 0 ? (
        <div className="bg-gray-50 rounded-lg p-8 text-center text-gray-500 border-2 border-dashed border-gray-300">
          <p className="text-4xl mb-2">📭</p>
          <p className="font-medium">Queue is empty</p>
          <p className="text-sm mt-1">No active downloads at the moment</p>
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {downloads.map((download) => (
            <QueueStatusCard key={download.correlationId} download={download} />
          ))}
        </div>
      )}
    </div>
  );
}
