import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

interface PageContainerProps {
  children: ReactNode;
  className?: string;
}

interface PageHeaderProps {
  title: string;
  description?: string;
  actions?: ReactNode;
}

function Header({ title, description, actions }: PageHeaderProps) {
  return (
    <header className="mb-8 flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
      <div className="space-y-1">
        <h1 className="text-3xl font-bold tracking-tight">{title}</h1>
        {description ? <p className="text-muted-foreground text-base">{description}</p> : null}
      </div>
      {actions ? <div className="flex items-center gap-2">{actions}</div> : null}
    </header>
  );
}

function Content({ children }: { children: ReactNode }) {
  return <div className="space-y-8">{children}</div>;
}

export function PageContainer({ children, className }: PageContainerProps) {
  return (
    <div className={cn("mx-auto w-full max-w-7xl px-4 py-8 md:px-6 lg:px-8", className)}>
      {children}
    </div>
  );
}

PageContainer.Header = Header;
PageContainer.Content = Content;
