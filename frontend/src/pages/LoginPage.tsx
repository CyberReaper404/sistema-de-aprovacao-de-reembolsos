import { useState } from "react";
import type { FormEvent } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import loginVisual from "@/assets/login-visual.jpg";
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
    <div className="login-screen">
      <section className="login-screen__form-side">
        <div className="login-screen__brand">
          <span>
            <strong>NIO</strong> Ticket
          </span>
        </div>

        <div className="login-screen__form-block">
          <div className="login-screen__heading">
            <h1>Bem-vindo de volta</h1>
            <p>Centralize solicitações, aprovações e pagamentos em um fluxo claro e seguro.</p>
          </div>

          <form className="login-form" onSubmit={handleSubmit}>
            {errorMessage ? <div className="login-form__error">{errorMessage}</div> : null}

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
              {isSubmitting ? "Entrando..." : "Entrar com e-mail"}
            </button>
          </form>

          <p className="login-screen__note">Acesso restrito a usuários cadastrados no sistema.</p>
        </div>

        <div className="login-screen__footer">
          <a href="/">Ajuda</a>
          <a href="/">Termos</a>
          <a href="/">Privacidade</a>
        </div>
      </section>

      <section className="login-screen__visual-side" aria-hidden="true">
        <img src={loginVisual} alt="" />
      </section>
    </div>
  );
}
