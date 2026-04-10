import { useState } from "react";
import { NavLink, Outlet } from "react-router-dom";
import { useSession } from "@/features/auth/session-context";
import { UserRole, userRoleLabels } from "@/types/domain";
import type { NavigationItem } from "@/types/navigation";

const navigationItems: NavigationItem[] = [
  { label: "Painel", to: "/", roles: [UserRole.Collaborator, UserRole.Manager, UserRole.Finance, UserRole.Administrator] },
  { label: "Solicitações", to: "/solicitacoes", roles: [UserRole.Collaborator, UserRole.Manager, UserRole.Finance, UserRole.Administrator] },
  { label: "Nova solicitação", to: "/solicitacoes/nova", roles: [UserRole.Collaborator] },
  { label: "Pagamentos", to: "/pagamentos", roles: [UserRole.Finance, UserRole.Administrator] },
  { label: "Administração", to: "/admin", roles: [UserRole.Administrator] }
];

function iconFor(label: string) {
  switch (label) {
    case "Painel":
      return "□";
    case "Solicitações":
      return "▣";
    case "Nova solicitação":
      return "+";
    case "Pagamentos":
      return "◫";
    case "Administração":
      return "☰";
    default:
      return "•";
  }
}

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
        <div className="app-shell__brand-block">
          <div className="app-shell__brand-mark">N</div>
          <div className="app-shell__brand-copy">
            <span>NIO Ticket</span>
            <small>Gestão de reembolsos</small>
          </div>
        </div>

        <nav className="app-shell__nav" aria-label="Navegação principal">
          {visibleItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === "/"}
              className={({ isActive }) => (isActive ? "app-shell__nav-link app-shell__nav-link--active" : "app-shell__nav-link")}
            >
              <span className="app-shell__nav-icon" aria-hidden="true">
                {iconFor(item.label)}
              </span>
              <span>{item.label}</span>
            </NavLink>
          ))}
        </nav>

        <footer className="app-shell__footer">
          <div className="app-shell__user">
            <div className="app-shell__avatar">{session.user.fullName.slice(0, 1).toUpperCase()}</div>
            <div className="app-shell__user-copy">
              <span>{session.user.fullName}</span>
              <small>{userRoleLabels[session.user.role]}</small>
            </div>
          </div>
          <button className="app-shell__logout-button" type="button" onClick={handleLogout} disabled={isSigningOut}>
            {isSigningOut ? "Saindo..." : "Sair"}
          </button>
        </footer>
      </aside>

      <main className="app-shell__main">
        <div className="app-shell__surface">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
