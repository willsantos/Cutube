"use client";

import { Clock3, HardDrive, Link2, Timer, Wifi, WifiOff, Zap } from "lucide-react";
import { DownloadActions } from "@/components/downloads/download-actions";
import { DateDisplay } from "@/components/data-display/date-display";
import { InfoRow } from "@/components/data-display/info-row";
import { ProgressBar } from "@/components/data-display/progress-bar";
import { StatCard } from "@/components/data-display/stat-card";
import { ErrorMessage } from "@/components/feedback/error-message";
import { LoadingSkeleton } from "@/components/feedback/loading-skeleton";
import { StatusBadge } from "@/components/feedback/status-badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useDownload, useDownloads } from "@/hooks/use-downloads";
import { useDownloadProgress } from "@/hooks/use-download-progress";
import { useWebSocket } from "@/hooks/use-websocket";
import { formatBytes, formatSpeed, formatTimeRange } from "@/lib/utils";

interface DownloadDetailProps {
  id: string;
}

export function DownloadDetail({ id }: DownloadDetailProps) {
  const { data, isLoading, isError, error, refetch } = useDownload(id);
  const { progress, error: progressError } = useDownloadProgress(id);
  const { isConnected } = useWebSocket();
  const listQuery = useDownloads();

  if (isLoading) {
    return <LoadingSkeleton variant="detail" />;
  }

  if (isError || !data) {
    return (
      <ErrorMessage
        message={error instanceof Error ? error.message : "Download nao encontrado"}
        onRetry={() => void refetch()}
      />
    );
  }

  const liveStatus = progress?.status ?? data.status;
  const liveProgress = progress?.progress ?? data.progress;
  const liveSpeed = progress?.speed ?? data.speed;
  const liveEta = progress?.eta ?? data.eta;
  const downloadedBytes = progress?.downloadedBytes ?? data.downloadedBytes;
  const totalBytes = progress?.totalBytes ?? data.totalBytes;

  return (
    <div className="grid gap-6 lg:grid-cols-[2fr_1fr]">
      <div className="space-y-6">
        <Card>
          <CardHeader className="space-y-3">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <CardTitle className="text-xl">Detalhes do download</CardTitle>
              <StatusBadge status={liveStatus} />
            </div>
            <div className="text-muted-foreground inline-flex items-center gap-2 text-sm">
              {isConnected ? (
                <>
                  <Wifi className="h-4 w-4 text-emerald-500" />
                  Atualizacao em tempo real ativa
                </>
              ) : (
                <>
                  <WifiOff className="h-4 w-4 text-amber-500" />
                  Reconectando SignalR...
                </>
              )}
            </div>
          </CardHeader>

          <CardContent className="space-y-6">
            <ProgressBar value={liveProgress} status={liveStatus} />

            <div className="space-y-3">
              <InfoRow label="URL" value={data.url} icon={Link2} />
              <InfoRow label="ID" value={data.downloadId} icon={HardDrive} />
              <div className="flex items-center justify-between gap-4">
                <span className="text-muted-foreground inline-flex items-center gap-2 text-sm">
                  <Timer className="h-4 w-4" aria-hidden="true" />
                  Criado em
                </span>
                <DateDisplay value={data.createdAt} />
              </div>
              {data.completedAt ? (
                <div className="flex items-center justify-between gap-4">
                  <span className="text-muted-foreground inline-flex items-center gap-2 text-sm">
                    <Timer className="h-4 w-4" aria-hidden="true" />
                    Concluido em
                  </span>
                  <DateDisplay value={data.completedAt} />
                </div>
              ) : null}
            </div>

            {progressError ? <ErrorMessage message={progressError} /> : null}

            {data.errorMessage ? <ErrorMessage message={data.errorMessage} /> : null}

            <DownloadActions
              download={{
                downloadId: data.downloadId,
                status: data.status,
                filePath: data.filePath,
              }}
              onRefresh={() => {
                void refetch();
                void listQuery.refetch();
              }}
            />
          </CardContent>
        </Card>
      </div>

      <aside className="space-y-4">
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-1">
          <StatCard label="Velocidade" value={formatSpeed(liveSpeed)} icon={Zap} />
          <StatCard label="ETA" value={liveEta ? formatTimeRange(liveEta) : "--"} icon={Clock3} />
          <StatCard label="Baixado" value={formatBytes(downloadedBytes)} icon={HardDrive} />
          <StatCard label="Total" value={formatBytes(totalBytes)} icon={HardDrive} />
        </div>
      </aside>
    </div>
  );
}
