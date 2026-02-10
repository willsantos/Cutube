import type { DownloadStatus } from "@/types";
import { Progress } from "@/components/ui/progress";
import { cn } from "@/lib/utils";

interface ProgressBarProps {
  value: number;
  status: DownloadStatus;
  showLabel?: boolean;
}

function getProgressClass(status: DownloadStatus): string {
  switch (status) {
    case "completed":
      return "[&_[data-slot=progress-indicator]]:bg-[hsl(var(--status-completed))]";
    case "failed":
      return "[&_[data-slot=progress-indicator]]:bg-destructive";
    case "processing":
      return "[&_[data-slot=progress-indicator]]:bg-[hsl(var(--status-processing))]";
    case "cancelled":
      return "[&_[data-slot=progress-indicator]]:bg-muted-foreground";
    case "downloading":
      return "[&_[data-slot=progress-indicator]]:bg-[hsl(var(--status-downloading))]";
    default:
      return "[&_[data-slot=progress-indicator]]:bg-primary";
  }
}

export function ProgressBar({ value, status, showLabel = true }: ProgressBarProps) {
  const progressValue = Math.min(100, Math.max(0, Number.isFinite(value) ? value : 0));

  return (
    <div className="space-y-2" data-status={status}>
      <div className="flex items-center justify-between gap-2">
        <Progress
          value={progressValue}
          className={cn("h-2.5 flex-1 bg-secondary/70", getProgressClass(status))}
          role="progressbar"
          aria-valuemin={0}
          aria-valuemax={100}
          aria-valuenow={Math.round(progressValue)}
          aria-label="Progresso do download"
        />
        {showLabel ? (
          <span className="tabular-nums text-sm font-medium">{Math.round(progressValue)}%</span>
        ) : null}
      </div>
      <span className="sr-only" aria-live="polite" aria-atomic="true">
        {Math.round(progressValue)}% concluido
      </span>
      {status === "processing" ? (
        <p className="text-muted-foreground text-xs">Processando arquivo...</p>
      ) : null}
    </div>
  );
}
