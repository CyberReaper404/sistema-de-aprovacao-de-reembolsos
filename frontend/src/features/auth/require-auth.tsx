import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useSession } from "@/features/auth/session-context";

export function RequireAuth() {
  const location = useLocation();
  const { isAuthenticated } = useSession();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  return <Outlet />;
}
