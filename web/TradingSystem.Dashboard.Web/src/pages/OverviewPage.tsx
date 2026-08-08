import {
  Activity,
  Bot,
  CircleDollarSign,
  RefreshCcw,
  TriangleAlert,
  WalletCards,
} from 'lucide-react'

import { ApiError } from '@/api/api-error'
import { PageHeader } from '@/components/layout/PageHeader'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { DataTable, type DataTableColumn } from '@/components/ui/DataTable'
import { EmptyState } from '@/components/ui/EmptyState'
import { ErrorState } from '@/components/ui/ErrorState'
import { MetricCard } from '@/components/ui/MetricCard'
import { PageSection } from '@/components/ui/PageSection'
import { StatusBadge } from '@/components/ui/StatusBadge'
import { DevelopmentAccessPanel } from '@/features/overview/DevelopmentAccessPanel'
import {
  environmentLabel,
  healthTone,
  money,
  relativeTime,
  runtimeStatusLabel,
  runtimeStatusTone,
  shortUtc,
} from '@/features/overview/overview-formatters'
import { OverviewLoading } from '@/features/overview/OverviewLoading'
import { useOverview } from '@/features/overview/useOverview'
import type { BotOverviewDto, ComponentHealthDto } from '@/types/dashboard-overview'

const botColumns: DataTableColumn<BotOverviewDto>[] = [
  {
    key: 'bot',
    header: 'Bot',
    render: (bot) => (
      <div>
        <p className="font-medium text-[var(--color-text-primary)]">{bot.botName}</p>
        <p className="mt-1 text-xs text-[var(--color-text-muted)]">v{bot.strategyVersion}</p>
      </div>
    ),
  },
  {
    key: 'status',
    header: 'Status',
    render: (bot) => (
      <StatusBadge label={runtimeStatusLabel(bot.status)} tone={runtimeStatusTone(bot.status)} />
    ),
  },
  {
    key: 'environment',
    header: 'Environment',
    render: (bot) => environmentLabel(bot.environment),
  },
  {
    key: 'signal',
    header: 'Last signal',
    render: (bot) => (
      <div>
        <p>{bot.lastSignalSide ?? '—'}</p>
        <p className="mt-1 text-xs text-[var(--color-text-muted)]">{relativeTime(bot.lastSignalAtUtc)}</p>
      </div>
    ),
  },
  {
    key: 'decision',
    header: 'Last decision',
    render: (bot) => (
      <div className="max-w-[250px]">
        <p className="truncate text-[var(--color-text-primary)]">{bot.lastDecision ?? '—'}</p>
        <p className="mt-1 truncate text-xs text-[var(--color-text-muted)]">{bot.lastDecisionReason ?? 'No reason recorded'}</p>
      </div>
    ),
  },
  {
    key: 'positions',
    header: 'Positions',
    align: 'right',
    render: (bot) => bot.openPositions,
  },
  {
    key: 'pnl',
    header: 'Today PnL',
    align: 'right',
    render: (bot) => (
      <span className={bot.realizedPnlToday < 0 ? 'text-[var(--color-danger)]' : bot.realizedPnlToday > 0 ? 'text-[var(--color-success)]' : ''}>
        {money(bot.realizedPnlToday)}
      </span>
    ),
  },
]

const componentColumns: DataTableColumn<ComponentHealthDto>[] = [
  {
    key: 'component',
    header: 'Component',
    render: (component) => <span className="font-medium text-[var(--color-text-primary)]">{component.component}</span>,
  },
  {
    key: 'status',
    header: 'Status',
    render: (component) => <StatusBadge label={component.status} tone={healthTone(component.status)} />,
  },
  {
    key: 'lastSeen',
    header: 'Last seen',
    render: (component) => (
      <div>
        <p>{relativeTime(component.lastSeenUtc)}</p>
        <p className="mt-1 text-xs text-[var(--color-text-muted)]">{shortUtc(component.lastSeenUtc)}</p>
      </div>
    ),
  },
  {
    key: 'details',
    header: 'Details',
    render: (component) => <span className="text-xs">{component.details || '—'}</span>,
  },
]

