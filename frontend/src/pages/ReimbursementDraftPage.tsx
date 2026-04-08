import { PlaceholderState } from "@/components/feedback/PlaceholderState";
import { PageHeader } from "@/components/layout/PageHeader";

export function ReimbursementDraftPage() {
  return (
    <div className="workspace-page">
      <PageHeader
        eyebrow="Rascunho"
        title="Criar ou editar solicitação"
        description="O formulário final entra na próxima rodada funcional, reaproveitando esta base visual e o espaçamento já definidos."
      />

      <PlaceholderState
        eyebrow="Próxima etapa"
        title="Estrutura do formulário reservada"
        description="Seções de dados da despesa, comprovantes e ações de envio entram na rodada de criação e edição."
      />
    </div>
  );
}
