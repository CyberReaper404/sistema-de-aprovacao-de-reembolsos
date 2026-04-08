import { createContext, useContext, useMemo, useState } from "react";
import type { ReactNode } from "react";
import type { AuthSession } from "@/types/auth";

interface SessionContextValue {
  session: AuthSession | null;
  isAuthenticated: boolean;
  setSession: (session: AuthSession | null) => void;
}

const SessionContext = createContext<SessionContextValue | undefined>(undefined);

export function SessionProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(null);

  const value = useMemo<SessionContextValue>(
    () => ({
      session,
      isAuthenticated: session !== null,
      setSession
    }),
    [session]
  );

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>;
}

export function useSession() {
  const context = useContext(SessionContext);

  if (!context) {
    throw new Error("useSession deve ser usado dentro de SessionProvider.");
  }

  return context;
}
