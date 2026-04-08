import { ChangeEvent, FormEvent, useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { PlaceholderState } from "@/components/feedback/PlaceholderState";
import { PageHeader } from "@/components/layout/PageHeader";
import { useSession } from "@/features/auth/session-context";
import { reimbursementService } from "@/services/api";
import { ApiError } from "@/services/http/api-error";
import { RequestStatus, UserRole } from "@/types/domain";
import type {
  Attachment,
  CreateReimbursementPayload,
  ReimbursementCategoryOption,
  ReimbursementDetail,
  UpdateReimbursementDraftPayload
} from "@/types/reimbursements";

interface DraftFormState {
  title: string;
  categoryId: string;
  amount: string;
  currency: string;
  expenseDate: string;
  description: string;
}

const attachmentDateFormatter = new Intl.DateTimeFormat("pt-BR", {
  dateStyle: "short",
  timeStyle: "short"
});

const currencyFormatter = new Intl.NumberFormat("pt-BR", {
  style: "currency",
  currency: "BRL"
});

function createEmptyFormState(): DraftFormState {
  return {
    title: "",
    categoryId: "",
    amount: "",
    currency: "BRL",
    expenseDate: "",
    description: ""
  };
}

function toDraftFormState(detail: ReimbursementDetail): DraftFormState {
  return {
    title: detail.title,
    categoryId: detail.categoryId,
    amount: detail.amount.toString(),
    currency: detail.currency,
    expenseDate: detail.expenseDate,
    description: detail.description
  };
}

function formatAttachmentDate(value: string) {
  return attachmentDateFormatter.format(new Date(value));
}

function getFieldError(errors: Record<string, string[]> | null, field: string) {
  return errors?.[field]?.[0];
}

export function ReimbursementDraftPage() {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const { session } = useSession();
  const [categories, setCategories] = useState<ReimbursementCategoryOption[]>([]);
  const [detail, setDetail] = useState<ReimbursementDetail | null>(null);
  const [formState, setFormState] = useState<DraftFormState>(() => createEmptyFormState());
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]> | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isSubmittingDraft, setIsSubmittingDraft] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [deletingAttachmentId, setDeletingAttachmentId] = useState<string | null>(null);

  const isEditMode = Boolean(id);
  const isCollaborator = session?.user.role === UserRole.Collaborator;
  const selectedCategory = useMemo(
    () => categories.find((category) => category.id === formState.categoryId) ?? null,
    [categories, formState.categoryId]
  );

  useEffect(() => {
    let isCancelled = false;

    const loadPageData = async () => {
      setIsLoading(true);
      setErrorMessage(null);

      try {
        const categoriesPromise = reimbursementService.getAvailableCategories();

        if (id) {
          const [availableCategories, reimbursementDetail] = await Promise.all([
            categoriesPromise,
            reimbursementService.getById(id)
          ]);

          if (!isCancelled) {
            setCategories(availableCategories);
            setDetail(reimbursementDetail);
            setFormState(toDraftFormState(reimbursementDetail));
          }
        } else {
          const availableCategories = await categoriesPromise;

          if (!isCancelled) {
            setCategories(availableCategories);
            setDetail(null);
            setFormState((current) => ({
              ...createEmptyFormState(),
              currency: current.currency || "BRL"
            }));
          }
        }
      } catch (error) {
        if (!isCancelled) {
          const message =
            error instanceof ApiError
              ? error.message
              : "Não foi possível carregar o formulário da solicitação.";

          setErrorMessage(message);
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    };

    void loadPageData();

    return () => {
      isCancelled = true;
    };
  }, [id]);

  const updateFormField = (field: keyof DraftFormState, value: string) => {
    setSuccessMessage(null);
    setErrorMessage(null);
    setFieldErrors((current) => {
      if (!current || !current[field]) {
        return current;
      }

      const nextErrors = { ...current };
      delete nextErrors[field];
      return Object.keys(nextErrors).length > 0 ? nextErrors : null;
    });
    setFormState((current) => ({
      ...current,
      [field]: value
    }));
  };

  const buildPayload = (): CreateReimbursementPayload => ({
    title: formState.title.trim(),
    categoryId: formState.categoryId,
    amount: Number(formState.amount.replace(",", ".")),
    currency: formState.currency.trim().toUpperCase(),
    expenseDate: formState.expenseDate,
    description: formState.description.trim()
  });

  const refreshDraft = async (requestId: string) => {
    const nextDetail = await reimbursementService.getById(requestId);
    setDetail(nextDetail);
    setFormState(toDraftFormState(nextDetail));
    return nextDetail;
  };

  const handleSave = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSaving(true);
    setErrorMessage(null);
    setFieldErrors(null);
    setSuccessMessage(null);

    try {
      const payload = buildPayload();

      if (detail && id) {
        const updatePayload: UpdateReimbursementDraftPayload = {
          ...payload,
          rowVersion: detail.rowVersion
        };

        const updated = await reimbursementService.updateDraft(id, updatePayload);
        setDetail(updated);
        setFormState(toDraftFormState(updated));
        setSuccessMessage("Rascunho atualizado com sucesso.");
      } else {
        const created = await reimbursementService.create(payload);
        setDetail(created);
        setFormState(toDraftFormState(created));
        setSuccessMessage("Rascunho criado com sucesso. Agora você já pode anexar comprovantes.");
        navigate(`/solicitacoes/${created.id}/editar`, { replace: true });
      }
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
        setFieldErrors(error.problemDetails?.errors ?? null);
      } else {
        setErrorMessage("Não foi possível salvar o rascunho.");
      }
    } finally {
      setIsSaving(false);
    }
  };

  const handleSubmitDraft = async () => {
    if (!detail) {
      return;
    }

    setIsSubmittingDraft(true);
    setErrorMessage(null);
    setFieldErrors(null);
    setSuccessMessage(null);

    try {
      await reimbursementService.submit(detail.id);
      navigate(`/solicitacoes/${detail.id}`, { replace: true });
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
        setFieldErrors(error.problemDetails?.errors ?? null);
      } else {
        setErrorMessage("Não foi possível enviar a solicitação.");
      }
    } finally {
      setIsSubmittingDraft(false);
    }
  };

  const handleAttachmentUpload = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = "";

    if (!detail || !file) {
      return;
    }

    setIsUploading(true);
    setErrorMessage(null);
    setSuccessMessage(null);

    try {
      await reimbursementService.addAttachment(detail.id, file);
      await refreshDraft(detail.id);
      setSuccessMessage("Anexo incluído com sucesso.");
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
        setFieldErrors(error.problemDetails?.errors ?? null);
      } else {
        setErrorMessage("Não foi possível incluir o anexo.");
      }
    } finally {
      setIsUploading(false);
    }
  };

  const handleAttachmentDelete = async (attachment: Attachment) => {
    if (!detail) {
      return;
    }

    setDeletingAttachmentId(attachment.id);
    setErrorMessage(null);
    setSuccessMessage(null);

    try {
      await reimbursementService.deleteAttachment(detail.id, attachment.id);
      await refreshDraft(detail.id);
      setSuccessMessage("Anexo removido com sucesso.");
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage("Não foi possível remover o anexo.");
      }
    } finally {
      setDeletingAttachmentId(null);
    }
  };

  if (!isCollaborator) {
    return (
      <div className="workspace-page">
        <PageHeader
          eyebrow="Rascunho"
          title="Acesso restrito"
          description="Somente o colaborador responsável pode criar ou editar uma solicitação de reembolso."
        />
        <PlaceholderState
          eyebrow="Sem permissão"
          title="Esta área é exclusiva do colaborador"
          description="Gestores, financeiro e administradores acompanham o fluxo pelas telas operacionais do sistema."
        />
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="workspace-page">
        <PageHeader
          eyebrow="Rascunho"
          title="Preparando o formulário"
          description="Estamos carregando as categorias ativas e o conteúdo do rascunho."
        />
        <div className="table-state">
          <strong>Carregando formulário...</strong>
          <span>Os campos, anexos e ações disponíveis serão exibidos em seguida.</span>
        </div>
      </div>
    );
  }

  if (isEditMode && detail && detail.status !== RequestStatus.Draft) {
    return (
      <div className="workspace-page">
        <PageHeader
          eyebrow="Rascunho"
          title="Edição indisponível"
          description="A solicitação já saiu do estado de rascunho e não pode mais ser alterada."
        />
        <PlaceholderState
          eyebrow="Fluxo encerrado"
          title="A edição está bloqueada"
          description="Volte para o detalhe da solicitação para acompanhar o andamento atual."
        />
      </div>
    );
  }

  if (errorMessage && !detail && categories.length === 0) {
    return (
      <div className="workspace-page">
        <PageHeader
          eyebrow="Rascunho"
          title="Não foi possível abrir o formulário"
          description="O carregamento inicial falhou e o rascunho não ficou disponível para edição."
        />
        <PlaceholderState
          eyebrow="Falha na consulta"
          title="Tente novamente"
          description={errorMessage}
        />
      </div>
    );
  }

  return (
    <div className="workspace-page">
      <PageHeader
        eyebrow="Rascunho"
        title={detail ? detail.requestNumber : "Nova solicitação"}
        description={
          detail
            ? "Atualize os dados da despesa, mantenha os comprovantes em ordem e envie a solicitação quando tudo estiver pronto."
            : "Preencha os dados essenciais, salve o rascunho e depois inclua os comprovantes necessários."
        }
        actions={
          <Link className="ui-button ui-button--secondary" to={detail ? `/solicitacoes/${detail.id}` : "/solicitacoes"}>
            {detail ? "Voltar ao detalhe" : "Voltar para a listagem"}
          </Link>
        }
      />

      {errorMessage ? <div className="inline-feedback inline-feedback--error">{errorMessage}</div> : null}
      {successMessage ? <div className="inline-feedback">{successMessage}</div> : null}

      <form className="draft-layout" onSubmit={handleSave}>
        <section className="workspace-section">
          <div className="section-title">
            <h2>Dados da despesa</h2>
            <p>Essas informações definem o protocolo, o valor e a política de comprovante da solicitação.</p>
          </div>

          <div className="form-grid">
            <label className="form-field form-field--full">
              <span>Título</span>
              <input
                type="text"
                value={formState.title}
                onChange={(event) => updateFormField("title", event.target.value)}
                placeholder="Ex.: Táxi para visita ao cliente"
                disabled={isSaving || isSubmittingDraft}
              />
              {getFieldError(fieldErrors, "title") ? <small>{getFieldError(fieldErrors, "title")}</small> : null}
            </label>

            <label className="form-field">
              <span>Categoria</span>
              <select
                value={formState.categoryId}
                onChange={(event) => updateFormField("categoryId", event.target.value)}
                disabled={isSaving || isSubmittingDraft}
              >
                <option value="">Selecione uma categoria</option>
                {categories.map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
              </select>
              {getFieldError(fieldErrors, "categoryId") ? <small>{getFieldError(fieldErrors, "categoryId")}</small> : null}
            </label>

            <label className="form-field">
              <span>Data da despesa</span>
              <input
                type="date"
                value={formState.expenseDate}
                onChange={(event) => updateFormField("expenseDate", event.target.value)}
                disabled={isSaving || isSubmittingDraft}
              />
            </label>

            <label className="form-field">
              <span>Valor</span>
              <input
                type="number"
                min="0"
                step="0.01"
                value={formState.amount}
                onChange={(event) => updateFormField("amount", event.target.value)}
                placeholder="0,00"
                disabled={isSaving || isSubmittingDraft}
              />
              {getFieldError(fieldErrors, "amount") ? <small>{getFieldError(fieldErrors, "amount")}</small> : null}
            </label>

            <label className="form-field">
              <span>Moeda</span>
              <input
                type="text"
                maxLength={3}
                value={formState.currency}
                onChange={(event) => updateFormField("currency", event.target.value.toUpperCase())}
                disabled={isSaving || isSubmittingDraft}
              />
              {getFieldError(fieldErrors, "currency") ? <small>{getFieldError(fieldErrors, "currency")}</small> : null}
            </label>

            <label className="form-field form-field--full">
              <span>Descrição</span>
              <textarea
                rows={5}
                value={formState.description}
                onChange={(event) => updateFormField("description", event.target.value)}
                placeholder="Descreva o contexto da despesa e o motivo do reembolso."
                disabled={isSaving || isSubmittingDraft}
              />
              {getFieldError(fieldErrors, "description") ? <small>{getFieldError(fieldErrors, "description")}</small> : null}
            </label>
          </div>

          {selectedCategory ? (
            <div className="draft-policy-box">
              <strong>{selectedCategory.name}</strong>
              {selectedCategory.description ? <p>{selectedCategory.description}</p> : null}
              <div className="draft-policy-box__meta">
                <span>
                  Limite máximo:{" "}
                  {selectedCategory.maxAmount ? currencyFormatter.format(selectedCategory.maxAmount) : "Sem limite definido"}
                </span>
                <span>
                  Comprovante obrigatório acima de:{" "}
                  {selectedCategory.receiptRequiredAboveAmount
                    ? currencyFormatter.format(selectedCategory.receiptRequiredAboveAmount)
                    : "Sem exigência adicional"}
                </span>
              </div>
            </div>
          ) : null}
        </section>

        <section className="workspace-section">
          <div className="section-title">
            <h2>Comprovantes</h2>
            <p>O upload fica disponível depois que o rascunho é salvo pela primeira vez.</p>
          </div>

          {!detail ? (
            <div className="table-state">
              <strong>Salve o rascunho para anexar arquivos</strong>
              <span>Assim que o protocolo for criado, o envio de comprovantes ficará disponível nesta seção.</span>
            </div>
          ) : (
            <>
              <div className="attachment-toolbar">
                <label
                  className={
                    detail.allowedActions.canUploadAttachment && !isUploading
                      ? "ui-button ui-button--secondary attachment-upload-button"
                      : "ui-button ui-button--secondary attachment-upload-button attachment-upload-button--disabled"
                  }
                >
                  {isUploading ? "Enviando..." : "Adicionar comprovante"}
                  <input
                    type="file"
                    accept=".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png"
                    onChange={handleAttachmentUpload}
                    disabled={!detail.allowedActions.canUploadAttachment || isUploading}
                  />
                </label>
                {getFieldError(fieldErrors, "attachments") ? <span>{getFieldError(fieldErrors, "attachments")}</span> : null}
              </div>

              {detail.attachments.length === 0 ? (
                <div className="table-state">
                  <strong>Nenhum comprovante enviado</strong>
                  <span>Inclua arquivos PDF, JPG ou PNG com até 10 MB.</span>
                </div>
              ) : (
                <div className="attachment-list">
                  {detail.attachments.map((attachment) => (
                    <article key={attachment.id} className="attachment-item">
                      <div>
                        <strong>{attachment.originalFileName}</strong>
                        <span>
                          {attachment.contentType} • {Math.max(1, Math.round(attachment.sizeInBytes / 1024))} KB •{" "}
                          {formatAttachmentDate(attachment.createdAt)}
                        </span>
                      </div>
                      {detail.allowedActions.canDeleteAttachment ? (
                        <button
                          className="ui-button ui-button--ghost"
                          type="button"
                          onClick={() => void handleAttachmentDelete(attachment)}
                          disabled={deletingAttachmentId === attachment.id}
                        >
                          {deletingAttachmentId === attachment.id ? "Removendo..." : "Remover"}
                        </button>
                      ) : null}
                    </article>
                  ))}
                </div>
              )}
            </>
          )}
        </section>

        <section className="workspace-section">
          <div className="section-title">
            <h2>Fechamento do rascunho</h2>
            <p>Salve as alterações quando ainda estiver montando a solicitação e envie apenas quando tudo estiver revisado.</p>
          </div>

          <div className="draft-actions">
            <button className="ui-button ui-button--secondary" type="submit" disabled={isSaving || isSubmittingDraft}>
              {isSaving ? "Salvando..." : detail ? "Salvar alterações" : "Salvar rascunho"}
            </button>

            {detail?.allowedActions.canSubmit ? (
              <button
                className="ui-button ui-button--primary"
                type="button"
                onClick={() => void handleSubmitDraft()}
                disabled={isSaving || isSubmittingDraft}
              >
                {isSubmittingDraft ? "Enviando..." : "Enviar solicitação"}
              </button>
            ) : null}
          </div>
        </section>
      </form>
    </div>
  );
}
