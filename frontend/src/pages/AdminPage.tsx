import { useEffect, useMemo, useState } from "react";
import { adminService } from "@/services/api";
import type { AdminAuditEntry, AdminCategory, AdminCostCenter, AdminUser } from "@/types/admin";
import { auditSeverityLabels, userRoleLabels } from "@/types/domain";

type AdminSection = "users" | "categories" | "costCenters" | "audit";

const demoUsers: AdminUser[] = [
  {
    id: "user-demo-1",
    fullName: "Mariana Farias",
    email: "mariana.farias@empresa.local",
    role: 1,
    primaryCostCenterId: "cc-01",
    isActive: true,
    managedCostCenterIds: []
  },
  {
    id: "user-demo-2",
    fullName: "Bruno Azevedo",
    email: "bruno.azevedo@empresa.local",
    role: 2,
    primaryCostCenterId: "cc-02",
    isActive: true,
    managedCostCenterIds: ["cc-02"]
  }
];

const demoCategories: AdminCategory[] = [
  {
    id: "cat-demo-1",
    name: "Táxi corporativo",
    description: "Deslocamentos urbanos a trabalho",
    isActive: true,
    maxAmount: 800,
    receiptRequiredAboveAmount: 80,
    receiptRequiredAlways: false,
    submissionDeadlineDays: 30
  },
  {
    id: "cat-demo-2",
    name: "Hospedagem",
    description: "Hospedagem em viagem corporativa",
    isActive: true,
    maxAmount: 4500,
    receiptRequiredAboveAmount: 0,
    receiptRequiredAlways: true,
    submissionDeadlineDays: 15
  }
];

const demoCostCenters: AdminCostCenter[] = [
  { id: "cc-demo-1", code: "FIN-001", name: "Financeiro", isActive: true },
  { id: "cc-demo-2", code: "OPS-002", name: "Operações", isActive: true }
];

const demoAudit: AdminAuditEntry[] = [
  {
    id: "audit-demo-1",
    eventType: "reimbursement.approved",
    entityType: "reimbursement_request",
    entityId: "RB-2026-0147",
    actorUserId: "user-demo-2",
    severity: 1,
    occurredAt: "2026-03-03T14:20:00Z",
    metadataJson: null
  },
  {
    id: "audit-demo-2",
    eventType: "payment.registered",
    entityType: "payment_record",
    entityId: "RB-2026-0150",
    actorUserId: "user-demo-2",
    severity: 1,
    occurredAt: "2026-03-07T10:05:00Z",
    metadataJson: null
  }
];

function formatMoney(value?: number | null) {
  if (value == null) {
    return "Sem limite";
  }

  return new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(value);
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit"
  }).format(new Date(value));
}

