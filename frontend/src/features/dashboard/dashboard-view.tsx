import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { StatusBadge } from "@/components/display/StatusBadge";
import { useSession } from "@/features/auth/session-context";
import {
  type DashboardListState,
  type DashboardRow,
  type DashboardStatusFilter,
  buildDemoState,
  buildPageNumbers,
  createDefaultRange,
  downloadRowsAsCsv,
  formatDate,
  formatDateRangeLabel,
  formatMoney,
  getStatusCount,
  mapRowsFromList
} from "@/features/dashboard/dashboard-data";
import { downloadProtocolPdf } from "@/features/reimbursements/protocol-pdf";
import { dashboardService, reimbursementService } from "@/services/api";
import type { DashboardByStatusItem, DashboardSummary } from "@/types/dashboard";
import { decisionReasonLabels, RequestStatus, UserRole, requestStatusLabels } from "@/types/domain";
import type { ReimbursementDetail } from "@/types/reimbursements";

const sortOptions = [
  { value: "createdAt:desc", label: "Mais recentes" },
  { value: "expenseDate:desc", label: "Despesa mais recente" },
  { value: "amount:desc", label: "Maior valor" }
] as const;

const statusToneMap: Record<RequestStatus, "amber" | "green" | "red" | "slate"> = {
  [RequestStatus.Draft]: "slate",
  [RequestStatus.Submitted]: "amber",
  [RequestStatus.Approved]: "green",
  [RequestStatus.Rejected]: "red",
  [RequestStatus.Paid]: "slate"
};

function emptySummary(): DashboardSummary {
  return {
    totalRequests: 0,
    pendingRequests: 0,
    approvedRequests: 0,
    paidRequests: 0,
    totalApprovedAmount: 0,
    totalPaidAmount: 0
  };
}

function buildDecisionReason(detail: ReimbursementDetail) {
  if (detail.decisionReasonCode) {
    return decisionReasonLabels[detail.decisionReasonCode];
  }

  if (detail.status === RequestStatus.Approved || detail.status === RequestStatus.Paid) {
    return "Despesa validada";
  }

  if (detail.status === RequestStatus.Rejected) {
    return "Solicitação recusada";
  }

  return "Motivo operacional registrado";
}

function buildDecisionComment(detail: ReimbursementDetail) {
  if (detail.decisionComment) {
    return detail.decisionComment;
  }

  if (detail.rejectionReason) {
    return detail.rejectionReason;
  }

  if (detail.status === RequestStatus.Approved || detail.status === RequestStatus.Paid) {
    return "A solicitação foi aprovada após conferência do comprovante, do valor e do enquadramento da despesa.";
  }

  if (detail.status === RequestStatus.Rejected) {
    return "A solicitação foi recusada após análise humana do comprovante e da política interna.";
  }

  return "Sem observação adicional registrada.";
}

