import { TablePreview } from "@/components/data/TablePreview";
import { StatusBadge } from "@/components/display/StatusBadge";
import { PageHeader } from "@/components/layout/PageHeader";

const paymentColumns = [
  { key: "requestNumber", label: "Solicitação" },
  { key: "beneficiary", label: "Colaborador" },
  { key: "method", label: "Pagamento" },
  { key: "amount", label: "Valor", align: "right" as const },
  { key: "status", label: "Situação", align: "right" as const }
];

const paymentRows = [
  {
    requestNumber: "RB-2026-0032",
    beneficiary: "Camila Soares",
    method: "PIX",
    amount: "R$ 830,00",
    status: <StatusBadge tone="slate">Liquidado</StatusBadge>
  },
  {
    requestNumber: "RB-2026-0030",
    beneficiary: "Bruno Azevedo",
    method: "Transferência",
    amount: "R$ 560,00",
    status: <StatusBadge tone="amber">Pendente</StatusBadge>
  }
];

export function PaymentsPage() {
  return (
    <div className="workspace-page">
      <PageHeader
        eyebrow="Pagamentos"
        title="Painel de pagamentos"
        description="A base visual do financeiro já está pronta para a rodada operacional de consulta e registro."
        actions={
          <button className="ui-button ui-button--secondary" type="button">
            Exportar
          </button>
        }
      />

      <section className="workspace-section">
        <div className="workspace-toolbar">
          <div className="workspace-search">
            <input type="search" placeholder="Buscar por solicitação ou colaborador" />
          </div>
          <div className="workspace-toolbar__actions">
            <button className="ui-button ui-button--ghost" type="button">
              Método
            </button>
            <button className="ui-button ui-button--ghost" type="button">
              Período
            </button>
          </div>
        </div>

        <TablePreview columns={paymentColumns} rows={paymentRows} />
      </section>
    </div>
  );
}
