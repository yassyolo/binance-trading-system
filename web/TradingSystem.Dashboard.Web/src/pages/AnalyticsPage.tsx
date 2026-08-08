import { useState } from 'react'
import {
  Activity,
  Clock3,
  Crosshair,
  Percent,
  RefreshCcw,
  TrendingDown,
  TrendingUp,
  WalletCards,
} from 'lucide-react'

import { PageHeader } from '@/components/layout/PageHeader'
import { Button } from '@/components/ui/Button'
import { EmptyState } from '@/components/ui/EmptyState'
import { ErrorState } from '@/components/ui/ErrorState'
import { MetricCard } from '@/components/ui/MetricCard'
import { PageSection } from '@/components/ui/PageSection'
import { AnalyticsBreakdowns } from '@/features/analytics/AnalyticsBreakdowns'
import { AnalyticsFilters } from '@/features/analytics/AnalyticsFilters'
import { AnalyticsLoading } from '@/features/analytics/AnalyticsLoading'
import { EquityChart } from '@/features/analytics/EquityChart'
import { PriceChartPanel } from '@/features/analytics/PriceChartPanel'
import {
  minutes,
  money,
  percent,
} from '@/features/analytics/analytics-formatters'
import {
  useAnalytics,
  useEquity,
} from '@/features/analytics/analytics.queries'
import type { AnalyticsFilter } from '@/types/analytics'

export function AnalyticsPage() {
  const [filter, setFilter] = useState<AnalyticsFilter>({})
  const analytics = useAnalytics(filter)
  const equity = useEquity(filter.botName)

  return (
    <>
      <PageHeader
        title="Analytics"
        description="Performance, equity, signal quality and historical market context."
      />

      <main className="space-y-8 p-8">
        <AnalyticsFilters onApply={setFilter} />

        <PageSection
          title="Performance"
          description="Aggregated trading and signal metrics from persisted history."
          actions={
            <Button
              size="sm"
              leftIcon={
                <RefreshCcw
                  className={
                    analytics.isFetching || equity.isFetching
                      ? 'animate-spin'
                      : ''
                  }
                  size={14}
                />
              }
              onClick={() => {
                void analytics.refetch()
                void equity.refetch()
              }}
            >
              Refresh
            </Button>
          }
        >
          {analytics.isPending && <AnalyticsLoading />}

          {analytics.isError && (
            <ErrorState
              title="Analytics unavailable"
              description={analytics.error.message}
              action={
                <Button onClick={() => void analytics.refetch()}>
                  Retry
                </Button>
              }
            />
          )}

          {analytics.data && (
            <div className="space-y-6">
              <div className="grid grid-cols-4 gap-4">
                <MetricCard
                  label="Total PnL"
                  value={money(analytics.data.totalPnl)}
                  helper="All selected history"
                  trend={
                    analytics.data.totalPnl > 0
                      ? 'up'
                      : analytics.data.totalPnl < 0
                        ? 'down'
                        : 'neutral'
                  }
                  icon={<WalletCards size={18} />}
                />

                <MetricCard
                  label="Daily PnL"
                  value={money(analytics.data.dailyPnl)}
                  helper="Current UTC day"
                  trend={
                    analytics.data.dailyPnl > 0
                      ? 'up'
                      : analytics.data.dailyPnl < 0
                        ? 'down'
                        : 'neutral'
                  }
                  icon={<Activity size={18} />}
                />

                <MetricCard
                  label="Weekly PnL"
                  value={money(analytics.data.weeklyPnl)}
                  helper="Current week"
                  trend={
                    analytics.data.weeklyPnl > 0
                      ? 'up'
                      : analytics.data.weeklyPnl < 0
                        ? 'down'
                        : 'neutral'
                  }
                  icon={<TrendingUp size={18} />}
                />

                <MetricCard
                  label="Monthly PnL"
                  value={money(analytics.data.monthlyPnl)}
                  helper="Current month"
                  trend={
                    analytics.data.monthlyPnl > 0
                      ? 'up'
                      : analytics.data.monthlyPnl < 0
                        ? 'down'
                        : 'neutral'
                  }
                  icon={<TrendingUp size={18} />}
                />

                <MetricCard
                  label="Win rate"
                  value={percent(analytics.data.winRate)}
                  helper={`${analytics.data.openedSignals} opened signals`}
                  icon={<Percent size={18} />}
                />

                <MetricCard
                  label="Max drawdown"
                  value={percent(analytics.data.maxDrawdownPercent)}
                  helper="Peak-to-trough decline"
                  trend={
                    analytics.data.maxDrawdownPercent > 0
                      ? 'down'
                      : 'neutral'
                  }
                  icon={<TrendingDown size={18} />}
                />

                <MetricCard
                  label="Average win"
                  value={money(analytics.data.averageWin)}
                  helper={`Average loss ${money(analytics.data.averageLoss)}`}
                  trend="up"
                  icon={<Crosshair size={18} />}
                />

                <MetricCard
                  label="Average holding"
                  value={minutes(analytics.data.averageHoldingMinutes)}
                  helper="Mean position duration"
                  icon={<Clock3 size={18} />}
                />
              </div>

              <AnalyticsBreakdowns analytics={analytics.data} />
            </div>
          )}
        </PageSection>

        <PageSection
          title="Equity"
          description={
            filter.botName
              ? `Equity series for ${filter.botName}`
              : 'Combined equity history'
          }
        >
          {equity.isPending && <AnalyticsLoading />}

          {equity.isError && (
            <ErrorState
              title="Equity unavailable"
              description={equity.error.message}
              action={<Button onClick={() => void equity.refetch()}>Retry</Button>}
            />
          )}

          {equity.data && equity.data.length === 0 && (
            <EmptyState
              title="No equity history"
              description="No equity points are available for the selected bot."
            />
          )}

          {equity.data && equity.data.length > 0 && (
            <EquityChart points={equity.data} />
          )}
        </PageSection>

        <PageSection
          title="Market chart"
          description="Historical candles and persisted trading markers."
        >
          <PriceChartPanel />
        </PageSection>
      </main>
    </>
  )
}
