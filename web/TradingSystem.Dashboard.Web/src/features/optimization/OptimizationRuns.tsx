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
} from '@/features/optimization/optimization-formatters'
import { useOptimizationRuns } from '@/features/optimization/optimization.queries'
import type { RunSummaryDto } from '@/types/optimization'

interface OptimizationRunsProps {
  botName?: string
  skip: number
  take: number
  selectedRunId: string | null
  onSelect: (runId: string) => void
  onRunsLoaded: (runs: RunSummaryDto[]) => void
  onPrevious: () => void
  onNext: () => void
}

export function OptimizationRuns({
  botName,
  skip,
  take,
  selectedRunId,
  onSelect,
  onRunsLoaded,
  onPrevious,
  onNext,
}: OptimizationRunsProps) {
  const query =
    useOptimizationRuns(
      botName,
      skip,
      take,
    )

  const optimizationRuns =
    query.data?.filter((run) =>
      run.runType
        .toLowerCase()
        .includes('optimization'),
    ) ?? []

  if (
    query.data &&
    query.data !== undefined
  ) {
    queueMicrotask(() =>
      onRunsLoaded(optimizationRuns),
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
        title="Optimization runs unavailable"
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

  if (
    optimizationRuns.length === 0
  ) {
    return (
      <EmptyState
        title="No optimization runs"
        description="Persisted optimization runs will appear here after the worker starts processing requests."
      />
    )
  }

  const columns: DataTableColumn<RunSummaryDto>[] = [
    {
      key: 'run',
      header: 'Run',
      render: (run) => (
        <button
          type="button"
          onClick={() =>
            onSelect(run.runId)
          }
          className="max-w-[180px] text-left"
        >
          <p
            className={`truncate font-mono text-xs ${
              selectedRunId === run.runId
                ? 'text-[var(--color-info)]'
                : 'text-[var(--color-text-primary)]'
            }`}
            title={run.runId}
          >
            {run.runId}
          </p>
          <p className="mt-1 text-xs text-[var(--color-text-muted)]">
            {run.runType}
          </p>
        </button>
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
      key: 'profit',
      header: 'Net profit',
      align: 'right',
      render: (run) => (
        <span
          className={
            run.netProfit === null
              ? ''
              : pnlClass(run.netProfit)
          }
        >
          {money(run.netProfit)}
        </span>
      ),
    },
    {
      key: 'winRate',
      header: 'Win rate',
      align: 'right',
      render: (run) =>
        percent(run.winRatePercent),
    },
    {
      key: 'drawdown',
      header: 'Max DD',
      align: 'right',
      render: (run) =>
        percent(
          run.maxDrawdownPercent,
        ),
    },
    {
      key: 'score',
      header: 'Score',
      align: 'right',
      render: (run) =>
        score(run.score),
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
  ]

  return (
    <div>
      <div className="mb-3 flex justify-end">
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
        <div className="min-w-[1200px]">
          <DataTable
            columns={columns}
            rows={optimizationRuns}
            rowKey={(run) => run.runId}
          />
        </div>
      </div>

      <div className="mt-4 flex items-center justify-between">
        <p className="text-xs text-[var(--color-text-muted)]">
          Page {Math.floor(skip / take) + 1}
        </p>

        <div className="flex gap-2">
          <Button
            size="sm"
            onClick={onPrevious}
            disabled={
              skip === 0 ||
              query.isFetching
            }
          >
            Previous
          </Button>

          <Button
            size="sm"
            onClick={onNext}
            disabled={
              query.isFetching ||
              (query.data?.length ?? 0) <
                take
            }
          >
            Next
          </Button>
        </div>
      </div>
    </div>
  )
}
