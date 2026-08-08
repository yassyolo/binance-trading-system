import { GitCompareArrows } from 'lucide-react'

import { Card } from '@/components/ui/Card'
import { EmptyState } from '@/components/ui/EmptyState'
import { ErrorState } from '@/components/ui/ErrorState'
import { LoadingSkeleton } from '@/components/ui/LoadingSkeleton'
import {
  money,
  percent,
  pnlClass,
  score,
} from '@/features/optimization/optimization-formatters'
import { useRunComparison } from '@/features/optimization/optimization.queries'
import type { RunSummaryDto } from '@/types/optimization'

interface RunComparisonProps {
  runs: RunSummaryDto[]
  leftRunId: string
  rightRunId: string
  onLeftChange: (value: string) => void
  onRightChange: (value: string) => void
}

export function RunComparison({
  runs,
  leftRunId,
  rightRunId,
  onLeftChange,
  onRightChange,
}: RunComparisonProps) {
  const query = useRunComparison(
    leftRunId,
    rightRunId,
  )

  return (
    <Card className="p-5">
      <div className="flex items-start justify-between gap-6">
        <div>
          <div className="flex items-center gap-2">
            <GitCompareArrows
              size={17}
              className="text-[var(--color-text-secondary)]"
            />
            <h3 className="text-sm font-semibold">
              Run comparison
            </h3>
          </div>

          <p className="mt-1 text-xs text-[var(--color-text-muted)]">
            Compare persisted performance metrics between two runs.
          </p>
        </div>
      </div>

      <div className="mt-5 grid grid-cols-2 gap-3">
        <select
          value={leftRunId}
          onChange={(event) =>
            onLeftChange(event.target.value)
          }
          className={inputClass}
        >
          <option value="">
            Select left run
          </option>
          {runs.map((run) => (
            <option
              key={run.runId}
              value={run.runId}
            >
              {run.botName} · {run.symbol} · {run.runId}
            </option>
          ))}
        </select>

        <select
          value={rightRunId}
          onChange={(event) =>
            onRightChange(event.target.value)
          }
          className={inputClass}
        >
          <option value="">
            Select right run
          </option>
          {runs.map((run) => (
            <option
              key={run.runId}
              value={run.runId}
            >
              {run.botName} · {run.symbol} · {run.runId}
            </option>
          ))}
        </select>
      </div>

      {!leftRunId || !rightRunId ? (
        <div className="mt-5">
          <EmptyState
            title="Choose two runs"
            description="Select different runs to compare net profit, drawdown, win rate and score."
          />
        </div>
      ) : leftRunId === rightRunId ? (
        <div className="mt-5">
          <EmptyState
            title="Choose different runs"
            description="The left and right comparison runs must be different."
          />
        </div>
      ) : query.isPending ? (
        <div className="mt-5 grid grid-cols-4 gap-3">
          {Array.from(
            { length: 4 },
            (_, index) => (
              <LoadingSkeleton
                key={index}
                className="h-24"
              />
            ),
          )}
        </div>
      ) : query.isError ? (
        <div className="mt-5">
          <ErrorState
            title="Comparison unavailable"
            description={query.error.message}
          />
        </div>
      ) : query.data ? (
        <div className="mt-5 grid grid-cols-4 gap-3">
          <DifferenceCard
            label="Net profit Δ"
            value={money(
              query.data.netProfitDifference,
            )}
            valueClass={pnlClass(
              query.data.netProfitDifference,
            )}
          />
          <DifferenceCard
            label="Drawdown Δ"
            value={percent(
              query.data.drawdownDifference,
            )}
          />
          <DifferenceCard
            label="Win rate Δ"
            value={percent(
              query.data.winRateDifference,
            )}
          />
          <DifferenceCard
            label="Score Δ"
            value={score(
              query.data.scoreDifference,
            )}
          />
        </div>
      ) : null}
    </Card>
  )
}

function DifferenceCard({
  label,
  value,
  valueClass = '',
}: {
  label: string
  value: string
  valueClass?: string
}) {
  return (
    <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] p-4">
      <p className="text-[10px] uppercase tracking-[0.08em] text-[var(--color-text-muted)]">
        {label}
      </p>
      <p
        className={`mt-2 text-lg font-semibold ${valueClass}`}
      >
        {value}
      </p>
    </div>
  )
}

const inputClass =
  'h-10 w-full rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] px-3 text-xs text-[var(--color-text-primary)] outline-none focus:border-[#454954]'
