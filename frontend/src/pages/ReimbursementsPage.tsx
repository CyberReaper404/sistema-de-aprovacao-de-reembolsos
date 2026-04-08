import { TablePreview } from "@/components/data/TablePreview";
import { StatusBadge } from "@/components/display/StatusBadge";
import { PageHeader } from "@/components/layout/PageHeader";

const reimbursementColumns = [
  { key: "requestNumber", label: "Solicitação" },
  { key: "category", label: "Categoria" },
  { key: "expenseDate", label: "Despesa" },
  { key: "amount", label: "Valor", align: "right" as const },
  { key: "status", label: "Status", align: "right" as const }
];

const reimbursementRows = [
  {
    requestNumber: "RB-2026-0038",
    category: "Viagem",
    expenseDate: "03/04/2026",
    amount: "R$ 1.240,00",
    status: <StatusBadge tone="amber">Em análise</StatusBadge>
  },
  {
    requestNumber: "RB-2026-0036",
    category: "Transporte",
    expenseDate: "02/04/2026",
    amount: "R$ 94,20",
    status: <StatusBadge tone="green">Aprovada</StatusBadge>
  },
  {
    requestNumber: "RB-2026-0032",
    category: "Hospedagem",
    expenseDate: "30/03/2026",
    amount: "R$ 830,00",
    status: <StatusBadge tone="slate">Paga</StatusBadge>
  }
];

export function ReimbursementsPage() {
  return (
    <div className="workspace-page">
      <PageHeader
        eyebrow="Solicitações"
        title="Acompanhar solicitações"
        description="A tabela final com filtros e paginação entra na próxima rodada funcional. Aqui, a base visual já segue o padrão do sistema."
        actions={
          <button className="ui-button ui-button--primary" type="button">
            Nova solicitação
          </button>
        }
      />

      <section className="workspace-section">
        <div className="workspace-toolbar">
          <div className="workspace-search">
            <input type="search" placeholder="Buscar por número da solicitação" />
          </div>
          <div className="workspace-toolbar__actions">
            <button className="ui-button ui-button--ghost" type="button">
              Status
            </button>
            <button className="ui-button ui-button--ghost" type="button">
              Categoria
            </button>
            <button className="ui-button ui-button--ghost" type="button">
              Período
            </button>
          </div>
        </div>

        <TablePreview columns={reimbursementColumns} rows={reimbursementRows} />
      </section>
    </div>
  );
}