export function AdminPage() {
  const [section, setSection] = useState<AdminSection>("users");
  const [users, setUsers] = useState<AdminUser[]>(demoUsers);
  const [categories, setCategories] = useState<AdminCategory[]>(demoCategories);
  const [costCenters, setCostCenters] = useState<AdminCostCenter[]>(demoCostCenters);
  const [auditEntries, setAuditEntries] = useState<AdminAuditEntry[]>(demoAudit);
  const [isDemoMode, setIsDemoMode] = useState(true);

  useEffect(() => {
    let cancelled = false;

    async function loadAdmin() {
      try {
        const [usersResponse, categoriesResponse, costCentersResponse, auditResponse] = await Promise.all([
          adminService.getUsers(),
          adminService.getCategories(),
          adminService.getCostCenters(),
          adminService.getAuditEntries()
        ]);

        if (cancelled) {
          return;
        }

        setUsers(usersResponse.items);
        setCategories(categoriesResponse);
        setCostCenters(costCentersResponse);
        setAuditEntries(auditResponse.items);
        setIsDemoMode(false);
      } catch {
        if (!cancelled) {
          setUsers(demoUsers);
          setCategories(demoCategories);
          setCostCenters(demoCostCenters);
          setAuditEntries(demoAudit);
          setIsDemoMode(true);
        }
      }
    }

    void loadAdmin();

    return () => {
      cancelled = true;
    };
  }, []);

  const content = useMemo(() => {
    if (section === "users") {
      return (
        <table>
          <thead>
            <tr>
              <th>Nome</th>
              <th>E-mail</th>
              <th>Papel</th>
              <th>Situação</th>
            </tr>
          </thead>
          <tbody>
            {users.map((item) => (
              <tr key={item.id}>
                <td>{item.fullName}</td>
                <td>{item.email}</td>
                <td>{userRoleLabels[item.role]}</td>
                <td>{item.isActive ? "Ativo" : "Inativo"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      );
    }

    if (section === "categories") {
      return (
        <table>
          <thead>
            <tr>
              <th>Categoria</th>
              <th>Limite</th>
              <th>Comprovante</th>
              <th>Prazo</th>
            </tr>
          </thead>
          <tbody>
            {categories.map((item) => (
              <tr key={item.id}>
                <td>{item.name}</td>
                <td>{formatMoney(item.maxAmount)}</td>
                <td>{item.receiptRequiredAlways ? "Sempre" : item.receiptRequiredAboveAmount ? `Acima de ${formatMoney(item.receiptRequiredAboveAmount)}` : "Opcional"}</td>
                <td>{item.submissionDeadlineDays ? `${item.submissionDeadlineDays} dias` : "Padrão"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      );
    }

    if (section === "costCenters") {
      return (
        <table>
          <thead>
            <tr>
              <th>Código</th>
              <th>Centro de custo</th>
              <th>Situação</th>
            </tr>
          </thead>
          <tbody>
            {costCenters.map((item) => (
              <tr key={item.id}>
                <td>{item.code}</td>
                <td>{item.name}</td>
                <td>{item.isActive ? "Ativo" : "Inativo"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      );
    }

    return (
      <table>
        <thead>
          <tr>
            <th>Evento</th>
            <th>Entidade</th>
            <th>Severidade</th>
            <th>Data</th>
          </tr>
        </thead>
        <tbody>
          {auditEntries.map((item) => (
            <tr key={item.id}>
              <td>{item.eventType}</td>
              <td>{item.entityId ?? item.entityType}</td>
              <td>{auditSeverityLabels[item.severity]}</td>
              <td>{formatDateTime(item.occurredAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    );
  }, [auditEntries, categories, costCenters, section, users]);

  return (
    <section className="operations-page">
      <header className="operations-page__header">
        <div>
          <h1>Gerenciar administração</h1>
          <p>Centralize estruturas, perfis e trilhas de auditoria na mesma malha operacional do sistema.</p>
        </div>

        <div className="operations-page__header-actions">
          <button className="ops-button ops-button--secondary" type="button">
            Exportar visão
          </button>
        </div>
      </header>

      <div className="operations-tabs" role="tablist" aria-label="Seções administrativas">
        <button type="button" className={section === "users" ? "operations-tabs__item operations-tabs__item--active" : "operations-tabs__item"} onClick={() => setSection("users")}>
          Usuários <strong>{users.length}</strong>
        </button>
        <button type="button" className={section === "categories" ? "operations-tabs__item operations-tabs__item--active" : "operations-tabs__item"} onClick={() => setSection("categories")}>
          Categorias <strong>{categories.length}</strong>
        </button>
        <button type="button" className={section === "costCenters" ? "operations-tabs__item operations-tabs__item--active" : "operations-tabs__item"} onClick={() => setSection("costCenters")}>
          Centros <strong>{costCenters.length}</strong>
        </button>
        <button type="button" className={section === "audit" ? "operations-tabs__item operations-tabs__item--active" : "operations-tabs__item"} onClick={() => setSection("audit")}>
          Auditoria <strong>{auditEntries.length}</strong>
        </button>
      </div>

      <section className="operations-table">
        {content}
        <footer className="operations-table__footer">
          <div className="operations-table__footer-copy">
            <strong>{isDemoMode ? "Exibindo base demonstrativa" : "Dados administrativos reais carregados"}</strong>
            <span>{isDemoMode ? "Os dados administrativos abaixo são exemplos seguros para visualização imediata." : "A página já conversa com os endpoints administrativos do backend."}</span>
          </div>
        </footer>
      </section>
    </section>
  );
}
