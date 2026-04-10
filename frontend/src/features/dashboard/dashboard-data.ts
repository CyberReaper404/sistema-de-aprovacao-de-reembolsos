import { RequestStatus, requestStatusLabels } from "@/types/domain";
import type { DashboardByStatusItem, DashboardSummary } from "@/types/dashboard";
import type { ReimbursementListItem } from "@/types/reimbursements";

export type DashboardStatusFilter = RequestStatus | "all";

export interface DashboardRow {
  id: string;
  requestNumber: string;
  ownerName: string;
  categoryName: string;
  expenseDate: string;
  amount: number;
  currency: string;
  paymentLabel: string;
  status: RequestStatus;
  pdfAvailable: boolean;
  costCenterCode: string;
  description: string;
  decisionReasonLabel?: string;
  decisionComment?: string;
}

export interface DashboardListState {
  items: DashboardRow[];
  page: number;
  totalPages: number;
  totalCount: number;
}

export const dashboardDemoRows: DashboardRow[] = [
  {
    id: "demo-01",
    requestNumber: "RB-2026-0147",
    ownerName: "Mariana Farias",
    categoryName: "Táxi corporativo",
    expenseDate: "2026-03-01",
    amount: 1200,
    currency: "BRL",
    paymentLabel: "Pago via Pix",
    status: RequestStatus.Paid,
    pdfAvailable: true,
    costCenterCode: "COM-014",
    description: "Deslocamento do aeroporto até a reunião com fornecedor em São Paulo.",
    decisionReasonLabel: "Despesa validada",
    decisionComment: "Solicitação aprovada após conferência do comprovante, do centro de custo e do contexto corporativo."
  },
  {
    id: "demo-02",
    requestNumber: "RB-2026-0150",
    ownerName: "Anderson Neves",
    categoryName: "Hospedagem",
    expenseDate: "2026-03-05",
    amount: 4200,
    currency: "BRL",
    paymentLabel: "Pago via transferência",
    status: RequestStatus.Paid,
    pdfAvailable: true,
    costCenterCode: "OPS-022",
    description: "Hospedagem em viagem operacional para visita à unidade do cliente.",
    decisionReasonLabel: "Despesa validada",
    decisionComment: "Aprovada com base em comprovante fiscal legível, agenda de viagem e enquadramento correto da categoria."
  },
  {
    id: "demo-03",
    requestNumber: "RB-2026-0156",
    ownerName: "Renata Souza",
    categoryName: "Equipamento digital",
    expenseDate: "2026-03-08",
    amount: 520,
    currency: "BRL",
    paymentLabel: "Aguardando financeiro",
    status: RequestStatus.Approved,
    pdfAvailable: true,
    costCenterCode: "TEC-010",
    description: "Reposição emergencial de periférico para atendimento externo.",
    decisionReasonLabel: "Despesa validada",
    decisionComment: "Aprovada após validação do comprovante, da urgência operacional e do enquadramento no centro de custo."
  },
  {
    id: "demo-04",
    requestNumber: "RB-2026-0161",
    ownerName: "Thiago Nascimento",
    categoryName: "Alimentação",
    expenseDate: "2026-03-10",
    amount: 700,
    currency: "BRL",
    paymentLabel: "Em análise",
    status: RequestStatus.Submitted,
    pdfAvailable: false,
    costCenterCode: "COM-001",
    description: "Refeição durante agenda comercial fora da sede."
  },
  {
    id: "demo-05",
    requestNumber: "RB-2026-0164",
    ownerName: "Camila Barreto",
    categoryName: "Internet móvel",
    expenseDate: "2026-03-12",
    amount: 300,
    currency: "BRL",
    paymentLabel: "Em análise",
    status: RequestStatus.Submitted,
    pdfAvailable: false,
    costCenterCode: "MKT-004",
    description: "Pacote adicional de dados para cobertura de evento corporativo."
  },
  {
    id: "demo-06",
    requestNumber: "RB-2026-0172",
    ownerName: "Gustavo Ribeiro",
    categoryName: "Hotel",
    expenseDate: "2026-03-15",
    amount: 700,
    currency: "BRL",
    paymentLabel: "Recusado",
    status: RequestStatus.Rejected,
    pdfAvailable: true,
    costCenterCode: "OPS-009",
    description: "Hospedagem declarada sem documentação fiscal compatível.",
    decisionReasonLabel: "Comprovante inválido",
    decisionComment: "Solicitação recusada porque o comprovante anexado não permitiu validar emissor, data e valor da despesa."
  },
  {
    id: "demo-07",
    requestNumber: "RB-2026-0184",
    ownerName: "Larissa Paiva",
    categoryName: "Hospedagem",
    expenseDate: "2026-03-20",
    amount: 363,
    currency: "BRL",
    paymentLabel: "Recusado",
    status: RequestStatus.Rejected,
    pdfAvailable: true,
    costCenterCode: "COM-018",
    description: "Reembolso de hospedagem enviado após o prazo interno da categoria.",
    decisionReasonLabel: "Fora do prazo",
    decisionComment: "Solicitação recusada porque a despesa foi enviada após a janela permitida para prestação de contas."
  },
  {
    id: "demo-08",
    requestNumber: "RB-2026-0193",
    ownerName: "Kaio Barreto",
    categoryName: "Equipamento digital",
    expenseDate: "2026-03-25",
    amount: 120,
    currency: "BRL",
    paymentLabel: "Pago via Pix",
    status: RequestStatus.Paid,
    pdfAvailable: true,
    costCenterCode: "TEC-011",
    description: "Aquisição de adaptador para montagem de estação de atendimento em evento.",
    decisionReasonLabel: "Despesa validada",
    decisionComment: "Aprovada com base em nota fiscal, necessidade operacional e valor compatível com a categoria."
  },
  {
    id: "demo-09",
    requestNumber: "RB-2026-0201",
    ownerName: "Mônica Tavares",
    categoryName: "Táxi corporativo",
    expenseDate: "2026-03-31",
    amount: 938,
    currency: "BRL",
    paymentLabel: "Aguardando financeiro",
    status: RequestStatus.Approved,
    pdfAvailable: true,
    costCenterCode: "COM-014",
    description: "Deslocamentos de campo vinculados a agenda externa com cliente estratégico.",
    decisionReasonLabel: "Despesa validada",
    decisionComment: "Solicitação aprovada após conferência do comprovante e confirmação do vínculo da despesa com a agenda comercial."
  },
  {
    id: "demo-10",
    requestNumber: "RB-2026-0208",
    ownerName: "Douglas Bueno",
    categoryName: "Alimentação",
    expenseDate: "2026-04-01",
    amount: 1200,
    currency: "BRL",
    paymentLabel: "Em análise",
    status: RequestStatus.Submitted,
    pdfAvailable: false,
    costCenterCode: "OPS-030",
    description: "Refeições durante deslocamento de equipe em agenda operacional."
  }
];

