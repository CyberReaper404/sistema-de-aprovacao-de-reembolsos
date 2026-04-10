import { MemoryRouter } from "react-router-dom";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { DashboardView } from "@/features/dashboard/dashboard-view";
import { createSession } from "@/tests/reimbursement-test-data";
import { RequestStatus, UserRole } from "@/types/domain";

const { dashboardServiceMock, reimbursementServiceMock, useSessionMock } = vi.hoisted(() => ({
  dashboardServiceMock: {
    getSummary: vi.fn(),
    getByStatus: vi.fn()
  },
  reimbursementServiceMock: {
    getPaged: vi.fn()
  },
  useSessionMock: vi.fn()
}));

vi.mock("@/services/api", () => ({
  dashboardService: dashboardServiceMock,
  reimbursementService: reimbursementServiceMock
}));

vi.mock("@/features/auth/session-context", () => ({
  useSession: () => useSessionMock()
}));

describe("DashboardView", () => {
  beforeEach(() => {
    dashboardServiceMock.getSummary.mockReset();
    dashboardServiceMock.getByStatus.mockReset();
    reimbursementServiceMock.getPaged.mockReset();
    useSessionMock.mockReset();

    useSessionMock.mockReturnValue({
      session: createSession({
        user: {
          id: "usuario-1",
          fullName: "Maria Oliveira",
          email: "maria@empresa.com",
          role: UserRole.Collaborator,
          primaryCostCenterId: "cc-01"
        }
      })
    });

    dashboardServiceMock.getSummary.mockResolvedValue({
      totalRequests: 8,
      pendingRequests: 3,
      approvedRequests: 2,
      paidRequests: 1,
      totalApprovedAmount: 1250,
      totalPaidAmount: 720
    });

    dashboardServiceMock.getByStatus.mockResolvedValue([
      { status: RequestStatus.Submitted, totalRequests: 3, totalAmount: 800 },
      { status: RequestStatus.Approved, totalRequests: 2, totalAmount: 1250 },
      { status: RequestStatus.Rejected, totalRequests: 2, totalAmount: 320 },
      { status: RequestStatus.Paid, totalRequests: 1, totalAmount: 720 }
    ]);

    reimbursementServiceMock.getPaged.mockResolvedValue({
      items: [
        {
          id: "solicitacao-1",
          requestNumber: "RB-2026-0001",
          title: "Táxi para reunião externa",
          categoryName: "Transporte",
          amount: 125.4,
          currency: "BRL",
          status: RequestStatus.Submitted,
          expenseDate: "2026-04-05",
          costCenterCode: "COM-001",
          createdAt: "2026-04-05T14:20:00Z"
        }
      ],
      page: 1,
      pageSize: 10,
      totalItems: 1,
      totalPages: 1,
      totalCount: 1
    });
  });

  it("deve carregar o painel operacional com tabela principal e CTA de colaborador", async () => {
    render(
      <MemoryRouter>
        <DashboardView />
      </MemoryRouter>
    );

    expect(await screen.findByRole("heading", { name: "Gerenciar solicitações" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /\+ nova solicitação/i })).toBeInTheDocument();
    expect(await screen.findByText("RB-2026-0001")).toBeInTheDocument();

    expect(dashboardServiceMock.getSummary).toHaveBeenCalled();
    expect(dashboardServiceMock.getByStatus).toHaveBeenCalled();
    expect(reimbursementServiceMock.getPaged).toHaveBeenCalledWith(
      expect.objectContaining({ page: 1, pageSize: 10, sort: "createdAt:desc" })
    );
  });

  it("deve refinar a tabela ao trocar a aba de status", async () => {
    const user = userEvent.setup();

    render(
      <MemoryRouter>
        <DashboardView />
      </MemoryRouter>
    );

    await screen.findByText("RB-2026-0001");
    reimbursementServiceMock.getPaged.mockClear();

    await user.click(screen.getByRole("button", { name: /aprovadas/i }));

    await waitFor(() => {
      expect(reimbursementServiceMock.getPaged).toHaveBeenCalledWith(
        expect.objectContaining({ status: RequestStatus.Approved })
      );
    });
  });
});
