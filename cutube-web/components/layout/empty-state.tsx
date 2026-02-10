import type { LucideIcon } from "lucide-react";
import { Button } from "@/components/ui/button";

interface EmptyStateProps {
  icon: LucideIcon;
  title: string;
  description: string;
  action?: {
    label: string;
    onClick: () => void;
  };
}

export function EmptyState({ icon: Icon, title, description, action }: EmptyStateProps) {
  return (
    <div className="border-border/70 bg-card/80 flex flex-col items-center justify-center rounded-xl border border-dashed px-6 py-14 text-center">
      <Icon className="text-muted-foreground/60 mb-4 h-12 w-12" aria-hidden="true" />
      <h3 className="text-lg font-semibold">{title}</h3>
      <p className="text-muted-foreground mt-2 max-w-md text-sm">{description}</p>
      {action ? (
        <Button onClick={action.onClick} variant="outline" className="mt-6">
          {action.label}
        </Button>
      ) : null}
    </div>
  );
}
