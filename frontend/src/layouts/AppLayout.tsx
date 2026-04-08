import { NavLink, Outlet } from "react-router-dom";
import { useSession } from "@/features/auth/session-context";
import type { NavigationItem } from "@/types/navigation";

const navigationItems: NavigationItem[] = [
  { label: "Painel", to: "/", roles: ["Collaborator", "Manager", "Finance", "Administrator"] },
  { label: "Solicitações", to: "/solicitacoes", roles: ["Collaborator", "Manager", "Finance", "Administrator"] },
  { label: "Nova solicitação", to: "/solicitacoes/nova", roles: ["Collaborator"] },
  { label: "Pagamentos", to: "/pagamentos", roles: ["Finance", "Administrator"] },
  { label: "Administração", to: "/admin", roles: ["Administrator"] }
];

export function AppLayout() {
  const { session } = useSession();

  if (!session) {
    return null;
  }

  const visibleItems = navigationItems.filter((item) => item.roles.includes(session.user.role));

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
          <small>{session.user.email}</small>
        </footer>
      </aside>

      <div className="app-shell__main">
        <header className="app-shell__header">
          <div>
            <p className="app-shell__eyebrow">Base do frontend</p>
            <h1>NIO Ticket</h1>
          </div>
        </header>
        <main className="app-shell__content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