export function DashboardView() {
  const { session } = useSession();
  const defaultRange = createDefaultRange();
  const [from, setFrom] = useState(defaultRange.from);
  const [to, setTo] = useState(defaultRange.to);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<DashboardStatusFilter>("all");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [sort, setSort] = useState<(typeof sortOptions)[number]["value"]>("createdAt:desc");
  const [summary, setSummary] = useState<DashboardSummary>(emptySummary());
  const [byStatus, setByStatus] = useState<DashboardByStatusItem[]>([]);
  const [listState, setListState] = useState<DashboardListState>({
    items: [],
    page: 1,
    totalPages: 1,
    totalCount: 0
  });
  const [isLoading, setIsLoading] = useState(true);
  const [isDemoMode, setIsDemoMode] = useState(false);
  const [showFilters, setShowFilters] = useState(false);
  const [downloadingId, setDownloadingId] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadDashboard() {
      setIsLoading(true);

      try {
        const query = { from, to };
        const [summaryResponse, statusResponse, listResponse] = await Promise.all([
          dashboardService.getSummary(query),
          dashboardService.getByStatus(query),
          reimbursementService.getPaged({
            page,
            pageSize,
            sort,
            status: statusFilter === "all" ? undefined : statusFilter,
            requestNumber: search.trim() || undefined,
            expenseDateFrom: from,
            expenseDateTo: to
          })
        ]);

        if (cancelled) {
          return;
        }

        if (listResponse.items.length > 0) {
          setSummary(summaryResponse);
          setByStatus(statusResponse);
          setListState({
            items: mapRowsFromList(listResponse.items, session?.user.fullName ?? "Colaborador interno"),
            page: listResponse.page,
            totalPages: listResponse.totalPages,
            totalCount: listResponse.totalItems
          });
          setIsDemoMode(false);
        } else {
          const demo = buildDemoState(statusFilter, search, page, pageSize);
          setSummary(demo.summary);
          setByStatus(demo.byStatus);
          setListState(demo.listState);
          setIsDemoMode(true);
        }
      } catch {
        if (cancelled) {
          return;
        }

        const demo = buildDemoState(statusFilter, search, page, pageSize);
        setSummary(demo.summary);
        setByStatus(demo.byStatus);
        setListState(demo.listState);
        setIsDemoMode(true);
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadDashboard();

    return () => {
      cancelled = true;
    };
  }, [from, page, pageSize, search, session?.user.fullName, sort, statusFilter, to]);

  const visiblePages = useMemo(() => buildPageNumbers(listState.page, listState.totalPages), [listState.page, listState.totalPages]);
  const primaryActionLabel = session?.user.role === UserRole.Collaborator ? "Nova solicitação" : "Abrir fila";
  const primaryActionTo = session?.user.role === UserRole.Collaborator ? "/solicitacoes/nova" : "/solicitacoes";

  const handleDownloadProtocol = async (row: DashboardRow) => {
    if (!row.pdfAvailable) {
      return;
    }

    try {
      setDownloadingId(row.id);

      if (row.id.startsWith("demo-")) {
        await downloadProtocolPdf({
          requestNumber: row.requestNumber,
          ownerName: row.ownerName,
          categoryName: row.categoryName,
          amount: row.amount,
          currency: row.currency,
          expenseDate: row.expenseDate,
          description: row.description,
          status: row.status,
          costCenterCode: row.costCenterCode,
          issuedAt: new Date().toISOString(),
          decisionReasonLabel: row.decisionReasonLabel,
          decisionComment: row.decisionComment
        });
        return;
      }

      const detail = await reimbursementService.getById(row.id);

      await downloadProtocolPdf({
        requestNumber: detail.requestNumber,
        ownerName: detail.createdByUserName,
        categoryName: detail.categoryName,
        amount: detail.amount,
        currency: detail.currency,
        expenseDate: detail.expenseDate,
        description: detail.description,
        status: detail.status,
        costCenterCode: detail.costCenterCode,
        issuedAt: new Date().toISOString(),
        decisionReasonLabel: buildDecisionReason(detail),
        decisionComment: buildDecisionComment(detail)
      });
    } finally {
      setDownloadingId(null);
    }
  };

  return (
    <section className="operations-page">
      <header className="operations-page__header">
        <div>
          <h1>Gerenciar solicitações</h1>
          <p>Visualize toda a fila de reembolsos em uma operação única, clara e sem ruído.</p>
        </div>

        <div className="operations-page__header-actions">
          <button className="ops-button ops-button--secondary" type="button" onClick={() => downloadRowsAsCsv(listState.items)}>
            Exportar painel
          </button>
          <Link className="ops-button ops-button--primary" to={primaryActionTo}>
            + {primaryActionLabel}
          </Link>
        </div>
      </header>

      <div className="operations-tabs" role="tablist" aria-label="Filtros de status do painel">
        <button
          type="button"
          className={statusFilter === "all" ? "operations-tabs__item operations-tabs__item--active" : "operations-tabs__item"}
          onClick={() => {
            setStatusFilter("all");
            setPage(1);
          }}
        >
          Todas <strong>{summary.totalRequests}</strong>
        </button>
        <button
          type="button"
          className={statusFilter === RequestStatus.Submitted ? "operations-tabs__item operations-tabs__item--active" : "operations-tabs__item"}
          onClick={() => {
            setStatusFilter(RequestStatus.Submitted);
            setPage(1);
          }}
        >
          Em análise <strong>{getStatusCount(byStatus, RequestStatus.Submitted)}</strong>
        </button>
        <button
          type="button"
          className={statusFilter === RequestStatus.Approved ? "operations-tabs__item operations-tabs__item--active" : "operations-tabs__item"}
          onClick={() => {
            setStatusFilter(RequestStatus.Approved);
            setPage(1);
          }}
        >
          Aprovadas <strong>{getStatusCount(byStatus, RequestStatus.Approved)}</strong>
        </button>
        <button
          type="button"
          className={statusFilter === RequestStatus.Rejected ? "operations-tabs__item operations-tabs__item--active" : "operations-tabs__item"}
          onClick={() => {
            setStatusFilter(RequestStatus.Rejected);
            setPage(1);
          }}
        >
          Recusadas <strong>{getStatusCount(byStatus, RequestStatus.Rejected)}</strong>
        </button>
        <button
          type="button"
          className={statusFilter === RequestStatus.Paid ? "operations-tabs__item operations-tabs__item--active" : "operations-tabs__item"}
          onClick={() => {
            setStatusFilter(RequestStatus.Paid);
            setPage(1);
          }}
        >
          Pagas <strong>{getStatusCount(byStatus, RequestStatus.Paid)}</strong>
        </button>
      </div>

      <div className="operations-toolbar">
        <label className="ops-search-field">
          <span className="sr-only">Buscar por protocolo</span>
          <input
            type="search"
            value={search}
            onChange={(event) => {
              setSearch(event.target.value);
              setPage(1);
            }}
            placeholder="Buscar por protocolo"
          />
        </label>

        <div className="operations-toolbar__actions">
          <div className="ops-date-range">
            <span>{formatDateRangeLabel(from, to)}</span>
            <div className="ops-date-range__fields">
              <input
                type="date"
                value={from}
                onChange={(event) => {
                  setFrom(event.target.value);
                  setPage(1);
                }}
              />
              <input
                type="date"
                value={to}
                onChange={(event) => {
                  setTo(event.target.value);
                  setPage(1);
                }}
              />
            </div>
          </div>

          <button className="ops-button ops-button--secondary ops-button--compact" type="button" onClick={() => setShowFilters((current) => !current)}>
            Filtros
          </button>
        </div>
      </div>

      {showFilters ? (
        <div className="operations-subtoolbar">
          <label className="ops-inline-field">
            <span>Ordenação</span>
            <select value={sort} onChange={(event) => setSort(event.target.value as (typeof sortOptions)[number]["value"])}>
              {sortOptions.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>

          <label className="ops-inline-field">
            <span>Linhas</span>
            <select
              value={pageSize}
              onChange={(event) => {
                setPageSize(Number(event.target.value));
                setPage(1);
              }}
            >
              {[10, 15, 20].map((size) => (
                <option key={size} value={size}>
                  {size}
                </option>
              ))}
            </select>
          </label>
        </div>
      ) : null}

      <section className="operations-table">
        {isLoading ? (
          <div className="operations-state">
            <strong>Carregando painel</strong>
            <span>Organizando a fila operacional do período selecionado.</span>
          </div>
        ) : (
          <>
            <table>
              <thead>
                <tr>
                  <th className="operations-table__check" aria-hidden="true" />
                  <th>Protocolo</th>
                  <th>Colaborador</th>
                  <th>Categoria</th>
                  <th>Data</th>
                  <th className="operations-table__right">Valor</th>
                  <th>Pagamento</th>
                  <th>Situação</th>
                  <th className="operations-table__action">Baixar</th>
                </tr>
              </thead>
              <tbody>
                {listState.items.map((item, index) => (
                  <tr key={item.id}>
                    <td className="operations-table__check">
                      <span className={index % 4 === 0 ? "operations-checkbox operations-checkbox--active" : "operations-checkbox"} />
                    </td>
                    <td>
                      <Link className="operations-link" to={`/solicitacoes/${item.id}`}>
                        {item.requestNumber}
                      </Link>
                    </td>
                    <td>{item.ownerName}</td>
                    <td>{item.categoryName}</td>
                    <td>{formatDate(item.expenseDate)}</td>
                    <td className="operations-table__right">{formatMoney(item.amount, item.currency)}</td>
                    <td>{item.paymentLabel}</td>
                    <td>
                      <StatusBadge tone={statusToneMap[item.status]}>{requestStatusLabels[item.status]}</StatusBadge>
                    </td>
                    <td className="operations-table__action">
                      <button
                        type="button"
                        className={
                          item.pdfAvailable ? "download-protocol-button download-protocol-button--enabled" : "download-protocol-button"
                        }
                        onClick={() => void handleDownloadProtocol(item)}
                        disabled={!item.pdfAvailable || downloadingId === item.id}
                        aria-label={item.pdfAvailable ? `Baixar protocolo ${item.requestNumber}` : `Sem protocolo disponível para ${item.requestNumber}`}
                        title={item.pdfAvailable ? "Baixar protocolo em PDF" : "Protocolo ainda indisponível"}
                      >
                        {downloadingId === item.id ? "..." : "↓"}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            <footer className="operations-table__footer">
              <div className="operations-table__footer-copy">
                <strong>{isDemoMode ? "Exibindo base demonstrativa" : `${listState.totalCount} solicitações no período`}</strong>
                <span>
                  {isDemoMode
                    ? "Os protocolos abaixo são exemplos operacionais para manter a visualização fiel enquanto a base real ainda está curta."
                    : `${summary.pendingRequests} solicitações aguardando decisão.`}
                </span>
              </div>

              <div className="operations-pagination">
                <button type="button" onClick={() => setPage((current) => Math.max(1, current - 1))} disabled={listState.page <= 1}>
                  &lt;
                </button>
                {visiblePages.map((pageNumber) => (
                  <button
                    key={pageNumber}
                    type="button"
                    className={pageNumber === listState.page ? "operations-pagination__number operations-pagination__number--active" : "operations-pagination__number"}
                    onClick={() => setPage(pageNumber)}
                  >
                    {pageNumber}
                  </button>
                ))}
                <button
                  type="button"
                  onClick={() => setPage((current) => Math.min(current + 1, listState.totalPages))}
                  disabled={listState.page >= listState.totalPages}
                >
                  &gt;
                </button>
              </div>
            </footer>
          </>
        )}
      </section>
    </section>
  );
}
