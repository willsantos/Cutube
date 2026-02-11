"use client";

import type { MetricsResponse } from "@/types";

interface MonitorMetricsProps {
  metrics: MetricsResponse | null;
}

export function MonitorMetrics({ metrics }: MonitorMetricsProps) {
  if (!metrics) {
    return (
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        {[...Array(4)].map((_, i) => (
          <div key={i} className="bg-gray-100 animate-pulse h-24 rounded-lg" />
        ))}
      </div>
    );
  }

  const formatDuration = (seconds: number) => {
    if (seconds < 60) return `${Math.round(seconds)}s`;
    if (seconds < 3600) return `${Math.round(seconds / 60)}m`;
    return `${Math.round(seconds / 3600)}h`;
  };

  const cards = [
    {
      label: "Processing",
      value: metrics.processingCount,
      subtext: `${metrics.queuedCount} queued`,
      color: "bg-blue-500",
      icon: "⚡",
    },
    {
      label: "Completed",
      value: metrics.completedCount,
      subtext: `${metrics.throughputPerMinute.toFixed(1)}/min`,
      color: "bg-green-500",
      icon: "✅",
    },
    {
      label: "Failed / DLQ",
      value: metrics.failedCount + metrics.deadLetterCount,
      subtext: `${metrics.errorRate.toFixed(1)}% error rate`,
      color: metrics.errorRate > 10 ? "bg-red-500" : "bg-yellow-500",
      icon: "⚠️",
    },
    {
      label: "Avg Time",
      value: formatDuration(metrics.averageProcessingTimeSeconds),
      subtext: "per download",
      color: "bg-purple-500",
      icon: "⏱️",
    },
  ];

  return (
    <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
      {cards.map((card) => (
        <div key={card.label} className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center justify-between mb-2">
            <span className="text-2xl">{card.icon}</span>
            <span className={`text-white text-xs px-2 py-1 rounded ${card.color}`}>
              {card.label}
            </span>
          </div>
          <div className="text-3xl font-bold text-gray-800">{card.value}</div>
          <div className="text-sm text-gray-500">{card.subtext}</div>
        </div>
      ))}
    </div>
  );
}
