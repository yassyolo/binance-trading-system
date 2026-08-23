import {
  createBrowserRouter,
  Navigate,
} from 'react-router'

import { AppShell } from '@/components/layout/AppShell'
import { RequireAuthentication } from '@/features/auth/RequireAuthentication'
import { AlertsPage } from '@/pages/AlertsPage'
import { AnalyticsPage } from '@/pages/AnalyticsPage'
import { AuditPage } from '@/pages/AuditPage'
import { BacktestsPage } from '@/pages/BacktestsPage'
import { BotDetailsPage } from '@/pages/BotDetailsPage'
import { BotsPage } from '@/pages/BotsPage'
import { HealthPage } from '@/pages/HealthPage'
import { LoginPage } from '@/pages/LoginPage'
import { NotFoundPage } from '@/pages/NotFoundPage'
import { OptimizationPage } from '@/pages/OptimizationPage'
import { OverviewPage } from '@/pages/OverviewPage'
import { PaperTradingPage } from '@/pages/PaperTradingPage'
import { PositionsPage } from '@/pages/PositionsPage'
import { ReplaysPage } from '@/pages/ReplaysPage'
import { SignalsPage } from '@/pages/SignalsPage'
import { TradesPage } from '@/pages/TradesPage'

export const router =
  createBrowserRouter([
    {
      path: '/login',
      element: <LoginPage />,
    },
    {
      element:
        <RequireAuthentication />,
      children: [
        {
          path: '/',
          element: (
            <Navigate
              to="/overview"
              replace
            />
          ),
        },
        {
          element: <AppShell />,
          children: [
            {
              path: '/overview',
              element: <OverviewPage />,
            },
            {
              path: '/bots',
              element: <BotsPage />,
            },
            {
              path: '/bots/:botName',
              element: <BotDetailsPage />,
            },
            {
              path: '/signals',
              element: <SignalsPage />,
            },
            {
              path: '/positions',
              element: <PositionsPage />,
            },
            {
              path: '/trades',
              element: <TradesPage />,
            },
            {
              path: '/analytics',
              element: <AnalyticsPage />,
            },
            {
              path: '/backtests',
              element: <BacktestsPage />,
            },
            {
              path: '/optimizations',
              element: <OptimizationPage />,
            },
            {
              path: '/replays',
              element: <ReplaysPage />,
            },
            {
              path: '/health',
              element: <HealthPage />,
            },
            {
              path: '/alerts',
              element: <AlertsPage />,
            },
            {
              path: '/audit',
              element: <AuditPage />,
            },
            {
              path: '/paper',
              element: <PaperTradingPage />,
            },
            {
              path: '*',
              element: <NotFoundPage />,
            },
          ],
        },
      ],
    },
  ])
