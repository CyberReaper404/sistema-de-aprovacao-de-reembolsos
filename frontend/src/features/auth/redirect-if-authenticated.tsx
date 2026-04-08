import { Navigate, Outlet } from "react-router-dom";
import { useSession } from "@/features/auth/session-context";

export function RedirectIfAuthenticated() {
  const { status, isAuthenticated } = useSession();

  if (status === "loading") {
    return <div className="session-state">Verificando sessão...</div>;
  }

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}
