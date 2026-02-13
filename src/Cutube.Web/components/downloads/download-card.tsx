import Link from "next/link";
import { HardDrive, Link2, Timer } from "lucide-react";
import { DateDisplay } from "@/components/data-display/date-display";
import { ProgressBar } from "@/components/data-display/progress-bar";
import { StatusBadge } from "@/components/feedback/status-badge";
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import type { DownloadSummary } from "@/types";

interface DownloadCardProps {
  download: DownloadSummary;
}

export function DownloadCard({ download }: DownloadCardProps) {
  return (
    <Card className="flex h-full flex-col transition-all duration-200 ease-out hover:-translate-y-0.5 hover:shadow-md">
      <CardHeader className="space-y-3">
        <div className="flex items-start justify-between gap-3">
          <CardTitle className="text-base leading-6 break-all">{download.url}</CardTitle>
          <StatusBadge status={download.status} />
        </div>
      </CardHeader>

      <CardContent className="flex-1 space-y-4">
        <ProgressBar value={download.progress} status={download.status} />

        <div className="text-muted-foreground space-y-2 text-sm">
          <p className="inline-flex items-center gap-2 break-all">
            <Link2 className="h-4 w-4" aria-hidden="true" />
            <span className="line-clamp-1">{download.downloadId}</span>
          </p>
          <p className="inline-flex items-center gap-2">
            <Timer className="h-4 w-4" aria-hidden="true" />
            <DateDisplay value={download.createdAt} />
          </p>
          {download.filePath ? (
            <p className="inline-flex items-center gap-2 break-all">
              <HardDrive className="h-4 w-4" aria-hidden="true" />
              {download.filePath}
            </p>
          ) : null}
        </div>
      </CardContent>

      <CardFooter>
        <Link
          href={`/downloads/${download.downloadId}`}
          className="text-primary text-sm font-medium hover:underline"
        >
          Ver detalhes
        </Link>
      </CardFooter>
    </Card>
  );
}
