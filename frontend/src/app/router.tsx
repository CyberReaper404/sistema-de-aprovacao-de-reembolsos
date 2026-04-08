import { createBrowserRouter } from "react-router-dom";
import { RequireAuth } from "@/features/auth/require-auth";
import { AuthLayout } from "@/layouts/AuthLayout";
import { AppLayout } from "@/layouts/AppLayout";
import { AdminPage } from "@/pages/AdminPage";
import { DashboardPage } from "@/pages/DashboardPage";
import { LoginPage } from "@/pages/LoginPage";
import { NotFoundPage } from "@/pages/NotFoundPage";
import { PaymentsPage } from "@/pages/PaymentsPage";
import { ReimbursementDetailPage } from "@/pages/ReimbursementDetailPage";
import { ReimbursementDraftPage } from "@/pages/ReimbursementDraftPage";
import { ReimbursementsPage } from "@/pages/ReimbursementsPage";

export const router = createBrowserRouter([
  {
    element: <AuthLayout />,
    children: [
      {
        path: "/login",
        element: <LoginPage />
      }
    ]
  },
  {
    element: <RequireAuth />,
    children: [
      {
        element: <AppLayout />,
        children: [
          {
            path: "/",
            element: <DashboardPage />
          },
          {
            path: "/solicitacoes",
            element: <ReimbursementsPage />
          },
          {
            path: "/solicitacoes/nova",
            element: <ReimbursementDraftPage />
          },
          {
            path: "/solicitacoes/:id",
            element: <ReimbursementDetailPage />
          },
          {
            path: "/pagamentos",
            element: <PaymentsPage />
          },
          {
            path: "/admin",
            element: <AdminPage />
          }
        ]
      }
    ]
  },
  {
    path: "*",
    element: <NotFoundPage />
  }
]);
