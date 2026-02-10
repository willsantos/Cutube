import { Calendar } from "lucide-react";
import { formatDateTime } from "@/lib/utils";

interface DateDisplayProps {
  value: string;
}

export function DateDisplay({ value }: DateDisplayProps) {
  return (
    <span className="text-muted-foreground inline-flex items-center gap-2 text-sm">
      <Calendar className="h-4 w-4" aria-hidden="true" />
      {formatDateTime(value)}
    </span>
  );
}