export function OverviewPage() {
  const query = useOverview()
  const error = query.error instanceof ApiError ? query.error : null

  if (query.isPending) {
    return (
      <>
        <PageHeader title="Overview" description="Live operational snapshot of the trading system." />
        <OverviewLoading />
      </>
    )
  }

  if (error?.status === 401 || error?.status === 403) {
    return (
      <>
        <PageHeader title="Overview" description="Live operational snapshot of the trading system." />
        <main className="space-y-5 p-8">
          <ErrorState
            title={error.status === 401 ? 'Authentication required' : 'Access denied'}
            description={
              error.status === 401
                ? 'The Dashboard API requires a valid JWT before live data can be loaded.'
                : 'The current token does not have Viewer access.'
            }
          />
          <DevelopmentAccessPanel onSaved={() => void query.refetch()} />
        </main>
      </>
    )
  }

  if (query.isError || !query.data) {
    return (
      <>
        <PageHeader title="Overview" description="Live operational snapshot of the trading system." />
        <main className="p-8">
          <ErrorState
            title="Dashboard API unavailable"
            description={error?.message ?? 'The overview request could not be completed.'}
            action={<Button onClick={() => void query.refetch()}>Retry</Button>}
          />
        </main>
      </>
    )
  }

  const overview = query.data
  const activeBots = overview.bots.filter((bot) => runtimeStatusLabel(bot.status) === 'Running').length

  return (
    <>
      <PageHeader title="Overview" description={`Generated ${shortUtc(overview.generatedAtUtc)} UTC`} />

      <main className="space-y-10 p-8">
        <PageSection
          title="Live metrics"
          description="Current aggregate state returned by the Dashboard API."
          actions={
            <Button
              size="sm"
              leftIcon={<RefreshCcw className={query.isFetching ? 'animate-spin' : ''} size={14} />}
              onClick={() => void query.refetch()}
              disabled={query.isFetching}
            >
              Refresh
            </Button>
          }
        >
          <div className="grid grid-cols-4 gap-4">
            <MetricCard
              label="Realized PnL today"
              value={money(overview.realizedPnlToday)}
              helper="Closed trading result today"
              trend={overview.realizedPnlToday > 0 ? 'up' : overview.realizedPnlToday < 0 ? 'down' : 'neutral'}
              icon={<CircleDollarSign size={18} />}
            />
            <MetricCard
              label="Unrealized PnL"
              value={money(overview.unrealizedPnl)}
              helper="Current open-position estimate"
              trend={overview.unrealizedPnl > 0 ? 'up' : overview.unrealizedPnl < 0 ? 'down' : 'neutral'}
              icon={<Activity size={18} />}
            />
            <MetricCard
              label="Open positions"
              value={String(overview.openPositions)}
              helper={`${activeBots} running bot${activeBots === 1 ? '' : 's'}`}
              icon={<WalletCards size={18} />}
            />
            <MetricCard
              label="Critical alerts"
              value={String(overview.criticalAlerts)}
              helper={overview.criticalAlerts === 0 ? 'No critical alerts' : 'Requires operator attention'}
              trend={overview.criticalAlerts > 0 ? 'down' : 'neutral'}
              icon={<TriangleAlert size={18} />}
            />
          </div>
        </PageSection>

        <PageSection title="Bots" description={`${overview.bots.length} bot runtime snapshots`}>
          {overview.bots.length === 0 ? (
            <EmptyState
              title="No bots found"
              description="The overview response did not contain any bot runtime snapshots."
              action={<Button leftIcon={<Bot size={15} />}>Open Bots</Button>}
            />
          ) : (
            <DataTable columns={botColumns} rows={overview.bots} rowKey={(bot) => bot.botName} />
          )}
        </PageSection>

        <PageSection title="Components" description="Service heartbeat and component health reported by the backend.">
          {overview.components.length === 0 ? (
            <Card className="p-6 text-sm text-[var(--color-text-secondary)]">
              No component health records are currently available.
            </Card>
          ) : (
            <DataTable
              columns={componentColumns}
              rows={overview.components}
              rowKey={(component) => component.component}
            />
          )}
        </PageSection>
      </main>
    </>
  )
}
