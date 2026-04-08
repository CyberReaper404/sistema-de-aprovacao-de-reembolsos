import { PlaceholderState } from "@/components/feedback/PlaceholderState";
import { PageSection } from "@/components/layout/PageSection";

export function LoginPage() {
  return (
    <div className="auth-page">
      <section className="auth-page__panel auth-page__panel--form">
        <div className="auth-page__brand">
          <strong>NIO</strong>
          <span>Ticket</span>
        </div>

        <PageSection
          title="Bem-vindo de volta"
          description="A autenticação completa entra na próxima rodada. Nesta etapa, a base do frontend já está pronta para receber o fluxo de sessão."
        >
          <form className="auth-form">
            <label>
              <span>E-mail</span>
              <input type="email" placeholder="nome@empresa.com" disabled />
            </label>
            <label>
              <span>Senha</span>
              <input type="password" placeholder="Sua senha" disabled />
            </label>
            <button type="button" disabled>
              Entrar
            </button>
          </form>
        </PageSection>
      </section>

      <section className="auth-page__panel auth-page__panel--visual" aria-hidden="true">
        <PlaceholderState
          eyebrow="Direção visual registrada"
          title="Área visual da autenticação"
          description="A imagem final e o acabamento fiel à referência entram na fase visual do login, mantendo a cor verde como eixo da marca."
        />
      </section>
    </div>
  );
}
