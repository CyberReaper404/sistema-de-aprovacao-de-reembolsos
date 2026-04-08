import { MemoryRouter } from "react-router-dom";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ReimbursementDetailView } from "@/features/reimbursements/reimbursement-detail-view";
import { createReimbursementDetail, paymentMethodExample } from "@/tests/reimbursement-test-data";
import { PaymentMethod, RequestStatus } from "@/types/domain";

const { mockedUseParams, reimbursementServiceMock } = vi.hoisted(() => ({
  mockedUseParams: vi.fn(),
  reimbursementServiceMock: {
    getById: vi.fn(),
    submit: vi.fn(),
    approve: vi.fn(),
    reject: vi.fn(),
    recordPayment: vi.fn(),
    downloadAttachment: vi.fn()
  }
}));

vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual<typeof import("react-router-dom")>("react-router-dom");

  return {
    ...actual,
    useParams: () => mockedUseParams()
  };
});

vi.mock("@/services/api", () => ({
  reimbursementService: reimbursementServiceMock
}));

describe("ReimbursementDetailView", () => {
  beforeEach(() => {
    mockedUseParams.mockReset();
    reimbursementServiceMock.getById.mockReset();
    reimbursementServiceMock.submit.mockReset();
    reimbursementServiceMock.approve.mockReset();
    reimbursementServiceMock.reject.mockReset();
    reimbursementServiceMock.recordPayment.mockReset();
    reimbursementServiceMock.downloadAttachment.mockReset();
  });

  it("deve aprovar a solicitacao com comentario opcional", async () => {
    const user = userEvent.setup();
    const submittedDetail = createReimbursementDetail({
      status: RequestStatus.Submitted,
      allowedActions: {
        canEditDraft: false,
        canSubmit: false,
        canApprove: true,
        canReject: true,
        canRecordPayment: false,
        canUploadAttachment: false,
        canDeleteAttachment: false
      }
    });
    const approvedDetail = createReimbursementDetail({
      status: RequestStatus.Approved,
      approvedAt: "2026-04-06T13:00:00Z",
      allowedActions: {
        ...submittedDetail.allowedActions,
        canApprove: false,
        canReject: false
      }
    });

    mockedUseParams.mockReturnValue({ id: submittedDetail.id });
    reimbursementServiceMock.getById
      .mockResolvedValueOnce(submittedDetail)
      .mockResolvedValueOnce(approvedDetail);
    reimbursementServiceMock.approve.mockResolvedValue(undefined);

    render(
      <MemoryRouter>
        <ReimbursementDetailView />
      </MemoryRouter>
    );

    await screen.findByText(submittedDetail.requestNumber);
    await user.type(screen.getAllByRole("textbox")[0], "Aprovado apos revisao.");
    await user.click(screen.getByRole("button", { name: "Aprovar" }));

    await waitFor(() => {
      expect(reimbursementServiceMock.approve).toHaveBeenCalledWith(submittedDetail.id, {
        comment: "Aprovado apos revisao."
      });
    });

    expect(await screen.findByText(/aprovada com sucesso/i)).toBeInTheDocument();
  });

  it("deve recusar a solicitacao com justificativa obrigatoria", async () => {
    const user = userEvent.setup();
    const submittedDetail = createReimbursementDetail({
      status: RequestStatus.Submitted,
      allowedActions: {
        canEditDraft: false,
        canSubmit: false,
        canApprove: false,
        canReject: true,
        canRecordPayment: false,
        canUploadAttachment: false,
        canDeleteAttachment: false
      }
    });
    const rejectedDetail = createReimbursementDetail({
      status: RequestStatus.Rejected,
      rejectionReason: "Falta comprovante fiscal valido.",
      allowedActions: {
        ...submittedDetail.allowedActions,
        canReject: false
      }
    });

    mockedUseParams.mockReturnValue({ id: submittedDetail.id });
    reimbursementServiceMock.getById
      .mockResolvedValueOnce(submittedDetail)
      .mockResolvedValueOnce(rejectedDetail);
    reimbursementServiceMock.reject.mockResolvedValue(undefined);

    render(
      <MemoryRouter>
        <ReimbursementDetailView />
      </MemoryRouter>
    );

    await screen.findByText(submittedDetail.requestNumber);
    await user.type(screen.getAllByRole("textbox")[0], "Falta comprovante fiscal valido.");
    await user.click(screen.getByRole("button", { name: "Recusar" }));

    await waitFor(() => {
      expect(reimbursementServiceMock.reject).toHaveBeenCalledWith(submittedDetail.id, {
        reason: "Falta comprovante fiscal valido."
      });
    });

    expect(await screen.findByText(/recusada com sucesso/i)).toBeInTheDocument();
  });

  it("deve registrar pagamento com os dados informados", async () => {
    const user = userEvent.setup();
    const approvedDetail = createReimbursementDetail({
      status: RequestStatus.Approved,
      allowedActions: {
        canEditDraft: false,
        canSubmit: false,
        canApprove: false,
        canReject: false,
        canRecordPayment: true,
        canUploadAttachment: false,
        canDeleteAttachment: false
      }
    });
    const paidDetail = createReimbursementDetail({
      status: RequestStatus.Paid,
      paidAt: "2026-04-07T10:15:00Z",
      allowedActions: {
        ...approvedDetail.allowedActions,
        canRecordPayment: false
      }
    });

    mockedUseParams.mockReturnValue({ id: approvedDetail.id });
    reimbursementServiceMock.getById
      .mockResolvedValueOnce(approvedDetail)
      .mockResolvedValueOnce(paidDetail);
    reimbursementServiceMock.recordPayment.mockResolvedValue(undefined);

    render(
      <MemoryRouter>
        <ReimbursementDetailView />
      </MemoryRouter>
    );

    await screen.findByText(approvedDetail.requestNumber);
    const paymentSelect = screen.getByRole("combobox");
    const notesInput = document.querySelector("textarea") as HTMLTextAreaElement;

    await user.selectOptions(paymentSelect, String(paymentMethodExample));
    await user.type(notesInput, "Pagamento confirmado pelo financeiro.");
    await user.click(screen.getByRole("button", { name: "Registrar pagamento" }));

    await waitFor(() => {
      expect(reimbursementServiceMock.recordPayment).toHaveBeenCalledWith(
        approvedDetail.id,
        expect.objectContaining({
          paymentMethod: PaymentMethod.BankTransfer,
          paymentReference: approvedDetail.requestNumber,
          amountPaid: approvedDetail.amount,
          notes: "Pagamento confirmado pelo financeiro."
        })
      );
    });

    expect(await screen.findByText(/pagamento registrado com sucesso/i)).toBeInTheDocument();
  });
});
