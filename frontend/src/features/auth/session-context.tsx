import { createContext, startTransition, useContext, useEffect, useMemo, useRef, useState } from "react";
import type { ReactNode } from "react";
import { apiClient, authService } from "@/services/api";
import type { AuthSession, LoginPayload } from "@/types/auth";

type SessionStatus = "loading" | "authenticated" | "unauthenticated";

interface SessionContextValue {
  session: AuthSession | null;
  status: SessionStatus;
  isAuthenticated: boolean;
  login: (payload: LoginPayload) => Promise<void>;
  logout: () => Promise<void>;
  logoutAll: () => Promise<void>;
  refreshSession: () => Promise<boolean>;
}

const SessionContext = createContext<SessionContextValue | undefined>(undefined);

export function SessionProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(null);
  const [status, setStatus] = useState<SessionStatus>("loading");
  const sessionRef = useRef<AuthSession | null>(null);
  const statusRef = useRef<SessionStatus>("loading");
  const refreshPromiseRef = useRef<Promise<boolean> | null>(null);
  const refreshSessionRef = useRef<() => Promise<boolean>>(async () => false);

  const applySession = (nextSession: AuthSession | null) => {
    startTransition(() => {
      setSession(nextSession);
      setStatus(nextSession ? "authenticated" : "unauthenticated");
    });
  };

  const refreshSession = async () => {
    if (refreshPromiseRef.current) {
      return refreshPromiseRef.current;
    }

    refreshPromiseRef.current = (async () => {
      try {
        const nextSession = await authService.refresh();
        applySession(nextSession);
        return true;
      } catch {
        applySession(null);
        return false;
      } finally {
        refreshPromiseRef.current = null;
      }
    })();

    return refreshPromiseRef.current;
  };

  refreshSessionRef.current = refreshSession;

  const login = async (payload: LoginPayload) => {
    const nextSession = await authService.login(payload);
    applySession(nextSession);
  };

  const logout = async () => {
    try {
      if (sessionRef.current) {
        await authService.logout();
      }
    } finally {
      applySession(null);
    }
  };

  const logoutAll = async () => {
    try {
      if (sessionRef.current) {
        await authService.logoutAll();
      }
    } finally {
      applySession(null);
    }
  };

  useEffect(() => {
    sessionRef.current = session;
    statusRef.current = status;
  }, [session, status]);

  useEffect(() => {
    apiClient.setAccessTokenResolver(() => sessionRef.current?.accessToken ?? null);
    apiClient.setUnauthorizedHandler(async () => {
      if (statusRef.current === "loading") {
        return false;
      }

      return refreshSessionRef.current();
    });

    return () => {
      apiClient.setAccessTokenResolver();
      apiClient.setUnauthorizedHandler();
    };
  }, []);

  useEffect(() => {
    void refreshSession();
  }, []);

  const value = useMemo<SessionContextValue>(
    () => ({
      session,
      status,
      isAuthenticated: session !== null,
      login,
      logout,
      logoutAll,
      refreshSession
    }),
    [session, status]
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
