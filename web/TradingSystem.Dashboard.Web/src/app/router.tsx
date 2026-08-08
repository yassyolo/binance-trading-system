import { createBrowserRouter, Navigate } from 'react-router'

import { AppShell } from '@/components/layout/AppShell'
import { AnalyticsPage } from '@/pages/AnalyticsPage'
import { BacktestsPage } from '@/pages/BacktestsPage'
import { BotDetailsPage } from '@/pages/BotDetailsPage'
import { BotsPage } from '@/pages/BotsPage'
import { NotFoundPage } from '@/pages/NotFoundPage'
import { OptimizationPage } from '@/pages/OptimizationPage'
import { OverviewPage } from '@/pages/OverviewPage'
import { PlaceholderPage } from '@/pages/PlaceholderPage'
import { PositionsPage } from '@/pages/PositionsPage'
import { SignalsPage } from '@/pages/SignalsPage'
import { TradesPage } from '@/pages/TradesPage'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <Navigate to="/overview" replace />,
  },
  {
    element: <AppShell />,
    children: [
      { path: '/overview', element: <OverviewPage /> },
      { path: '/bots', element: <BotsPage /> },
      { path: '/bots/:botName', element: <BotDetailsPage /> },
      { path: '/signals', element: <SignalsPage /> },
      { path: '/positions', element: <PositionsPage /> },
      { path: '/trades', element: <TradesPage /> },
      { path: '/analytics', element: <AnalyticsPage /> },
      { path: '/backtests', element: <BacktestsPage /> },
      { path: '/optimizations', element: <OptimizationPage /> },
      { path: '/replays', element: <PlaceholderPage title="Replays" description="Replay historical event streams and inspect results." /> },
      { path: '/health', element: <PlaceholderPage title="Health" description="Service heartbeat and component health monitoring." /> },
      { path: '/alerts', element: <PlaceholderPage title="Alerts" description="Operational and trading alerts requiring attention." /> },
      { path: '/audit', element: <PlaceholderPage title="Audit" description="Configuration, command and administrative audit history." /> },
      { path: '/paper', element: <PlaceholderPage title="Paper Trading" description="Simulated trading account, positions and performance." /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