export function formatMoney(value: number, currency = "BRL") {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency
  }).format(value);
}

export function formatDate(value: string) {
  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric"
  }).format(new Date(`${value}T00:00:00`));
}

export function formatDateRangeLabel(from: string, to: string) {
  const formatter = new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "short",
    year: "numeric"
  });

  return `${formatter.format(new Date(`${from}T00:00:00`))} - ${formatter.format(new Date(`${to}T00:00:00`))}`;
}

export function createDefaultRange() {
  const today = new Date();
  const from = new Date(today);
  from.setDate(today.getDate() - 29);

  return {
    from: from.toISOString().slice(0, 10),
    to: today.toISOString().slice(0, 10)
  };
}

export function buildSummary(rows: DashboardRow[]): DashboardSummary {
  const paidRows = rows.filter((row) => row.status === RequestStatus.Paid);
  const approvedRows = rows.filter((row) => row.status === RequestStatus.Approved || row.status === RequestStatus.Paid);

  return {
    totalRequests: rows.length,
    pendingRequests: rows.filter((row) => row.status === RequestStatus.Submitted).length,
    approvedRequests: rows.filter((row) => row.status === RequestStatus.Approved).length,
    paidRequests: paidRows.length,
    totalApprovedAmount: approvedRows.reduce((total, row) => total + row.amount, 0),
    totalPaidAmount: paidRows.reduce((total, row) => total + row.amount, 0)
  };
}

