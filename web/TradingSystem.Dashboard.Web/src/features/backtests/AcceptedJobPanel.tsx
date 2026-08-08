import { Clock3, Copy, FlaskConical } from 'lucide-react'

import { Card } from '@/components/ui/Card'
import { StatusBadge } from '@/components/ui/StatusBadge'
import { utcDate } from '@/features/backtests/backtest-formatters'
import type { JobAcceptedDto } from '@/types/backtesting'

export function AcceptedJobPanel({
  job,
}: {
  job: JobAcceptedDto
}) {
  async function copyId() {
    await navigator.clipboard.writeText(job.jobId)
  }

  return (
    <Card className="border-[rgba(96,165,250,0.18)] bg-[rgba(96,165,250,0.04)] p-5">
      <div className="flex items-start gap-4">
        <div className="grid size-10 shrink-0 place-items-center rounded-xl border border-[rgba(96,165,250,0.18)] bg-[rgba(96,165,250,0.06)] text-[var(--color-info)]">
          <FlaskConical size={18} />
        </div>

        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-3">
            <h3 className="text-sm font-semibold">
              Backtest accepted
            </h3>
            <StatusBadge
              label={job.status}
              tone="info"
            />
          </div>

          <p className="mt-2 text-xs leading-5 text-[var(--color-text-secondary)]">
            The Dashboard API returned HTTP 202. TradingSystem.Jobs.Worker will claim the durable job and persist the resulting run.
          </p>

          <div className="mt-4 flex items-center gap-6 text-xs text-[var(--color-text-muted)]">
            <button
              type="button"
              onClick={() => void copyId()}
              className="inline-flex items-center gap-2 font-mono transition hover:text-[var(--color-text-primary)]"
              title="Copy job id"
            >
              <Copy size={13} />
              {job.jobId}
            </button>

            <span className="inline-flex items-center gap-2">
              <Clock3 size={13} />
              {utcDate(job.createdAtUtc)} UTC
            </span>
          </div>

          <div className="mt-4 rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] px-4 py-3 text-[11px] leading-5 text-[var(--color-text-muted)]">
            Live job percentage/stage is not exposed by the current Dashboard API yet. The runs table below polls every 5 seconds and will show the persisted run as soon as it is available.
          </div>
        </div>
      </div>
    </Card>
  )
}
