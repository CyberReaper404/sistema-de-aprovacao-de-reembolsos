import { Link } from "react-router-dom";

export function NotFoundPage() {
  return (
    <div className="not-found-page">
      <span>404</span>
      <h1>Página não encontrada</h1>
      <p>O endereço informado não existe nesta aplicação.</p>
      <Link to="/">Voltar ao início</Link>
    </div>
  );
}
