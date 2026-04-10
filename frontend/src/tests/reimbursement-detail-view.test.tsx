import { MemoryRouter } from "react-router-dom";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ReimbursementDetailView } from "@/features/reimbursements/reimbursement-detail-view";
import { createReimbursementDetail, paymentMethodExample } from "@/tests/reimbursement-test-data";
import { DecisionReasonCode, PaymentMethod, RequestStatus } from "@/types/domain";

const { mockedUseParams, reimbursementServiceMock } = vi.hoisted(() => ({
  mockedUseParams: vi.fn(),
  reimbursementServiceMock: {
    getById: vi.fn(),
    submit: vi.fn(),
    approve: vi.fn(),
    reject: vi.fn(),
    requestComplementation: vi.fn(),
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
    reimbursementServiceMock.requestComplementation.mockReset();
    reimbursementServiceMock.recordPayment.mockReset();
    reimbursementServiceMock.downloadAttachment.mockReset();
  });

  it("deve aprovar a solicitacao com justificativa obrigatoria", async () => {
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
        canDeleteAttachment: false,
        canRequestComplementation: true
      }
    });
    const approvedDetail = createReimbursementDetail({
      status: RequestStatus.Approved,
      decisionComment: "Solicitação aprovada após conferência do comprovante e do centro de custo.",
      approvedAt: "2026-04-06T13:00:00Z",
      allowedActions: {
        ...submittedDetail.allowedActions,
        canApprove: false,
        canReject: false,
        canRequestComplementation: false
      }
    });

    mockedUseParams.mockReturnValue({ id: submittedDetail.id });
    reimbursementServiceMock.getById.mockResolvedValueOnce(submittedDetail).mockResolvedValueOnce(approvedDetail);
    reimbursementServiceMock.approve.mockResolvedValue(undefined);

    render(
      <MemoryRouter>
        <ReimbursementDetailView />
      </MemoryRouter>
    );

    await screen.findByText(submittedDetail.requestNumber);
    await user.type(screen.getByLabelText("Justificativa da aprovação"), "Solicitação aprovada após conferência do comprovante e do centro de custo.");
    await user.click(screen.getByRole("button", { name: "Aprovar" }));

    await waitFor(() => {
      expect(reimbursementServiceMock.approve).toHaveBeenCalledWith(submittedDetail.id, {
        comment: "Solicitação aprovada após conferência do comprovante e do centro de custo."
      });
    });

    expect(await screen.findByText(/aprovada com sucesso/i)).toBeInTheDocument();
    expect(await screen.findByText("Despesa validada")).toBeInTheDocument();
  });

  it("deve recusar a solicitacao com motivo padronizado", async () => {
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
        canDeleteAttachment: false,
        canRequestComplementation: false
      }
    });
    const rejectedDetail = createReimbursementDetail({
      status: RequestStatus.Rejected,
      decisionReasonCode: DecisionReasonCode.MissingReceipt,
      decisionComment: "Falta comprovante fiscal legível.",
      rejectionReason: "Falta comprovante fiscal legível.",
      allowedActions: {
        ...submittedDetail.allowedActions,
        canReject: false
      }
    });

    mockedUseParams.mockReturnValue({ id: submittedDetail.id });
    reimbursementServiceMock.getById.mockResolvedValueOnce(submittedDetail).mockResolvedValueOnce(rejectedDetail);
    reimbursementServiceMock.reject.mockResolvedValue(undefined);

    render(
      <MemoryRouter>
        <ReimbursementDetailView />
      </MemoryRouter>
    );

    await screen.findByText(submittedDetail.requestNumber);
    await user.selectOptions(screen.getByLabelText("Motivo padronizado"), String(DecisionReasonCode.MissingReceipt));
    await user.type(screen.getByLabelText("Observação da recusa"), "Falta comprovante fiscal legível.");
    await user.click(screen.getByRole("button", { name: "Recusar" }));

    await waitFor(() => {
      expect(reimbursementServiceMock.reject).toHaveBeenCalledWith(submittedDetail.id, {
        reasonCode: DecisionReasonCode.MissingReceipt,
        comment: "Falta comprovante fiscal legível."
      });
    });

    expect(await screen.findByText(/comprovante ausente/i)).toBeInTheDocument();
  });

  it("deve solicitar complementacao com motivo padronizado", async () => {
    const user = userEvent.setup();
    const submittedDetail = createReimbursementDetail({
      status: RequestStatus.Submitted,
      allowedActions: {
        canEditDraft: false,
        canSubmit: false,
        canApprove: false,
        canReject: false,
        canRecordPayment: false,
        canUploadAttachment: false,
        canDeleteAttachment: false,
        canRequestComplementation: true
      }
    });
    const complementedDetail = createReimbursementDetail({
      status: RequestStatus.Submitted,
      hasPendingComplementation: true,
      complementationRequestedAt: "2026-04-06T13:00:00Z",
      decisionReasonCode: DecisionReasonCode.NeedAdditionalDocument,
      decisionComment: "Anexe um comprovante legível com emissor, data e valor.",
      allowedActions: {
        ...submittedDetail.allowedActions,
        canRequestComplementation: false
      }
    });

    mockedUseParams.mockReturnValue({ id: submittedDetail.id });
    reimbursementServiceMock.getById.mockResolvedValueOnce(submittedDetail).mockResolvedValueOnce(complementedDetail);
    reimbursementServiceMock.requestComplementation.mockResolvedValue(undefined);

    render(
      <MemoryRouter>
        <ReimbursementDetailView />
      </MemoryRouter>
    );

    await screen.findByText(submittedDetail.requestNumber);
    const selects = screen.getAllByLabelText("Motivo padronizado");
    await user.selectOptions(selects[0], String(DecisionReasonCode.NeedAdditionalDocument));
    await user.type(screen.getByLabelText("Orientação para o colaborador"), "Anexe um comprovante legível com emissor, data e valor.");
    await user.click(screen.getByRole("button", { name: "Solicitar complementação" }));

    await waitFor(() => {
      expect(reimbursementServiceMock.requestComplementation).toHaveBeenCalledWith(submittedDetail.id, {
        reasonCode: DecisionReasonCode.NeedAdditionalDocument,
        comment: "Anexe um comprovante legível com emissor, data e valor."
      });
    });

    expect(await screen.findByText(/complementação solicitada com sucesso/i)).toBeInTheDocument();
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
        canDeleteAttachment: false,
        canRequestComplementation: false
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
    reimbursementServiceMock.getById.mockResolvedValueOnce(approvedDetail).mockResolvedValueOnce(paidDetail);
    reimbursementServiceMock.recordPayment.mockResolvedValue(undefined);

    render(
      <MemoryRouter>
        <ReimbursementDetailView />
      </MemoryRouter>
    );

    await screen.findByText(approvedDetail.requestNumber);
    await user.selectOptions(screen.getByLabelText("Método de pagamento"), String(paymentMethodExample));
    await user.type(screen.getByLabelText("Observações"), "Pagamento confirmado pelo financeiro.");
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
