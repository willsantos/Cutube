"use client";

import { useMonitor } from "@/hooks/use-monitor";
import { useSignalR } from "@/hooks/use-signalr";
import { MonitorMetrics } from "@/components/monitor/monitor-metrics";
import { ActiveQueue } from "@/components/monitor/active-queue";
import { FailedDownloads } from "@/components/monitor/failed-downloads";
import { useCallback } from "react";

export default function MonitorPage() {
  const {
    activeDownloads,
    failedDownloads,
    metrics,
    isLoading,
    error,
    refresh,
    lastUpdated,
  } = useMonitor(5000);

  const handleStatusChanged = useCallback(() => {
    refresh();
  }, [refresh]);

  const { isConnected } = useSignalR({
    onDownloadStatusChanged: handleStatusChanged,
    onDownloadCompleted: handleStatusChanged,
    onDownloadFailed: handleStatusChanged,
  });

  if (error) {
    return (
      <div className="min-h-screen bg-gray-50 p-6">
        <div className="max-w-7xl mx-auto">
          <div className="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
            <p className="text-4xl mb-2">❌</p>
            <h2 className="text-lg font-semibold text-red-700 mb-2">Error loading monitor</h2>
            <p className="text-red-600 mb-4">{error}</p>
            <button
              onClick={refresh}
              className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700"
            >
              Retry
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center mb-6 gap-4">
          <div>
            <h1 className="text-3xl font-bold text-gray-800">Monitor</h1>
            <p className="text-gray-500">
              System monitoring and queue status
              {isConnected ? (
                <span className="ml-2 text-green-600 font-medium">● Live</span>
              ) : (
                <span className="ml-2 text-yellow-600">○ Polling</span>
              )}
              {lastUpdated && (
                <span className="ml-2 text-xs text-gray-400">
                  Updated: {lastUpdated.toLocaleTimeString()}
                </span>
              )}
            </p>
          </div>
          <button
            onClick={refresh}
            disabled={isLoading}
            className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50 flex items-center gap-2"
          >
            {isLoading ? "🔄 Refreshing..." : "🔄 Refresh"}
          </button>
        </div>

        {/* Metrics */}
        <MonitorMetrics metrics={metrics} />

        {/* Active Queue */}
        <div className="mb-8">
          <ActiveQueue downloads={activeDownloads} />
        </div>

        {/* Failed Downloads */}
        <FailedDownloads downloads={failedDownloads} />
      </div>
    </div>
  );
}
