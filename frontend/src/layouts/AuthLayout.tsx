import { Outlet } from "react-router-dom";

export function AuthLayout() {
  return (
    <div className="auth-shell">
      <main className="auth-shell__content">
        <Outlet />
      </main>
    </div>
  );
}
