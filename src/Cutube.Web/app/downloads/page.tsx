import { DownloadList } from "@/components/downloads/download-list";
import { PageContainer } from "@/components/layout/page-container";
import { Section } from "@/components/layout/section";

export default function DownloadsPage() {
  return (
    <PageContainer>
      <PageContainer.Header
        title="Todos os downloads"
        description="Visualize historico, status e andamento de cada download."
      />

      <PageContainer.Content>
        <Section>
          <DownloadList />
        </Section>
      </PageContainer.Content>
    </PageContainer>
  );
}
