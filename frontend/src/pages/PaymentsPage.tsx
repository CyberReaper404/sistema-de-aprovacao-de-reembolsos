import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { paymentService } from "@/services/api";
import { paymentMethodLabels } from "@/types/domain";
import type { PaymentListItem } from "@/types/payments";

interface PaymentRow {
  id: string;
  requestNumber: string;
  label: string;
  method: string;
  amount: number;
  currency: string;
  paidAt: string;
  reference: string;
}

const demoPayments: PaymentRow[] = [
  {
    id: "pay-demo-1",
    requestNumber: "RB-2026-0147",
    label: "Mariana Farias · Táxi corporativo",
    method: "Pix",
    amount: 1200,
    currency: "BRL",
    paidAt: "2026-03-03",
    reference: "PIX-1457001"
  },
  {
    id: "pay-demo-2",
    requestNumber: "RB-2026-0150",
    label: "Anderson Neves · Hospedagem",
    method: "Transferência bancária",
    amount: 4200,
    currency: "BRL",
    paidAt: "2026-03-07",
    reference: "TED-1457002"
  },
  {
    id: "pay-demo-3",
    requestNumber: "RB-2026-0193",
    label: "Kaio Barreto · Equipamento digital",
    method: "Pix",
    amount: 120,
    currency: "BRL",
    paidAt: "2026-03-28",
    reference: "PIX-1457003"
  }
];

function formatMoney(value: number, currency = "BRL") {
  return new Intl.NumberFormat("pt-BR", { style: "currency", currency }).format(value);
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "2-digit", year: "numeric" }).format(new Date(value));
}

function mapPaymentRows(items: PaymentListItem[]): PaymentRow[] {
  return items.map((item) => ({
    id: item.id,
    requestNumber: item.requestNumber,
    label: item.requestTitle,
    method: paymentMethodLabels[item.paymentMethod],
    amount: item.amountPaid,
    currency: item.currency,
    paidAt: item.paidAt,
    reference: item.paymentReference
  }));
}

export function PaymentsPage() {
  const [rows, setRows] = useState<PaymentRow[]>(demoPayments);
  const [search, setSearch] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isDemoMode, setIsDemoMode] = useState(true);

  useEffect(() => {
    let cancelled = false;

    async function loadPayments() {
      try {
        const response = await paymentService.getPaged({ page: 1, pageSize: 10, sort: "paidAt:desc" });

        if (cancelled) {
          return;
        }

        if (response.items.length > 0) {
          setRows(mapPaymentRows(response.items));
          setIsDemoMode(false);
        } else {
          setRows(demoPayments);
          setIsDemoMode(true);
        }
      } catch {
        if (!cancelled) {
          setRows(demoPayments);
          setIsDemoMode(true);
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadPayments();

    return () => {
      cancelled = true;
    };
  }, []);

  const filteredRows = useMemo(() => {
    const normalized = search.trim().toLowerCase();
    if (!normalized) {
      return rows;
    }

    return rows.filter((row) => row.requestNumber.toLowerCase().includes(normalized) || row.label.toLowerCase().includes(normalized));
  }, [rows, search]);

  return (
    <section className="operations-page">
      <header className="operations-page__header">
        <div>
          <h1>Gerenciar pagamentos</h1>
          <p>Consulte a fila financeira e acompanhe os pagamentos já liquidados pelo setor responsável.</p>
        </div>

        <div className="operations-page__header-actions">
          <button className="ops-button ops-button--secondary" type="button">
            Exportar pagamentos
          </button>
          <Link className="ops-button ops-button--primary" to="/solicitacoes">
            Abrir solicitações
          </Link>
        </div>
      </header>

      <div className="operations-toolbar">
        <label className="ops-search-field">
          <span className="sr-only">Buscar pagamento</span>
          <input type="search" placeholder="Buscar por protocolo ou colaborador" value={search} onChange={(event) => setSearch(event.target.value)} />
        </label>
      </div>

      <section className="operations-table">
        {isLoading ? (
          <div className="operations-state">
            <strong>Carregando pagamentos</strong>
            <span>Consultando o histórico financeiro mais recente.</span>
          </div>
        ) : (
          <>
            <table>
              <thead>
                <tr>
                  <th>Protocolo</th>
                  <th>Solicitação</th>
                  <th>Pagamento</th>
                  <th className="operations-table__right">Valor</th>
                  <th>Data</th>
                  <th>Referência</th>
                </tr>
              </thead>
              <tbody>
                {filteredRows.map((row) => (
                  <tr key={row.id}>
                    <td>{row.requestNumber}</td>
                    <td>{row.label}</td>
                    <td>{row.method}</td>
                    <td className="operations-table__right">{formatMoney(row.amount, row.currency)}</td>
                    <td>{formatDate(row.paidAt)}</td>
                    <td>{row.reference}</td>
                  </tr>
                ))}
              </tbody>
            </table>

            <footer className="operations-table__footer">
              <div className="operations-table__footer-copy">
                <strong>{isDemoMode ? "Exibindo base demonstrativa" : `${filteredRows.length} pagamentos carregados`}</strong>
                <span>{isDemoMode ? "Os registros abaixo são exemplos realistas enquanto a base real ainda amadurece." : "Fila financeira carregada com dados reais da API."}</span>
              </div>
            </footer>
          </>
        )}
      </section>
    </section>
  );
}
