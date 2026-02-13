import { Skeleton } from "@/components/ui/skeleton";

interface LoadingSkeletonProps {
  variant?: "card" | "list" | "detail";
  count?: number;
}

export function LoadingSkeleton({ variant = "card", count = 1 }: LoadingSkeletonProps) {
  if (variant === "detail") {
    return (
      <div className="grid gap-4 lg:grid-cols-[2fr_1fr]">
        <Skeleton className="h-72 rounded-xl" />
        <div className="space-y-4">
          <Skeleton className="h-24 rounded-xl" />
          <Skeleton className="h-24 rounded-xl" />
          <Skeleton className="h-24 rounded-xl" />
        </div>
      </div>
    );
  }

  if (variant === "list") {
    return (
      <div className="space-y-3">
        {Array.from({ length: count }).map((_, index) => (
          <Skeleton key={`list-skeleton-${index}`} className="h-14 rounded-lg" />
        ))}
      </div>
    );
  }

  return (
    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
      {Array.from({ length: count }).map((_, index) => (
        <Skeleton key={`card-skeleton-${index}`} className="h-48 rounded-xl" />
      ))}
    </div>
  );
}
