import { ArrowLeft } from "lucide-react";
import Link from "next/link";
import { DownloadDetail } from "@/components/downloads/download-detail";
import { PageContainer } from "@/components/layout/page-container";
import { Button } from "@/components/ui/button";

interface DownloadDetailPageProps {
  params: Promise<{ id: string }>;
}

export default async function DownloadDetailPage({ params }: DownloadDetailPageProps) {
  const { id } = await params;

  return (
    <PageContainer>
      <PageContainer.Header
        title="Detalhes do download"
        description={`Acompanhamento completo do download ${id}.`}
        actions={
          <Button asChild variant="outline">
            <Link href="/downloads">
              <ArrowLeft className="h-4 w-4" />
              Voltar para lista
            </Link>
          </Button>
        }
      />

      <PageContainer.Content>
        <DownloadDetail id={id} />
      </PageContainer.Content>
    </PageContainer>
  );
}