export function buildStatusSummary(rows: DashboardRow[]): DashboardByStatusItem[] {
  return [RequestStatus.Submitted, RequestStatus.Approved, RequestStatus.Rejected, RequestStatus.Paid].map((status) => {
    const items = rows.filter((row) => row.status === status);

    return {
      status,
      totalRequests: items.length,
      totalAmount: items.reduce((total, item) => total + item.amount, 0)
    };
  });
}

export function getStatusCount(items: DashboardByStatusItem[], status: RequestStatus) {
  return items.find((item) => item.status === status)?.totalRequests ?? 0;
}

export function mapRowsFromList(items: ReimbursementListItem[], currentUserName: string): DashboardRow[] {
  return items.map((item) => ({
    id: item.id,
    requestNumber: item.requestNumber,
    ownerName: currentUserName,
    categoryName: item.categoryName,
    expenseDate: item.expenseDate,
    amount: item.amount,
    currency: item.currency,
    paymentLabel:
      item.status === RequestStatus.Paid
        ? "Pago"
        : item.status === RequestStatus.Approved
          ? "Aguardando financeiro"
          : item.status === RequestStatus.Rejected
            ? "Recusado"
            : item.status === RequestStatus.Draft
              ? "Rascunho"
              : "Em análise",
    status: item.status,
    pdfAvailable: item.status === RequestStatus.Approved || item.status === RequestStatus.Paid || item.status === RequestStatus.Rejected,
    costCenterCode: item.costCenterCode,
    description: item.title
  }));
}

export function buildPageNumbers(currentPage: number, totalPages: number) {
  const pages: number[] = [];
  const start = Math.max(1, currentPage - 1);
  const end = Math.min(totalPages, Math.max(1, currentPage + 2));

  for (let value = start; value <= end; value += 1) {
    pages.push(value);
  }

  return pages;
}

export function buildDemoState(statusFilter: DashboardStatusFilter, search: string, page: number, pageSize: number) {
  const normalizedSearch = search.trim().toLowerCase();
  const filteredRows = dashboardDemoRows.filter((row) => {
    const matchesStatus = statusFilter === "all" || row.status === statusFilter;
    const matchesSearch =
      normalizedSearch.length === 0 ||
      row.requestNumber.toLowerCase().includes(normalizedSearch) ||
      row.ownerName.toLowerCase().includes(normalizedSearch) ||
      row.categoryName.toLowerCase().includes(normalizedSearch);

    return matchesStatus && matchesSearch;
  });

  const totalPages = Math.max(1, Math.ceil(filteredRows.length / pageSize));
  const safePage = Math.min(page, totalPages);
  const start = (safePage - 1) * pageSize;

  return {
    summary: buildSummary(filteredRows),
    byStatus: buildStatusSummary(filteredRows),
    listState: {
      items: filteredRows.slice(start, start + pageSize),
      page: safePage,
      totalPages,
      totalCount: filteredRows.length
    }
  };
}

export function downloadRowsAsCsv(items: DashboardRow[]) {
  const header = ["Protocolo", "Colaborador", "Categoria", "Data", "Valor", "Pagamento", "Status"];
  const rows = items.map((item) => [
    item.requestNumber,
    item.ownerName,
    item.categoryName,
    formatDate(item.expenseDate),
    item.amount.toFixed(2),
    item.paymentLabel,
    requestStatusLabels[item.status]
  ]);

  const csv = [header, ...rows]
    .map((row) => row.map((value) => `"${String(value).replace(/"/g, '""')}"`).join(";"))
    .join("\n");

  const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = "painel-reembolsos.csv";
  link.click();
  URL.revokeObjectURL(url);
}
