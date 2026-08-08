import { RefreshCcw } from 'lucide-react'

import { Button } from '@/components/ui/Button'
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
  runStatusTone,
  score,
  utcDate,
} from '@/features/backtests/backtest-formatters'
import { useRuns } from '@/features/backtests/backtests.queries'
import type {
  RunsQuery,
  RunSummaryDto,
} from '@/types/backtesting'

const columns: DataTableColumn<RunSummaryDto>[] = [
  {
    key: 'run',
    header: 'Run',
    render: (run) => (
      <div className="max-w-[190px]">
        <p
          className="truncate font-mono text-xs text-[var(--color-text-primary)]"
          title={run.runId}
        >
          {run.runId}
        </p>
        <p className="mt-1 text-xs text-[var(--color-text-muted)]">
          {run.runType}
        </p>
      </div>
    ),
  },
  {
    key: 'bot',
    header: 'Bot',
    render: (run) => (
      <div>
        <p className="font-medium text-[var(--color-text-primary)]">
          {run.botName}
        </p>
        <p className="mt-1 text-xs text-[var(--color-text-muted)]">
          v{run.strategyVersion}
        </p>
      </div>
    ),
  },
  {
    key: 'market',
    header: 'Market',
    render: (run) => (
      <div>
        <p>{run.symbol}</p>
        <p className="mt-1 text-xs text-[var(--color-text-muted)]">
          {run.interval}
        </p>
      </div>
    ),
  },
  {
    key: 'status',
    header: 'Status',
    render: (run) => (
      <StatusBadge
        label={run.status}
        tone={runStatusTone(run.status)}
      />
    ),
  },
  {
    key: 'netProfit',
    header: 'Net profit',
    align: 'right',
    render: (run) => (
      <span className={pnlClass(run.netProfit)}>
        {money(run.netProfit)}
      </span>
    ),
  },
  {
    key: 'winRate',
    header: 'Win rate',
    align: 'right',
    render: (run) => percent(run.winRatePercent),
  },
  {
    key: 'drawdown',
    header: 'Max DD',
    align: 'right',
    render: (run) => percent(run.maxDrawdownPercent),
  },
  {
    key: 'score',
    header: 'Score',
    align: 'right',
    render: (run) => score(run.score),
  },
  {
    key: 'started',
    header: 'Started UTC',
    render: (run) => (
      <span className="whitespace-nowrap text-xs">
        {utcDate(run.startedAtUtc)}
      </span>
    ),
  },
  {
    key: 'completed',
    header: 'Completed UTC',
    render: (run) => (
      <span className="whitespace-nowrap text-xs">
        {utcDate(run.completedAtUtc)}
      </span>
    ),
  },
]

interface BacktestRunsTableProps {
  query: RunsQuery
  onPrevious: () => void
  onNext: () => void
}

export function BacktestRunsTable({
  query,
  onPrevious,
  onNext,
}: BacktestRunsTableProps) {
  const runs = useRuns(query)

  const backtestRuns =
    runs.data?.filter(
      (run) =>
        run.runType.toLowerCase().includes('backtest') ||
        !run.runType.toLowerCase().includes('optimization'),
    ) ?? []

  if (runs.isPending) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 7 }, (_, index) => (
          <LoadingSkeleton
            key={index}
            className="h-14 w-full"
          />
        ))}
      </div>
    )
  }

  if (runs.isError) {
    return (
      <ErrorState
        title="Backtest runs unavailable"
        description={runs.error.message}
        action={
          <Button onClick={() => void runs.refetch()}>
            Retry
          </Button>
        }
      />
    )
  }

  if (backtestRuns.length === 0) {
    return (
      <EmptyState
        title="No backtest runs found"
        description="Completed or persisted backtest runs will appear here."
      />
    )
  }

  return (
    <div>
      <div className="mb-3 flex justify-end">
        <Button
          size="sm"
          leftIcon={
            <RefreshCcw
              className={runs.isFetching ? 'animate-spin' : ''}
              size={14}
            />
          }
          onClick={() => void runs.refetch()}
        >
          Refresh
        </Button>
      </div>

      <div className="overflow-x-auto">
        <div className="min-w-[1380px]">
          <DataTable
            columns={columns}
            rows={backtestRuns}
            rowKey={(run) => run.runId}
          />
        </div>
      </div>

      <div className="mt-4 flex items-center justify-between">
        <p className="text-xs text-[var(--color-text-muted)]">
          Page {Math.floor(query.skip / query.take) + 1} · {backtestRuns.length} run{backtestRuns.length === 1 ? '' : 's'}
        </p>

        <div className="flex gap-2">
          <Button
            size="sm"
            onClick={onPrevious}
            disabled={query.skip === 0 || runs.isFetching}
          >
            Previous
          </Button>

          <Button
            size="sm"
            onClick={onNext}
            disabled={
              runs.isFetching ||
              (runs.data?.length ?? 0) < query.take
            }
          >
            Next
          </Button>
        </div>
      </div>
    </div>
  )
}
