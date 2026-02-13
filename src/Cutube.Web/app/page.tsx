import { DownloadList } from "@/components/downloads/download-list";
import { DownloadForm } from "@/components/downloads/download-form";
import { PageContainer } from "@/components/layout/page-container";
import { Section } from "@/components/layout/section";

export default function Home() {
  return (
    <PageContainer>
      <PageContainer.Header
        title="Dashboard"
        description="Gerencie downloads de videos com progresso em tempo real."
      />
      <PageContainer.Content>
        <Section title="Criar download">
          <DownloadForm />
        </Section>

        <Section
          title="Downloads recentes"
          description="Atualizacao automatica a cada 5 segundos e eventos em tempo real via SignalR."
        >
          <DownloadList limit={12} />
        </Section>
      </PageContainer.Content>
    </PageContainer>
  );
}
