import { useState } from "react";
import type { FormEvent } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { PageSection } from "@/components/layout/PageSection";
import { useSession } from "@/features/auth/session-context";
import { ApiError } from "@/services/http/api-error";

export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { login, status } = useSession();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const redirectPath = (location.state as { from?: string } | null)?.from ?? "/";
  const isBlocked = status === "loading" || isSubmitting;

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setErrorMessage(null);
    setIsSubmitting(true);

    try {
      await login({
        email,
        password
      });

      navigate(redirectPath, { replace: true });
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage("Não foi possível iniciar a sessão. Tente novamente.");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="auth-page">
      <section className="auth-page__panel auth-page__panel--form">
        <div className="auth-page__brand">
          <strong>NIO</strong>
          <span>Ticket</span>
        </div>

        <PageSection
          title="Bem-vindo de volta"
          description="Entre com seu e-mail corporativo e sua senha para acessar o fluxo de reembolsos."
        >
          <form className="auth-form" onSubmit={handleSubmit}>
            {errorMessage ? <div className="auth-form__error">{errorMessage}</div> : null}

            <label>
              <span>E-mail</span>
              <input
                type="email"
                placeholder="nome@empresa.com"
                autoComplete="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                disabled={isBlocked}
                required
              />
            </label>
            <label>
              <span>Senha</span>
              <input
                type="password"
                placeholder="Sua senha"
                autoComplete="current-password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                disabled={isBlocked}
                required
              />
            </label>
            <button type="submit" disabled={isBlocked}>
              {isSubmitting ? "Entrando..." : "Entrar"}
            </button>
          </form>
        </PageSection>
      </section>

      <section className="auth-page__panel auth-page__panel--visual" aria-hidden="true">
        <div className="auth-page__visual-copy">
          <span>Fluxo corporativo</span>
          <h2>Controle solicitações, aprovações e pagamentos em um só lugar.</h2>
          <p>A identidade visual final do login entra na rodada de layout, mantendo a direção já aprovada para a marca.</p>
        </div>
      </section>
    </div>
  );
}
