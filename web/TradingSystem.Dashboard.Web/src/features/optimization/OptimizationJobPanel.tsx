import {
  Clock3,
  Copy,
  SlidersHorizontal,
} from 'lucide-react'

import { Card } from '@/components/ui/Card'
import { StatusBadge } from '@/components/ui/StatusBadge'
import { utcDate } from '@/features/optimization/optimization-formatters'
import type { JobAcceptedDto } from '@/types/optimization'

export function OptimizationJobPanel({
  job,
}: {
  job: JobAcceptedDto
}) {
  async function copyJobId() {
    await navigator.clipboard.writeText(
      job.jobId,
    )
  }

  return (
    <Card className="border-[rgba(96,165,250,0.18)] bg-[rgba(96,165,250,0.04)] p-5">
      <div className="flex items-start gap-4">
        <div className="grid size-10 shrink-0 place-items-center rounded-xl border border-[rgba(96,165,250,0.18)] bg-[rgba(96,165,250,0.06)] text-[var(--color-info)]">
          <SlidersHorizontal size={18} />
        </div>

        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-3">
            <h3 className="text-sm font-semibold">
              Optimization accepted
            </h3>
            <StatusBadge
              label={job.status}
              tone="info"
            />
          </div>

          <p className="mt-2 text-xs leading-5 text-[var(--color-text-secondary)]">
            The durable job is queued for TradingSystem.Jobs.Worker. Optimization trials are persisted as they are produced.
          </p>

          <div className="mt-4 flex items-center gap-6 text-xs text-[var(--color-text-muted)]">
            <button
              type="button"
              onClick={() =>
                void copyJobId()
              }
              className="inline-flex items-center gap-2 font-mono transition hover:text-[var(--color-text-primary)]"
            >
              <Copy size={13} />
              {job.jobId}
            </button>

            <span className="inline-flex items-center gap-2">
              <Clock3 size={13} />
              {utcDate(job.createdAtUtc)} UTC
            </span>
          </div>
        </div>
      </div>
    </Card>
  )
}
