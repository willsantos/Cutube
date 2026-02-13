import type { LucideIcon } from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";

interface StatCardProps {
  label: string;
  value: string;
  icon: LucideIcon;
}

export function StatCard({ label, value, icon: Icon }: StatCardProps) {
  return (
    <Card>
      <CardContent className="space-y-3 p-4">
        <div className="text-muted-foreground flex items-center gap-2 text-xs font-medium tracking-wide uppercase">
          <Icon className="h-4 w-4" aria-hidden="true" />
          {label}
        </div>
        <p className="tabular-nums text-xl font-semibold">{value}</p>
      </CardContent>
    </Card>
  );
}
