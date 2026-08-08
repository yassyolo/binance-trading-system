import { Check, RefreshCcw } from 'lucide-react'

import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import {
  DataTable,
  type DataTableColumn,
} from '@/components/ui/DataTable'
import { EmptyState } from '@/components/ui/EmptyState'
import { ErrorState } from '@/components/ui/ErrorState'
import { LoadingSkeleton } from '@/components/ui/LoadingSkeleton'
import { StatusBadge } from '@/components/ui/StatusBadge'
import {
  money,
  percent,
  pnlClass,
  score,
} from '@/features/optimization/optimization-formatters'
import { useOptimizationTrials } from '@/features/optimization/optimization.queries'
import type { OptimizationTrialDto } from '@/types/optimization'

const columns: DataTableColumn<OptimizationTrialDto>[] = [
  {
    key: 'rank',
    header: 'Rank',
    width: '70px',
    render: (trial) => (
      <span className="font-mono font-semibold text-[var(--color-text-primary)]">
        #{trial.rank}
      </span>
    ),
  },
  {
    key: 'selected',
    header: 'Selected',
    width: '95px',
    render: (trial) =>
      trial.selected ? (
        <StatusBadge
          label="Selected"
          tone="success"
        />
      ) : (
        <span className="text-xs text-[var(--color-text-muted)]">
          —
        </span>
      ),
  },
  {
    key: 'score',
    header: 'Score',
    align: 'right',
    render: (trial) => score(trial.score),
  },
  {
    key: 'netProfit',
    header: 'Net profit',
    align: 'right',
    render: (trial) => (
      <span className={pnlClass(trial.netProfit)}>
        {money(trial.netProfit)}
      </span>
    ),
  },
  {
    key: 'drawdown',
    header: 'Drawdown',
    align: 'right',
    render: (trial) =>
      percent(trial.drawdownPercent),
  },
  {
    key: 'winRate',
    header: 'Win rate',
    align: 'right',
    render: (trial) =>
      percent(trial.winRatePercent),
  },
  {
    key: 'trades',
    header: 'Trades',
    align: 'right',
    render: (trial) => trial.trades,
  },
  {
    key: 'parameters',
    header: 'Parameters',
    render: (trial) => (
      <div className="flex max-w-[520px] flex-wrap gap-1.5">
        {Object.entries(trial.parameters).map(
          ([name, value]) => (
            <span
              key={name}
              className="rounded-lg border border-[var(--color-border)] bg-[var(--color-app)] px-2 py-1 font-mono text-[10px] text-[var(--color-text-secondary)]"
            >
              {name}={value}
            </span>
          ),
        )}
      </div>
    ),
  },
]

export function OptimizationTrials({
  runId,
}: {
  runId: string | null
}) {
  const query =
    useOptimizationTrials(runId, 100)

  if (!runId) {
    return (
      <EmptyState
        title="Select an optimization run"
        description="Choose a persisted optimization run above to inspect its ranked parameter trials."
      />
    )
  }

  if (query.isPending) {
    return (
      <div className="space-y-2">
        {Array.from(
          { length: 7 },
          (_, index) => (
            <LoadingSkeleton
              key={index}
              className="h-14 w-full"
            />
          ),
        )}
      </div>
    )
  }

  if (query.isError) {
    return (
      <ErrorState
        title="Optimization trials unavailable"
        description={query.error.message}
        action={
          <Button
            onClick={() =>
              void query.refetch()
            }
          >
            Retry
          </Button>
        }
      />
    )
  }

  if (!query.data.length) {
    return (
      <EmptyState
        title="No trials found"
        description="No persisted optimization trials are available for the selected run."
      />
    )
  }

  const best =
    [...query.data].sort(
      (left, right) =>
        left.rank - right.rank,
    )[0]

  return (
    <div className="space-y-4">
      <Card className="p-5">
        <div className="flex items-start justify-between gap-6">
          <div>
            <p className="text-[10px] font-medium uppercase tracking-[0.10em] text-[var(--color-text-muted)]">
              Best candidate
            </p>

            <div className="mt-3 flex items-baseline gap-4">
              <span className="font-mono text-2xl font-semibold">
                #{best.rank}
              </span>
              <span className="text-sm text-[var(--color-text-secondary)]">
                Score {score(best.score)}
              </span>
              <span className={pnlClass(best.netProfit)}>
                {money(best.netProfit)}
              </span>
            </div>

            <div className="mt-4 flex flex-wrap gap-1.5">
              {Object.entries(best.parameters).map(
                ([name, value]) => (
                  <span
                    key={name}
                    className="rounded-lg border border-[var(--color-border)] bg-[var(--color-app)] px-2 py-1 font-mono text-[10px] text-[var(--color-text-secondary)]"
                  >
                    {name}={value}
                  </span>
                ),
              )}
            </div>
          </div>

          {best.selected && (
            <div className="flex items-center gap-2 text-xs text-[var(--color-success)]">
              <Check size={14} />
              Selected
            </div>
          )}
        </div>
      </Card>

      <div className="flex justify-end">
        <Button
          size="sm"
          leftIcon={
            <RefreshCcw
              size={14}
              className={
                query.isFetching
                  ? 'animate-spin'
                  : ''
              }
            />
          }
          onClick={() =>
            void query.refetch()
          }
        >
          Refresh
        </Button>
      </div>

      <div className="overflow-x-auto">
        <div className="min-w-[1250px]">
          <DataTable
            columns={columns}
            rows={query.data}
            rowKey={(trial) =>
              trial.trialId
            }
          />
        </div>
      </div>
    </div>
  )
}
