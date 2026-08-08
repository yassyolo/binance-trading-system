import type { LucideIcon } from 'lucide-react'
import {
  Activity,
  Bot,
  ChartNoAxesCombined,
  FlaskConical,
  Gauge,
  HeartPulse,
  History,
  LayoutDashboard,
  Radio,
  ScrollText,
  SlidersHorizontal,
  TriangleAlert,
  WalletCards,
} from 'lucide-react'

export interface NavigationItem {
  label: string
  path: string
  icon: LucideIcon
}

export interface NavigationGroup {
  label: string
  items: NavigationItem[]
}

export const navigationGroups: NavigationGroup[] = [
  {
    label: 'Overview',
    items: [
      { label: 'Overview', path: '/overview', icon: LayoutDashboard },
    ],
  },
  {
    label: 'Trading',
    items: [
      { label: 'Bots', path: '/bots', icon: Bot },
      { label: 'Signals', path: '/signals', icon: Radio },
      { label: 'Positions', path: '/positions', icon: WalletCards },
      { label: 'Trades', path: '/trades', icon: Activity },
    ],
  },
  {
    label: 'Research',
    items: [
      { label: 'Analytics', path: '/analytics', icon: ChartNoAxesCombined },
      { label: 'Backtests', path: '/backtests', icon: FlaskConical },
      { label: 'Optimization', path: '/optimizations', icon: SlidersHorizontal },
      { label: 'Replays', path: '/replays', icon: History },
    ],
  },
  {
    label: 'Operations',
    items: [
      { label: 'Health', path: '/health', icon: HeartPulse },
      { label: 'Alerts', path: '/alerts', icon: TriangleAlert },
      { label: 'Audit', path: '/audit', icon: ScrollText },
    ],
  },
  {
    label: 'Simulation',
    items: [
      { label: 'Paper Trading', path: '/paper', icon: Gauge },
    ],
  },
]
