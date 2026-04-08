import { useState } from "react";
import { NavLink, Outlet } from "react-router-dom";
import { useSession } from "@/features/auth/session-context";
import { UserRole, userRoleLabels } from "@/types/domain";
import type { NavigationItem } from "@/types/navigation";

const navigationItems: NavigationItem[] = [
  { label: "Painel", to: "/", roles: [UserRole.Collaborator, UserRole.Manager, UserRole.Finance, UserRole.Administrator] },
  {
    label: "Solicitações",
    to: "/solicitacoes",
    roles: [UserRole.Collaborator, UserRole.Manager, UserRole.Finance, UserRole.Administrator]
  },
  { label: "Nova solicitação", to: "/solicitacoes/nova", roles: [UserRole.Collaborator] },
  { label: "Pagamentos", to: "/pagamentos", roles: [UserRole.Finance, UserRole.Administrator] },
  { label: "Administração", to: "/admin", roles: [UserRole.Administrator] }
];

export function AppLayout() {
  const { session, logout } = useSession();
  const [isSigningOut, setIsSigningOut] = useState(false);

  if (!session) {
    return null;
  }

  const visibleItems = navigationItems.filter((item) => item.roles.includes(session.user.role));

  const handleLogout = async () => {
    try {
      setIsSigningOut(true);
      await logout();
    } finally {
      setIsSigningOut(false);
    }
  };

  return (
    <div className="app-shell">
      <aside className="app-shell__sidebar">
        <div className="app-shell__brand">
          <strong>NIO</strong>
          <span>Ticket</span>
        </div>

        <nav className="app-shell__nav" aria-label="Navegação principal">
          {visibleItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === "/"}
              className={({ isActive }) =>
                isActive ? "app-shell__nav-link app-shell__nav-link--active" : "app-shell__nav-link"
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>

        <footer className="app-shell__footer">
          <span>{session.user.fullName}</span>
          <small>
            {userRoleLabels[session.user.role]} · {session.user.email}
          </small>
        </footer>
      </aside>

      <div className="app-shell__main">
        <header className="app-shell__header">
          <div>
            <p className="app-shell__eyebrow">Sessão autenticada</p>
            <h1>NIO Ticket</h1>
          </div>
          <button className="app-shell__logout-button" type="button" onClick={handleLogout} disabled={isSigningOut}>
            {isSigningOut ? "Saindo..." : "Sair"}
          </button>
        </header>
        <main className="app-shell__content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
