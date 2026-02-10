import { AlertCircle, RotateCcw } from "lucide-react";
import { Button } from "@/components/ui/button";

interface ErrorMessageProps {
  message: string;
  onRetry?: () => void;
}

export function ErrorMessage({ message, onRetry }: ErrorMessageProps) {
  return (
    <div
      className="border-destructive/30 bg-destructive/10 text-destructive flex items-start gap-3 rounded-lg border px-4 py-3"
      role="alert"
    >
      <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
      <div className="flex-1">
        <p className="text-sm">{message}</p>
      </div>
      {onRetry ? (
        <Button variant="ghost" size="sm" onClick={onRetry} className="h-8 gap-1">
          <RotateCcw className="h-3.5 w-3.5" />
          Tentar novamente
        </Button>
      ) : null}
    </div>
  );
}
