import { MetricCard } from "@/components/data/MetricCard";
import { TablePreview } from "@/components/data/TablePreview";
import { StatusBadge } from "@/components/display/StatusBadge";
import { PageHeader } from "@/components/layout/PageHeader";

const dashboardColumns = [
  { key: "requestNumber", label: "Solicitação" },
  { key: "collaborator", label: "Colaborador" },
  { key: "category", label: "Categoria" },
  { key: "date", label: "Data" },
  { key: "amount", label: "Valor", align: "right" as const },
  { key: "status", label: "Status", align: "right" as const }
];

const dashboardRows = [
  {
    requestNumber: "RB-2026-0038",
    collaborator: "Marina Prado",
    category: "Viagem",
    date: "03/04/2026",
    amount: "R$ 1.240,00",
    status: <StatusBadge tone="amber">Em análise</StatusBadge>
  },
  {
    requestNumber: "RB-2026-0034",
    collaborator: "Leonardo Costa",
    category: "Alimentação",
    date: "02/04/2026",
    amount: "R$ 186,50",
    status: <StatusBadge tone="green">Aprovada</StatusBadge>
  },
  {
    requestNumber: "RB-2026-0031",
    collaborator: "Camila Soares",
    category: "Hospedagem",
    date: "01/04/2026",
    amount: "R$ 930,00",
    status: <StatusBadge tone="red">Recusada</StatusBadge>
  },
  {
    requestNumber: "RB-2026-0027",
    collaborator: "Paulo Mendes",
    category: "Transporte",
    date: "31/03/2026",
    amount: "R$ 412,80",
    status: <StatusBadge tone="slate">Paga</StatusBadge>
  }
];

const dashboardTabs = [
  { label: "Todas", value: "84", active: true },
  { label: "Em análise", value: "19" },
  { label: "Aprovadas", value: "28" },
  { label: "Recusadas", value: "11" },
  { label: "Pagas", value: "26" }
];

export function DashboardPage() {
  return (
    <div className="workspace-page">
      <PageHeader
        eyebrow="Painel"
        title="Gerenciar reembolsos"
        description="Acompanhe solicitações, aprovações e pagamentos em um único fluxo operacional."
        actions={
          <>
            <button className="ui-button ui-button--secondary" type="button">
              Exportar resumo
            </button>
            <button className="ui-button ui-button--primary" type="button">
              Nova solicitação
            </button>
          </>
        }
      />

      <section className="workspace-section">
        <div className="workspace-tabs">
          {dashboardTabs.map((tab) => (
            <button
              key={tab.label}
              className={tab.active ? "workspace-tabs__item workspace-tabs__item--active" : "workspace-tabs__item"}
              type="button"
            >
              {tab.label}
              <span>{tab.value}</span>
            </button>
          ))}
        </div>

        <div className="workspace-toolbar">
          <div className="workspace-search">
            <input type="search" placeholder="Buscar por número ou colaborador" />
          </div>
          <div className="workspace-toolbar__actions">
            <button className="ui-button ui-button--ghost" type="button">
              Abr 2026
            </button>
            <button className="ui-button ui-button--ghost" type="button">
              Filtros
            </button>
          </div>
        </div>

        <div className="metric-grid">
          <MetricCard label="Solicitações abertas" value="19" hint="Itens aguardando decisão nesta semana." />
          <MetricCard label="Aprovadas no mês" value="28" hint="Fluxo liberado para pagamento ou conferência." />
          <MetricCard label="Valor aprovado" value="R$ 18.420" hint="Somatório das aprovações em abril." />
          <MetricCard label="Valor pago" value="R$ 14.930" hint="Pagamentos já liquidados no período." />
        </div>

        <TablePreview columns={dashboardColumns} rows={dashboardRows} />
      </section>
    </div>
  );
}
