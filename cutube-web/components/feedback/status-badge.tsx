import { ArrowDownToLine, Check, CircleSlash, Clock3, Cog, X, type LucideIcon } from "lucide-react";
import type { DownloadStatus } from "@/types";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

interface StatusMeta {
  label: string;
  icon: LucideIcon;
  className: string;
}

const STATUS_META: Record<DownloadStatus, StatusMeta> = {
  queued: {
    label: "Na fila",
    icon: Clock3,
    className: "bg-secondary text-secondary-foreground",
  },
  downloading: {
    label: "Baixando",
    icon: ArrowDownToLine,
    className: "bg-[var(--status-downloading)] text-white",
  },
  processing: {
    label: "Processando",
    icon: Cog,
    className: "bg-[var(--status-processing)] text-white",
  },
  completed: {
    label: "Concluido",
    icon: Check,
    className: "bg-[var(--status-completed)] text-white",
  },
  failed: {
    label: "Falhou",
    icon: X,
    className: "bg-destructive text-destructive-foreground",
  },
  cancelled: {
    label: "Cancelado",
    icon: CircleSlash,
    className: "bg-muted text-muted-foreground",
  },
};

export function StatusBadge({ status }: { status: DownloadStatus }) {
  const meta = STATUS_META[status] ?? STATUS_META.queued;
  const Icon = meta.icon;

  return (
    <Badge className={cn("gap-1.5 font-medium", meta.className)} role="status" data-status={status}>
      <Icon
        className={cn("h-3.5 w-3.5", status === "processing" && "animate-spin")}
        aria-hidden="true"
      />
      {meta.label}
    </Badge>
  );
}
