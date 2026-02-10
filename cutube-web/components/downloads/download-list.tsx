"use client";

import { Download } from "lucide-react";
import { DownloadCard } from "@/components/downloads/download-card";
import { ErrorMessage } from "@/components/feedback/error-message";
import { LoadingSkeleton } from "@/components/feedback/loading-skeleton";
import { EmptyState } from "@/components/layout/empty-state";
import { useDownloads } from "@/hooks/use-downloads";

interface DownloadListProps {
  status?: string;
  limit?: number;
}

export function DownloadList({ status, limit }: DownloadListProps) {
  const { data, isLoading, isError, error, refetch } = useDownloads({ status, limit });

  if (isLoading) {
    return <LoadingSkeleton variant="card" count={6} />;
  }

  if (isError) {
    return <ErrorMessage message={error.message} onRetry={() => void refetch()} />;
  }

  if (!data || data.downloads.length === 0) {
    return (
      <EmptyState
        icon={Download}
        title="Nenhum download ainda"
        description="Crie o primeiro download usando o formulario acima."
      />
    );
  }

  return (
    <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
      {data.downloads.map((download) => (
        <DownloadCard key={download.downloadId} download={download} />
      ))}
    </div>
  );
}
