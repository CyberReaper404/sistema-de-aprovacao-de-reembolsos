import { FormEvent, useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { StatusBadge } from "@/components/display/StatusBadge";
import { PlaceholderState } from "@/components/feedback/PlaceholderState";
import { PageHeader } from "@/components/layout/PageHeader";
import { reimbursementService } from "@/services/api";
import { ApiError } from "@/services/http/api-error";
import {
  PaymentMethod,
  RequestStatus,
  paymentMethodLabels,
  requestStatusLabels,
  workflowActionTypeLabels
} from "@/types/domain";
import type { Attachment, ReimbursementDetail } from "@/types/reimbursements";

type StatusTone = "green" | "amber" | "red" | "slate";

const currencyFormatters = new Map<string, Intl.NumberFormat>();
const expenseDateFormatter = new Intl.DateTimeFormat("pt-BR", { dateStyle: "long" });
const dateTimeFormatter = new Intl.DateTimeFormat("pt-BR", {
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

function formatDateTime(value?: string | null) {
  if (!value) {
    return "Não registrado";
  }

  return dateTimeFormatter.format(new Date(value));
}

function saveDownloadedFile(content: Blob, fileName: string) {
  const url = URL.createObjectURL(content);
  const anchor = document.createElement("a");

  anchor.href = url;
  anchor.download = fileName;
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  URL.revokeObjectURL(url);
}

function getDefaultPaidAtValue() {
  const now = new Date();
  const offset = now.getTimezoneOffset();
  const localDate = new Date(now.getTime() - offset * 60_000);
  return localDate.toISOString().slice(0, 16);
}

export function ReimbursementDetailView() {
  const { id } = useParams<{ id: string }>();
  const [detail, setDetail] = useState<ReimbursementDetail | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [actionSuccessMessage, setActionSuccessMessage] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmittingAction, setIsSubmittingAction] = useState(false);
  const [downloadingAttachmentId, setDownloadingAttachmentId] = useState<string | null>(null);
  const [approveComment, setApproveComment] = useState("");
  const [rejectReason, setRejectReason] = useState("");
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>(PaymentMethod.Pix);
  const [paymentReference, setPaymentReference] = useState("");
  const [paymentNotes, setPaymentNotes] = useState("");
  const [paidAt, setPaidAt] = useState(getDefaultPaidAtValue());

  const loadDetail = async (requestId: string) => {
    const nextDetail = await reimbursementService.getById(requestId);
    setDetail(nextDetail);
    return nextDetail;
  };

  useEffect(() => {
    let isCancelled = false;

    const execute = async () => {
      if (!id) {
        setErrorMessage("A solicitação informada é inválida.");
        setIsLoading(false);
        return;
      }

      setIsLoading(true);
      setErrorMessage(null);
      setActionSuccessMessage(null);

      try {
        const nextDetail = await reimbursementService.getById(id);

        if (!isCancelled) {
          setDetail(nextDetail);
        }
      } catch (error) {
        if (!isCancelled) {
          const message =
            error instanceof ApiError
              ? error.message
              : "Não foi possível carregar o detalhe da solicitação.";

          setErrorMessage(message);
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    };

    void execute();

    return () => {
      isCancelled = true;
    };
  }, [id]);

  useEffect(() => {
    if (!detail) {
      return;
    }

    setPaymentReference(detail.requestNumber);
    setPaidAt(getDefaultPaidAtValue());
  }, [detail?.id, detail?.requestNumber]);

  const availableActions = useMemo(() => {
    if (!detail) {
      return [];
    }

    const actions: string[] = [];

    if (detail.allowedActions.canEditDraft) {
      actions.push("Editar rascunho");
    }

    if (detail.allowedActions.canSubmit) {
      actions.push("Enviar solicitação");
    }

    if (detail.allowedActions.canApprove) {
      actions.push("Aprovar");
    }

    if (detail.allowedActions.canReject) {
      actions.push("Recusar");
    }

    if (detail.allowedActions.canRecordPayment) {
      actions.push("Registrar pagamento");
    }

    if (detail.allowedActions.canUploadAttachment) {
      actions.push("Adicionar anexo");
    }

    if (detail.allowedActions.canDeleteAttachment) {
      actions.push("Remover anexo");
    }

    return actions;
  }, [detail]);

  const handleAttachmentDownload = async (attachment: Attachment) => {
    if (!detail) {
      return;
    }

    try {
      setDownloadingAttachmentId(attachment.id);
      setErrorMessage(null);
      const file = await reimbursementService.downloadAttachment(detail.id, attachment.id);
      saveDownloadedFile(file.content, file.fileName ?? attachment.originalFileName);
    } catch (error) {
      const message =
        error instanceof ApiError
          ? error.message
          : "Não foi possível baixar o anexo selecionado.";

      setErrorMessage(message);
    } finally {
      setDownloadingAttachmentId(null);
    }
  };

  const reloadDetail = async () => {
    if (!id) {
      return;
    }

    await loadDetail(id);
  };

  const handleSubmitDraft = async () => {
    if (!detail) {
      return;
    }

    try {
      setIsSubmittingAction(true);
      setActionSuccessMessage(null);
      setErrorMessage(null);
      await reimbursementService.submit(detail.id);
      await reloadDetail();
      setActionSuccessMessage("Solicitação enviada com sucesso.");
    } catch (error) {
      const message =
        error instanceof ApiError
          ? error.message
          : "Não foi possível enviar a solicitação.";

      setErrorMessage(message);
    } finally {
      setIsSubmittingAction(false);
    }
  };

  const handleApprove = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!detail) {
      return;
    }

    try {
      setIsSubmittingAction(true);
      setActionSuccessMessage(null);
      setErrorMessage(null);
      await reimbursementService.approve(detail.id, { comment: approveComment.trim() || undefined });
      await reloadDetail();
      setApproveComment("");
      setActionSuccessMessage("Solicitação aprovada com sucesso.");
    } catch (error) {
      const message =
        error instanceof ApiError
          ? error.message
          : "Não foi possível aprovar a solicitação.";

      setErrorMessage(message);
    } finally {
      setIsSubmittingAction(false);
    }
  };

  const handleReject = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!detail) {
      return;
    }

    try {
      setIsSubmittingAction(true);
      setActionSuccessMessage(null);
      setErrorMessage(null);
      await reimbursementService.reject(detail.id, { reason: rejectReason.trim() });
      await reloadDetail();
      setRejectReason("");
      setActionSuccessMessage("Solicitação recusada com sucesso.");
    } catch (error) {
      const message =
        error instanceof ApiError
          ? error.message
          : "Não foi possível recusar a solicitação.";

      setErrorMessage(message);
    } finally {
      setIsSubmittingAction(false);
    }
  };

  const handleRecordPayment = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!detail) {
      return;
    }

    try {
      setIsSubmittingAction(true);
      setActionSuccessMessage(null);
      setErrorMessage(null);
      await reimbursementService.recordPayment(detail.id, {
        paymentMethod,
        paymentReference: paymentReference.trim(),
        paidAt: new Date(paidAt).toISOString(),
        amountPaid: detail.amount,
        notes: paymentNotes.trim() || undefined
      });
      await reloadDetail();
      setPaymentNotes("");
      setActionSuccessMessage("Pagamento registrado com sucesso.");
    } catch (error) {
      const message =
        error instanceof ApiError
          ? error.message
          : "Não foi possível registrar o pagamento.";

      setErrorMessage(message);
    } finally {
      setIsSubmittingAction(false);
    }
  };

  if (isLoading) {
    return (
      <div className="workspace-page">
        <PageHeader
          eyebrow="Solicitação"
          title="Carregando detalhe"
          description="Estamos buscando os dados completos da solicitação."
        />
        <div className="table-state">
          <strong>Carregando detalhe da solicitação...</strong>
          <span>O histórico, os anexos e o status serão exibidos em seguida.</span>
        </div>
      </div>
    );
  }

  if (!detail) {
    return (
      <div className="workspace-page">
        <PageHeader
          eyebrow="Solicitação"
          title="Detalhe indisponível"
          description="Não foi possível montar a visualização completa da solicitação."
        />
        <PlaceholderState
          eyebrow="Falha na consulta"
          title="Não foi possível carregar o detalhe"
          description={errorMessage ?? "Tente atualizar a página ou voltar para a listagem principal."}
        />
      </div>
    );
  }

  return (
    <div className="workspace-page">
      <PageHeader
        eyebrow="Solicitação"
        title={detail.requestNumber}
        description={detail.title}
        actions={
          <div className="detail-header__actions">
            <StatusBadge tone={getStatusTone(detail.status)}>
              {requestStatusLabels[detail.status]}
            </StatusBadge>
            {detail.allowedActions.canEditDraft ? (
              <Link className="ui-button ui-button--secondary" to={`/solicitacoes/${detail.id}/editar`}>
                Editar rascunho
              </Link>
            ) : null}
            <Link className="ui-button ui-button--secondary" to="/solicitacoes">
              Voltar para a listagem
            </Link>
          </div>
        }
      />

      {errorMessage ? <div className="inline-feedback inline-feedback--error">{errorMessage}</div> : null}
      {actionSuccessMessage ? <div className="inline-feedback">{actionSuccessMessage}</div> : null}

      <section className="workspace-section detail-grid">
        <div className="detail-card">
          <h2>Resumo da despesa</h2>
          <div className="detail-meta-grid">
            <div>
              <span>Categoria</span>
              <strong>{detail.categoryName}</strong>
            </div>
            <div>
              <span>Centro de custo</span>
              <strong>{detail.costCenterCode}</strong>
            </div>
            <div>
              <span>Valor</span>
              <strong>{formatCurrency(detail.amount, detail.currency)}</strong>
            </div>
            <div>
              <span>Data da despesa</span>
              <strong>{formatExpenseDate(detail.expenseDate)}</strong>
            </div>
            <div>
              <span>Criada por</span>
              <strong>{detail.createdByUserName}</strong>
            </div>
            <div>
              <span>Criada em</span>
              <strong>{formatDateTime(detail.createdAt)}</strong>
            </div>
          </div>

          <div className="detail-copy-block">
            <span>Descrição</span>
            <p>{detail.description}</p>
          </div>

          {detail.rejectionReason ? (
            <div className="detail-alert">
              <span>Motivo da recusa</span>
              <p>{detail.rejectionReason}</p>
            </div>
          ) : null}
        </div>

        <div className="detail-card detail-card--side">
          <h2>Rastreamento</h2>
          <div className="detail-meta-stack">
            <div>
              <span>Enviada em</span>
              <strong>{formatDateTime(detail.submittedAt)}</strong>
            </div>
            <div>
              <span>Aprovada em</span>
              <strong>{formatDateTime(detail.approvedAt)}</strong>
            </div>
            <div>
              <span>Recusada em</span>
              <strong>{formatDateTime(detail.rejectedAt)}</strong>
            </div>
            <div>
              <span>Paga em</span>
              <strong>{formatDateTime(detail.paidAt)}</strong>
            </div>
          </div>

          <div className="detail-copy-block">
            <span>Ações disponíveis</span>
            {availableActions.length > 0 ? (
              <div className="detail-action-list">
                {availableActions.map((action) => (
                  <span key={action} className="detail-chip">
                    {action}
                  </span>
                ))}
              </div>
            ) : (
              <p>Nenhuma ação operacional está disponível para o seu perfil neste momento.</p>
            )}
          </div>

          {detail.allowedActions.canSubmit ? (
            <div className="detail-action-panel">
              <h3>Enviar solicitação</h3>
              <p>Use esta ação quando o rascunho já estiver completo e com todos os comprovantes necessários.</p>
              <button
                className="ui-button ui-button--primary"
                type="button"
                onClick={() => void handleSubmitDraft()}
                disabled={isSubmittingAction}
              >
                {isSubmittingAction ? "Enviando..." : "Enviar solicitação"}
              </button>
            </div>
          ) : null}

          {detail.allowedActions.canApprove ? (
            <form className="detail-action-panel" onSubmit={handleApprove}>
              <h3>Aprovar solicitação</h3>
              <p>O comentário é opcional e fica registrado no histórico do workflow.</p>
              <label className="form-field form-field--compact">
                <span>Comentário</span>
                <textarea
                  rows={3}
                  value={approveComment}
                  onChange={(event) => setApproveComment(event.target.value)}
                  placeholder="Observação interna para registrar na aprovação."
                  disabled={isSubmittingAction}
                />
              </label>
              <button className="ui-button ui-button--primary" type="submit" disabled={isSubmittingAction}>
                {isSubmittingAction ? "Aprovando..." : "Aprovar"}
              </button>
            </form>
          ) : null}

          {detail.allowedActions.canReject ? (
            <form className="detail-action-panel" onSubmit={handleReject}>
              <h3>Recusar solicitação</h3>
              <p>A justificativa é obrigatória e precisa explicar o motivo da recusa.</p>
              <label className="form-field form-field--compact">
                <span>Motivo da recusa</span>
                <textarea
                  rows={4}
                  value={rejectReason}
                  onChange={(event) => setRejectReason(event.target.value)}
                  placeholder="Descreva claramente o motivo da recusa."
                  disabled={isSubmittingAction}
                  required
                />
              </label>
              <button className="ui-button ui-button--primary" type="submit" disabled={isSubmittingAction}>
                {isSubmittingAction ? "Recusando..." : "Recusar"}
              </button>
            </form>
          ) : null}

          {detail.allowedActions.canRecordPayment ? (
            <form className="detail-action-panel" onSubmit={handleRecordPayment}>
              <h3>Registrar pagamento</h3>
              <p>O valor registrado segue o total aprovado da solicitação.</p>
              <div className="form-grid form-grid--compact">
                <label className="form-field form-field--compact">
                  <span>Método de pagamento</span>
                  <select
                    value={paymentMethod}
                    onChange={(event) => setPaymentMethod(Number(event.target.value) as PaymentMethod)}
                    disabled={isSubmittingAction}
                  >
                    {Object.values(PaymentMethod)
                      .filter((value): value is PaymentMethod => typeof value === "number")
                      .map((value) => (
                        <option key={value} value={value}>
                          {paymentMethodLabels[value]}
                        </option>
                      ))}
                  </select>
                </label>

                <label className="form-field form-field--compact">
                  <span>Valor</span>
                  <input type="text" value={formatCurrency(detail.amount, detail.currency)} disabled />
                </label>

                <label className="form-field form-field--compact">
                  <span>Data do pagamento</span>
                  <input
                    type="datetime-local"
                    value={paidAt}
                    onChange={(event) => setPaidAt(event.target.value)}
                    disabled={isSubmittingAction}
                    required
                  />
                </label>

                <label className="form-field form-field--compact">
                  <span>Referência</span>
                  <input
                    type="text"
                    value={paymentReference}
                    onChange={(event) => setPaymentReference(event.target.value)}
                    placeholder="Ex.: Comprovante PIX"
                    disabled={isSubmittingAction}
                    required
                  />
                </label>

                <label className="form-field form-field--full form-field--compact">
                  <span>Observações</span>
                  <textarea
                    rows={3}
                    value={paymentNotes}
                    onChange={(event) => setPaymentNotes(event.target.value)}
                    placeholder="Informação complementar para o financeiro."
                    disabled={isSubmittingAction}
                  />
                </label>
              </div>

              <button className="ui-button ui-button--primary" type="submit" disabled={isSubmittingAction}>
                {isSubmittingAction ? "Registrando..." : "Registrar pagamento"}
              </button>
            </form>
          ) : null}
        </div>
      </section>

      <section className="workspace-section">
        <div className="section-title">
          <h2>Anexos</h2>
          <p>Arquivos vinculados à solicitação e liberados pelo backend autorizado.</p>
        </div>

        {detail.attachments.length === 0 ? (
          <div className="table-state">
            <strong>Nenhum anexo vinculado</strong>
            <span>Esta solicitação ainda não possui comprovantes cadastrados.</span>
          </div>
        ) : (
          <div className="attachment-list">
            {detail.attachments.map((attachment) => (
              <article key={attachment.id} className="attachment-item">
                <div>
                  <strong>{attachment.originalFileName}</strong>
                  <span>
                    {attachment.contentType} • {Math.max(1, Math.round(attachment.sizeInBytes / 1024))} KB
                  </span>
                </div>
                <button
                  className="ui-button ui-button--secondary"
                  type="button"
                  onClick={() => void handleAttachmentDownload(attachment)}
                  disabled={downloadingAttachmentId === attachment.id}
                >
                  {downloadingAttachmentId === attachment.id ? "Baixando..." : "Baixar"}
                </button>
              </article>
            ))}
          </div>
        )}
      </section>

      <section className="workspace-section">
        <div className="section-title">
          <h2>Histórico do workflow</h2>
          <p>Cada transição mostra o estado resultante e os comentários registrados no backend.</p>
        </div>

        {detail.workflowActions.length === 0 ? (
          <div className="table-state">
            <strong>Sem histórico registrado</strong>
            <span>O workflow ainda não gerou eventos auditáveis para esta solicitação.</span>
          </div>
        ) : (
          <div className="timeline-list">
            {detail.workflowActions.map((action) => (
              <article key={action.id} className="timeline-item">
                <div className="timeline-item__marker" aria-hidden="true" />
                <div className="timeline-item__content">
                  <div className="timeline-item__header">
                    <strong>{workflowActionTypeLabels[action.actionType]}</strong>
                    <span>{formatDateTime(action.occurredAt)}</span>
                  </div>
                  <p>
                    Status resultante: <strong>{requestStatusLabels[action.toStatus]}</strong>
                  </p>
                  {action.comment ? <p>{action.comment}</p> : null}
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
