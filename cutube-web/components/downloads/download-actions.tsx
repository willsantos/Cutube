"use client";

import { Download, RefreshCw, X } from "lucide-react";
import { toast } from "sonner";
import { useCancelDownload } from "@/hooks/use-downloads";
import type { DownloadDetails, DownloadStatus } from "@/types";
import { Button } from "@/components/ui/button";

interface DownloadActionsProps {
  download: Pick<DownloadDetails, "downloadId" | "status" | "filePath">;
  onRefresh?: () => void;
}

function canCancel(status: DownloadStatus): boolean {
  return status === "queued" || status === "downloading" || status === "processing";
}

export function DownloadActions({ download, onRefresh }: DownloadActionsProps) {
  const cancelMutation = useCancelDownload();

  const onCancel = async () => {
    try {
      await cancelMutation.mutateAsync(download.downloadId);
      toast.success("Download cancelado com sucesso");
      onRefresh?.();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Falha ao cancelar download");
    }
  };

  const onDownloadFile = () => {
    if (!download.filePath) return;
    window.open(download.filePath, "_blank", "noopener,noreferrer");
  };

  return (
    <div className="flex flex-wrap gap-2">
      {canCancel(download.status) ? (
        <Button variant="destructive" onClick={onCancel} disabled={cancelMutation.isPending}>
          <X className="h-4 w-4" />
          Cancelar
        </Button>
      ) : null}

      {download.status === "completed" && download.filePath ? (
        <Button variant="secondary" onClick={onDownloadFile}>
          <Download className="h-4 w-4" />
          Baixar arquivo
        </Button>
      ) : null}

      <Button variant="outline" onClick={onRefresh}>
        <RefreshCw className="h-4 w-4" />
        Atualizar
      </Button>
    </div>
  );
}
