import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";

interface InfoRowProps {
  label: string;
  value: string;
  icon?: LucideIcon;
  className?: string;
}

export function InfoRow({ label, value, icon: Icon, className }: InfoRowProps) {
  return (
    <div className={cn("flex items-center justify-between gap-4", className)}>
      <div className="text-muted-foreground flex items-center gap-2 text-sm">
        {Icon ? <Icon className="h-4 w-4" aria-hidden="true" /> : null}
        <span>{label}</span>
      </div>
      <span className="tabular-nums text-right text-sm font-medium break-all">{value}</span>
    </div>
  );
}
