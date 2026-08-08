import {
  Activity,
  CircleDollarSign,
  Receipt,
  WalletCards,
  Waypoints,
} from 'lucide-react'

import { MetricCard } from '@/components/ui/MetricCard'
import { money } from '@/features/paper/paper-formatters'
import type { PaperTradingAccountDto } from '@/types/paper-trading'

export function PaperAccountSummary({
  summary,
}: {
  summary: PaperTradingAccountDto
}) {
  return (
    <div className="grid grid-cols-3 gap-4">
      <MetricCard label="Initial balance" value={money(summary.initialBalance)} helper="Configured paper capital" icon={<CircleDollarSign size={18} />} />
      <MetricCard label="Equity" value={money(summary.equity)} helper="Current simulated equity" trend={summary.equity > summary.initialBalance ? 'up' : summary.equity < summary.initialBalance ? 'down' : 'neutral'} icon={<Activity size={18} />} />
      <MetricCard label="Realized PnL" value={money(summary.realizedPnl)} helper={`${summary.closedPositions} closed positions`} trend={summary.realizedPnl > 0 ? 'up' : summary.realizedPnl < 0 ? 'down' : 'neutral'} icon={<WalletCards size={18} />} />
      <MetricCard label="Open positions" value={String(summary.openPositions)} helper="Active simulated exposure" icon={<Waypoints size={18} />} />
      <MetricCard label="Closed positions" value={String(summary.closedPositions)} helper="Completed paper positions" icon={<WalletCards size={18} />} />
      <MetricCard label="Fees" value={money(summary.fees)} helper="Accumulated paper fees" icon={<Receipt size={18} />} />
    </div>
  )
}
