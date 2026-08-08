import { useState } from 'react'
import { Search } from 'lucide-react'

import { PageHeader } from '@/components/layout/PageHeader'
import { Button } from '@/components/ui/Button'
import { PageSection } from '@/components/ui/PageSection'
import { AcceptedJobPanel } from '@/features/backtests/AcceptedJobPanel'
import { BacktestRunsTable } from '@/features/backtests/BacktestRunsTable'
import { CreateBacktestForm } from '@/features/backtests/CreateBacktestForm'
import type {
  JobAcceptedDto,
  RunsQuery,
} from '@/types/backtesting'

export function BacktestsPage() {
  const [acceptedJob, setAcceptedJob] =
    useState<JobAcceptedDto | null>(null)

  const [botDraft, setBotDraft] = useState('')
  const [runsQuery, setRunsQuery] = useState<RunsQuery>({
    skip: 0,
    take: 50,
  })

  function applyBotFilter() {
    setRunsQuery((current) => ({
      ...current,
      skip: 0,
      botName: botDraft.trim() || undefined,
    }))
  }

  return (
    <>
      <PageHeader
        title="Backtests"
        description="Queue historical simulations and inspect persisted strategy performance."
      />

      <main className="space-y-10 p-8">
        <PageSection
          title="Create"
          description="Backtests run outside the HTTP request and never execute live Binance orders."
        >
          <CreateBacktestForm
            onAccepted={setAcceptedJob}
          />
        </PageSection>

        {acceptedJob && (
          <AcceptedJobPanel job={acceptedJob} />
        )}

        <PageSection
          title="Backtest runs"
          description="Persisted performance runs returned by GET /runs."
          actions={
            <div className="flex gap-2">
              <input
                value={botDraft}
                onChange={(event) =>
                  setBotDraft(event.target.value)
                }
                onKeyDown={(event) => {
                  if (event.key === 'Enter')
                    applyBotFilter()
                }}
                placeholder="Filter by bot"
                className="h-8 w-44 rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] px-3 text-xs outline-none focus:border-[#454954]"
              />

              <Button
                size="sm"
                leftIcon={<Search size={14} />}
                onClick={applyBotFilter}
              >
                Filter
              </Button>
            </div>
          }
        >
          <BacktestRunsTable
            query={runsQuery}
            onPrevious={() =>
              setRunsQuery((current) => ({
                ...current,
                skip: Math.max(
                  0,
                  current.skip - current.take,
                ),
              }))
            }
            onNext={() =>
              setRunsQuery((current) => ({
                ...current,
                skip: current.skip + current.take,
              }))
            }
          />
        </PageSection>
      </main>
    </>
  )
}
