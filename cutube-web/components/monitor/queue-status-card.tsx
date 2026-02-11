"use client";

import type { MonitorDownloadStatus } from "@/types";

interface QueueStatusCardProps {
  download: MonitorDownloadStatus;
}

export function QueueStatusCard({ download }: QueueStatusCardProps) {
  const getStatusColor = (state: string) => {
    switch (state) {
      case "queued":
        return "bg-gray-100 text-gray-700 border-gray-300";
      case "processing":
      case "downloading":
        return "bg-blue-50 text-blue-700 border-blue-300";
      case "completed":
        return "bg-green-50 text-green-700 border-green-300";
      case "failed":
        return "bg-red-50 text-red-700 border-red-300";
      case "dead_letter":
        return "bg-orange-50 text-orange-700 border-orange-300";
      default:
        return "bg-gray-50 text-gray-700 border-gray-300";
    }
  };

  const getStatusIcon = (state: string) => {
    switch (state) {
      case "queued":
        return "⏳";
      case "processing":
      case "downloading":
        return "⚡";
      case "completed":
        return "✅";
      case "failed":
        return "❌";
      case "dead_letter":
        return "⚠️";
      default:
        return "❓";
    }
  };

  const formatBytes = (bytes: number) => {
    if (bytes === 0) return "0 B";
    const k = 1024;
    const sizes = ["B", "KB", "MB", "GB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i];
  };

  const formatSpeed = (bytesPerSecond: number) => {
    return formatBytes(bytesPerSecond) + "/s";
  };

  const isDownloading = download.state === "downloading" || download.state === "processing";

  return (
    <div className={`bg-white rounded-lg shadow p-4 border-2 ${getStatusColor(download.state)}`}>
      {/* Header */}
      <div className="flex justify-between items-start mb-3">
        <div className="flex-1 min-w-0">
          <h3 className="font-semibold text-gray-800 truncate" title={download.url}>
            {download.metadata?.title || "Untitled"}
          </h3>
          <p className="text-xs text-gray-500 truncate">{download.url}</p>
        </div>
        <span className={`text-xs px-2 py-1 rounded-full border ${getStatusColor(download.state)}`}>
          {getStatusIcon(download.state)} {download.state}
        </span>
      </div>

      {/* Progress */}
      {isDownloading && (
        <div className="mb-3">
          <div className="flex justify-between text-sm mb-1">
            <span className="text-gray-700 font-medium">{download.progress}%</span>
            <span className="text-gray-500">{formatSpeed(download.speed)}</span>
          </div>
          <div className="w-full bg-gray-200 rounded-full h-2">
            <div
              className="bg-blue-600 h-2 rounded-full transition-all duration-300"
              style={{ width: `${download.progress}%` }}
            />
          </div>
          <div className="flex justify-between text-xs text-gray-500 mt-1">
            <span>{formatBytes(download.downloadedBytes)}</span>
            <span>{download.totalBytes ? formatBytes(download.totalBytes) : "Unknown"}</span>
          </div>
        </div>
      )}

      {/* Footer */}
      <div className="flex justify-between items-center text-xs text-gray-500">
        <div>
          <span className="font-mono">ID: {download.correlationId.slice(0, 8)}...</span>
          {download.retryCount > 0 && (
            <span className="ml-2 text-orange-600 font-semibold">Retry: {download.retryCount}</span>
          )}
        </div>
        <div className="flex gap-2">
          {download.audioOnly && <span className="text-purple-600">🎵 Audio</span>}
          {download.metadata?.duration && (
            <span className="text-gray-400">⏱️ {download.metadata.duration}</span>
          )}
        </div>
      </div>

      {/* Error Message */}
      {download.errorMessage && (
        <div className="mt-2 text-xs text-red-600 bg-red-50 p-2 rounded border border-red-200">
          <strong>Error:</strong> {download.errorMessage}
        </div>
      )}
    </div>
  );
}
