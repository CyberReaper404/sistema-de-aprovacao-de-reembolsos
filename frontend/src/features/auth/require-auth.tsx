import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useSession } from "@/features/auth/session-context";

export function RequireAuth() {
  const location = useLocation();
  const { status, isAuthenticated } = useSession();

  if (status === "loading") {
    return <div className="session-state">Verificando sessão...</div>;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: `${location.pathname}${location.search}` }} />;
  }

  return <Outlet />;
}
