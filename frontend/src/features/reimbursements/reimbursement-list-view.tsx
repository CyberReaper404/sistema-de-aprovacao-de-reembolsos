import { startTransition, useDeferredValue, useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { StatusBadge } from "@/components/display/StatusBadge";
import { PlaceholderState } from "@/components/feedback/PlaceholderState";
import { PageHeader } from "@/components/layout/PageHeader";
import { useSession } from "@/features/auth/session-context";
import { reimbursementService } from "@/services/api";
import { ApiError } from "@/services/http/api-error";
import { RequestStatus, UserRole, requestStatusLabels } from "@/types/domain";
import type { ReimbursementListQuery, ReimbursementListResponse } from "@/types/reimbursements";

type StatusTone = "green" | "amber" | "red" | "slate";
type ReimbursementSort =
  | "createdAt:desc"
  | "createdAt:asc"
  | "expenseDate:desc"
  | "expenseDate:asc"
  | "amount:desc"
  | "amount:asc"
  | "status:asc";

interface ListState {
  page: number;
  pageSize: number;
  status?: RequestStatus;
  requestNumber: string;
  expenseDateFrom: string;
  expenseDateTo: string;
  createdByMe: boolean;
  sort: ReimbursementSort;
}

interface StatusTab {
  label: string;
  value?: RequestStatus;
}

const statusTabs: StatusTab[] = [
  { label: "Todas" },
  { label: "Rascunhos", value: RequestStatus.Draft },
  { label: "Em análise", value: RequestStatus.Submitted },
  { label: "Aprovadas", value: RequestStatus.Approved },
  { label: "Recusadas", value: RequestStatus.Rejected },
  { label: "Pagas", value: RequestStatus.Paid }
];

const sortOptions: Array<{ value: ReimbursementSort; label: string }> = [
  { value: "createdAt:desc", label: "Mais recentes" },
  { value: "createdAt:asc", label: "Mais antigas" },
  { value: "expenseDate:desc", label: "Despesa mais recente" },
  { value: "expenseDate:asc", label: "Despesa mais antiga" },
  { value: "amount:desc", label: "Maior valor" },
  { value: "amount:asc", label: "Menor valor" },
  { value: "status:asc", label: "Status" }
];

const currencyFormatters = new Map<string, Intl.NumberFormat>();
const expenseDateFormatter = new Intl.DateTimeFormat("pt-BR", { dateStyle: "short" });
const createdAtFormatter = new Intl.DateTimeFormat("pt-BR", {
  dateStyle: "short",
  timeStyle: "short"
});

function getStatusTone(status: RequestStatus): StatusTone {
  switch (status) {
    case RequestStatus.Approved:
    case RequestStatus.Paid:
      return "green";
    case RequestStatus.Submitted:
      return "amber";
    case RequestStatus.Rejected:
      return "red";
    default:
      return "slate";
  }
}

function formatCurrency(amount: number, currency: string) {
  const normalizedCurrency = currency.toUpperCase();

  if (!currencyFormatters.has(normalizedCurrency)) {
    currencyFormatters.set(
      normalizedCurrency,
      new Intl.NumberFormat("pt-BR", {
        style: "currency",
        currency: normalizedCurrency
      })
    );
  }

  return currencyFormatters.get(normalizedCurrency)?.format(amount) ?? `${amount}`;
}

function formatExpenseDate(value: string) {
  return expenseDateFormatter.format(new Date(`${value}T00:00:00`));
}

function formatCreatedAt(value: string) {
  return createdAtFormatter.format(new Date(value));
}

function getDefaultState(role: UserRole): ListState {
  return {
    page: 1,
    pageSize: 10,
    status: undefined,
    requestNumber: "",
    expenseDateFrom: "",
    expenseDateTo: "",
    createdByMe: role === UserRole.Collaborator,
    sort: "createdAt:desc"
  };
}

export function ReimbursementListView() {
  const { session } = useSession();
  const hasLoadedOnceRef = useRef(false);
  const currentUserRole = session?.user.role ?? UserRole.Collaborator;
  const [listState, setListState] = useState<ListState>(() => getDefaultState(currentUserRole));
  const [response, setResponse] = useState<ReimbursementListResponse | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const deferredRequestNumber = useDeferredValue(listState.requestNumber.trim());
  const hasActiveFilters = Boolean(
    listState.status ||
      listState.requestNumber ||
      listState.expenseDateFrom ||
      listState.expenseDateTo ||
      (currentUserRole !== UserRole.Collaborator && listState.createdByMe) ||
      listState.sort !== "createdAt:desc"
  );

  useEffect(() => {
    let isCancelled = false;

    const loadReimbursements = async () => {
      const isInitialLoad = !hasLoadedOnceRef.current;

      setErrorMessage(null);

      if (isInitialLoad) {
        setIsLoading(true);
      } else {
        setIsRefreshing(true);
      }

      try {
        const query: ReimbursementListQuery = {
          page: listState.page,
          pageSize: listState.pageSize,
          status: listState.status,
          requestNumber: deferredRequestNumber || undefined,
          expenseDateFrom: listState.expenseDateFrom || undefined,
          expenseDateTo: listState.expenseDateTo || undefined,
          createdByMe: listState.createdByMe,
          sort: listState.sort
        };

        const nextResponse = await reimbursementService.getPaged(query);

        if (!isCancelled) {
          hasLoadedOnceRef.current = true;
          setResponse(nextResponse);
        }
      } catch (error) {
        if (!isCancelled) {
          const message =
            error instanceof ApiError
              ? error.message
              : "Não foi possível carregar as solicitações.";

          setErrorMessage(message);
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
          setIsRefreshing(false);
        }
      }
    };

    void loadReimbursements();

    return () => {
      isCancelled = true;
    };
  }, [
    deferredRequestNumber,
    listState.createdByMe,
    listState.expenseDateFrom,
    listState.expenseDateTo,
    listState.page,
    listState.pageSize,
    listState.sort,
    listState.status
  ]);

  const handleStateUpdate = (updater: (current: ListState) => ListState) => {
    startTransition(() => {
      setListState((current) => updater(current));
    });
  };

  const resetFilters = () => {
    const defaultState = getDefaultState(currentUserRole);
    handleStateUpdate(() => defaultState);
  };

  const totalLabel = response
    ? `${response.totalItems} ${response.totalItems === 1 ? "solicitação encontrada" : "solicitações encontradas"}`
    : "Carregando solicitações";

  const canCreate = currentUserRole === UserRole.Collaborator;

  return (
    <div className="workspace-page">
      <PageHeader
        eyebrow="Solicitações"
        title="Acompanhar solicitações"
        description="Consulte o andamento, refine a busca por período e acompanhe a fila de reembolsos sem sair da área principal."
        actions={
          canCreate ? (
            <Link className="ui-button ui-button--primary" to="/solicitacoes/nova">
              Nova solicitação
            </Link>
          ) : null
        }
      />

      <section className="workspace-section">
        <div className="workspace-tabs" role="tablist" aria-label="Filtrar por status">
          {statusTabs.map((tab) => {
            const isActive = tab.value === listState.status || (!tab.value && !listState.status);

            return (
              <button
                key={tab.label}
                className={isActive ? "workspace-tabs__item workspace-tabs__item--active" : "workspace-tabs__item"}
                type="button"
                onClick={() =>
                  handleStateUpdate((current) => ({
                    ...current,
                    page: 1,
                    status: tab.value
                  }))
                }
              >
                {tab.label}
              </button>
            );
          })}
        </div>

        <div className="list-toolbar">
          <div className="list-toolbar__summary" aria-live="polite">
            <strong>{totalLabel}</strong>
            <span>
              {response
                ? `Página ${response.page} de ${Math.max(response.totalPages, 1)}`
                : "Preparando a consulta"}
            </span>
          </div>

          {hasActiveFilters ? (
            <button className="ui-button ui-button--ghost" type="button" onClick={resetFilters}>
              Limpar filtros
            </button>
          ) : null}
        </div>

        <div className="list-filters">
          <label className="filter-field filter-field--search">
            <span>Buscar por número</span>
            <input
              type="search"
              placeholder="Ex.: RB-2026-0038"
              value={listState.requestNumber}
              onChange={(event) =>
                handleStateUpdate((current) => ({
                  ...current,
                  page: 1,
                  requestNumber: event.target.value
                }))
              }
            />
          </label>

          <label className="filter-field">
            <span>Despesa a partir de</span>
            <input
              type="date"
              value={listState.expenseDateFrom}
              onChange={(event) =>
                handleStateUpdate((current) => ({
                  ...current,
                  page: 1,
                  expenseDateFrom: event.target.value
                }))
              }
            />
          </label>

          <label className="filter-field">
            <span>Despesa até</span>
            <input
              type="date"
              value={listState.expenseDateTo}
              onChange={(event) =>
                handleStateUpdate((current) => ({
                  ...current,
                  page: 1,
                  expenseDateTo: event.target.value
                }))
              }
            />
          </label>

          <label className="filter-field">
            <span>Ordenar por</span>
            <select
              value={listState.sort}
              onChange={(event) =>
                handleStateUpdate((current) => ({
                  ...current,
                  page: 1,
                  sort: event.target.value as ReimbursementSort
                }))
              }
            >
              {sortOptions.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>

          <label className="filter-field">
            <span>Itens por página</span>
            <select
              value={listState.pageSize}
              onChange={(event) =>
                handleStateUpdate((current) => ({
                  ...current,
                  page: 1,
                  pageSize: Number(event.target.value)
                }))
              }
            >
              {[10, 20, 50].map((option) => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </select>
          </label>
        </div>

        {currentUserRole !== UserRole.Collaborator ? (
          <label className="filter-toggle">
            <input
              type="checkbox"
              checked={listState.createdByMe}
              onChange={(event) =>
                handleStateUpdate((current) => ({
                  ...current,
                  page: 1,
                  createdByMe: event.target.checked
                }))
              }
            />
            <span>Apenas solicitações criadas por mim</span>
          </label>
        ) : null}

        {errorMessage && response ? <div className="inline-feedback inline-feedback--error">{errorMessage}</div> : null}
        {isRefreshing && response ? <div className="inline-feedback">Atualizando a listagem...</div> : null}

        {isLoading && !response ? (
          <div className="table-state">
            <strong>Carregando solicitações...</strong>
            <span>Estamos consultando os dados mais recentes do backend.</span>
          </div>
        ) : null}

        {!isLoading && !response && errorMessage ? (
          <PlaceholderState
            eyebrow="Falha na consulta"
            title="Não foi possível carregar a listagem"
            description={errorMessage}
          />
        ) : null}

        {response && response.items.length === 0 ? (
          <div className="table-state">
            <strong>Nenhuma solicitação encontrada</strong>
            <span>Ajuste os filtros ou limpe a busca para ampliar o resultado.</span>
          </div>
        ) : null}

        {response && response.items.length > 0 ? (
          <>
            <div className="table-preview">
              <table>
                <thead>
                  <tr>
                    <th>Solicitação</th>
                    <th>Categoria</th>
                    <th>Centro de custo</th>
                    <th>Data da despesa</th>
                    <th className="table-preview__cell--right">Valor</th>
                    <th className="table-preview__cell--right">Criada em</th>
                    <th className="table-preview__cell--right">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {response.items.map((item) => (
                    <tr key={item.id}>
                      <td>
                        <div className="request-cell">
                          <Link className="table-link" to={`/solicitacoes/${item.id}`}>
                            {item.requestNumber}
                          </Link>
                          <span>{item.title}</span>
                        </div>
                      </td>
                      <td>{item.categoryName}</td>
                      <td>{item.costCenterCode}</td>
                      <td>{formatExpenseDate(item.expenseDate)}</td>
                      <td className="table-preview__cell--right">
                        {formatCurrency(item.amount, item.currency)}
                      </td>
                      <td className="table-preview__cell--right">
                        {formatCreatedAt(item.createdAt)}
                      </td>
                      <td className="table-preview__cell--right">
                        <StatusBadge tone={getStatusTone(item.status)}>
                          {requestStatusLabels[item.status]}
                        </StatusBadge>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="pagination-bar">
              <div className="pagination-bar__info">
                <strong>
                  {response.totalItems === 1 ? "1 solicitação" : `${response.totalItems} solicitações`}
                </strong>
                <span>
                  Exibindo página {response.page} de {Math.max(response.totalPages, 1)}.
                </span>
              </div>

              <div className="pagination-bar__actions">
                <button
                  className="ui-button ui-button--secondary"
                  type="button"
                  disabled={response.page <= 1}
                  onClick={() =>
                    handleStateUpdate((current) => ({
                      ...current,
                      page: Math.max(1, current.page - 1)
                    }))
                  }
                >
                  Página anterior
                </button>
                <button
                  className="ui-button ui-button--primary"
                  type="button"
                  disabled={response.page >= response.totalPages}
                  onClick={() =>
                    handleStateUpdate((current) => ({
                      ...current,
                      page: current.page + 1
                    }))
                  }
                >
                  Próxima página
                </button>
              </div>
            </div>
          </>
        ) : null}
      </section>
    </div>
  );
}
