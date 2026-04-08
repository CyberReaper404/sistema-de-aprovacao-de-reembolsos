import { MemoryRouter } from "react-router-dom";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ReimbursementDraftPage } from "@/pages/ReimbursementDraftPage";
import { createCategory, createReimbursementDetail, createSession } from "@/tests/reimbursement-test-data";
import { UserRole } from "@/types/domain";

const {
  mockedNavigate,
  mockedUseParams,
  mockedUseSession,
  reimbursementServiceMock
} = vi.hoisted(() => ({
  mockedNavigate: vi.fn(),
  mockedUseParams: vi.fn(),
  mockedUseSession: vi.fn(),
  reimbursementServiceMock: {
    getAvailableCategories: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    updateDraft: vi.fn(),
    submit: vi.fn(),
    addAttachment: vi.fn(),
    deleteAttachment: vi.fn()
  }
}));

vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual<typeof import("react-router-dom")>("react-router-dom");

  return {
    ...actual,
    useNavigate: () => mockedNavigate,
    useParams: () => mockedUseParams()
  };
});

vi.mock("@/features/auth/session-context", () => ({
  useSession: () => mockedUseSession()
}));

vi.mock("@/services/api", () => ({
  reimbursementService: reimbursementServiceMock
}));

describe("ReimbursementDraftPage", () => {
  beforeEach(() => {
    mockedNavigate.mockReset();
    mockedUseParams.mockReset();
    mockedUseSession.mockReset();
    reimbursementServiceMock.getAvailableCategories.mockReset();
    reimbursementServiceMock.getById.mockReset();
    reimbursementServiceMock.create.mockReset();
    reimbursementServiceMock.updateDraft.mockReset();
    reimbursementServiceMock.submit.mockReset();
    reimbursementServiceMock.addAttachment.mockReset();
    reimbursementServiceMock.deleteAttachment.mockReset();
  });

  it("deve criar um novo rascunho com os dados preenchidos", async () => {
    const user = userEvent.setup();
    const category = createCategory();
    const createdDraft = createReimbursementDetail();

    mockedUseParams.mockReturnValue({});
    mockedUseSession.mockReturnValue({
      session: createSession(),
      status: "authenticated",
      isAuthenticated: true
    });
    reimbursementServiceMock.getAvailableCategories.mockResolvedValue([category]);
    reimbursementServiceMock.create.mockResolvedValue(createdDraft);

    render(
      <MemoryRouter>
        <ReimbursementDraftPage />
      </MemoryRouter>
    );

    await screen.findByRole("option", { name: category.name });

    const textInputs = screen.getAllByRole("textbox");
    const titleInput = textInputs[0];
    const currencyInput = textInputs[1];
    const descriptionInput = textInputs[2];
    const selects = screen.getAllByRole("combobox");
    const categorySelect = selects[0];
    const dateInput = document.querySelector("input[type='date']") as HTMLInputElement;
    const amountInput = document.querySelector("input[type='number']") as HTMLInputElement;

    await user.type(titleInput, "Taxi para visita ao cliente");
    await user.selectOptions(categorySelect, category.id);
    await user.type(dateInput, "2026-04-05");
    await user.type(amountInput, "125.40");
    await user.clear(currencyInput);
    await user.type(currencyInput, "BRL");
    await user.type(
      descriptionInput,
      "Corrida entre o escritorio e a reuniao externa."
    );

    await user.click(screen.getByRole("button", { name: "Salvar rascunho" }));

    await waitFor(() => {
      expect(reimbursementServiceMock.create).toHaveBeenCalledWith({
        title: "Taxi para visita ao cliente",
        categoryId: category.id,
        amount: 125.4,
        currency: "BRL",
        expenseDate: "2026-04-05",
        description: "Corrida entre o escritorio e a reuniao externa."
      });
    });

    expect(mockedNavigate).toHaveBeenCalledWith(`/solicitacoes/${createdDraft.id}/editar`, {
      replace: true
    });
  });

  it("deve atualizar um rascunho existente sem perder a rowVersion", async () => {
    const user = userEvent.setup();
    const category = createCategory();
    const existingDraft = createReimbursementDetail();
    const updatedDraft = createReimbursementDetail({
      title: "Taxi atualizado",
      rowVersion: "AAAAAAABAAE="
    });

    mockedUseParams.mockReturnValue({ id: existingDraft.id });
    mockedUseSession.mockReturnValue({
      session: createSession(),
      status: "authenticated",
      isAuthenticated: true
    });
    reimbursementServiceMock.getAvailableCategories.mockResolvedValue([category]);
    reimbursementServiceMock.getById.mockResolvedValue(existingDraft);
    reimbursementServiceMock.updateDraft.mockResolvedValue(updatedDraft);

    render(
      <MemoryRouter>
        <ReimbursementDraftPage />
      </MemoryRouter>
    );

    await screen.findByDisplayValue(existingDraft.title);

    const titleInput = screen.getAllByRole("textbox")[0];
    await user.clear(titleInput);
    await user.type(titleInput, "Taxi atualizado");
    await user.click(screen.getByRole("button", { name: /Salvar/i }));

    await waitFor(() => {
      expect(reimbursementServiceMock.updateDraft).toHaveBeenCalledWith(existingDraft.id, {
        title: "Taxi atualizado",
        categoryId: existingDraft.categoryId,
        amount: existingDraft.amount,
        currency: existingDraft.currency,
        expenseDate: existingDraft.expenseDate,
        description: existingDraft.description,
        rowVersion: existingDraft.rowVersion
      });
    });
  });

  it("deve enviar a solicitacao quando o rascunho permitir submissao", async () => {
    const user = userEvent.setup();
    const category = createCategory();
    const editableDraft = createReimbursementDetail();

    mockedUseParams.mockReturnValue({ id: editableDraft.id });
    mockedUseSession.mockReturnValue({
      session: createSession(),
      status: "authenticated",
      isAuthenticated: true
    });
    reimbursementServiceMock.getAvailableCategories.mockResolvedValue([category]);
    reimbursementServiceMock.getById.mockResolvedValue(editableDraft);
    reimbursementServiceMock.submit.mockResolvedValue(undefined);

    render(
      <MemoryRouter>
        <ReimbursementDraftPage />
      </MemoryRouter>
    );

    await screen.findByDisplayValue(editableDraft.title);
    await user.click(screen.getByRole("button", { name: /Enviar/i }));

    await waitFor(() => {
      expect(reimbursementServiceMock.submit).toHaveBeenCalledWith(editableDraft.id);
    });

    expect(mockedNavigate).toHaveBeenCalledWith(`/solicitacoes/${editableDraft.id}`, {
      replace: true
    });
  });

  it("deve bloquear o formulario para usuarios que nao sao colaboradores", async () => {
    mockedUseParams.mockReturnValue({});
    reimbursementServiceMock.getAvailableCategories.mockResolvedValue([]);
    mockedUseSession.mockReturnValue({
      session: createSession({
        user: {
          ...createSession().user,
          role: UserRole.Manager
        }
      }),
      status: "authenticated",
      isAuthenticated: true
    });

    render(
      <MemoryRouter>
        <ReimbursementDraftPage />
      </MemoryRouter>
    );

    expect(screen.getByText("Acesso restrito")).toBeInTheDocument();
    expect(screen.getByText(/exclusiva do colaborador/i)).toBeInTheDocument();
  });

  it("deve carregar um comprovante no rascunho salvo", async () => {
    const category = createCategory();
    const editableDraft = createReimbursementDetail();
    const refreshedDraft = createReimbursementDetail({
      attachments: [
        {
          id: "anexo-1",
          originalFileName: "comprovante.pdf",
          contentType: "application/pdf",
          sizeInBytes: 1024,
          createdAt: "2026-04-05T14:30:00Z"
        }
      ]
    });

    mockedUseParams.mockReturnValue({ id: editableDraft.id });
    mockedUseSession.mockReturnValue({
      session: createSession(),
      status: "authenticated",
      isAuthenticated: true
    });
    reimbursementServiceMock.getAvailableCategories.mockResolvedValue([category]);
    reimbursementServiceMock.getById
      .mockResolvedValueOnce(editableDraft)
      .mockResolvedValueOnce(refreshedDraft);
    reimbursementServiceMock.addAttachment.mockResolvedValue(refreshedDraft.attachments[0]);

    render(
      <MemoryRouter>
        <ReimbursementDraftPage />
      </MemoryRouter>
    );

    await screen.findByDisplayValue(editableDraft.title);

    const fileInput = document.querySelector("input[type='file']") as HTMLInputElement | null;
    expect(fileInput).not.toBeNull();

    const file = new File(["conteudo"], "comprovante.pdf", { type: "application/pdf" });
    fireEvent.change(fileInput!, { target: { files: [file] } });

    await waitFor(() => {
      expect(reimbursementServiceMock.addAttachment).toHaveBeenCalledWith(editableDraft.id, file);
    });

    expect(await screen.findByText("comprovante.pdf")).toBeInTheDocument();
  });
});
