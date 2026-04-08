import { PlaceholderState } from "@/components/feedback/PlaceholderState";
import { PageHeader } from "@/components/layout/PageHeader";

export function ReimbursementDetailPage() {
  return (
    <div className="workspace-page">
      <PageHeader
        eyebrow="Solicitação"
        title="Detalhe da solicitação"
        description="A composição da página já segue o padrão visual do sistema. O conteúdo final de dados, anexos e histórico entra na rodada específica do detalhe."
      />

      <PlaceholderState
        eyebrow="Próxima etapa"
        title="Detalhamento reservado"
        description="Dados completos da despesa, anexos e histórico de workflow serão conectados na rodada funcional desta tela."
      />
    </div>
  );
}
