import {
  useEffect,
  useState,
} from 'react'
import { Search } from 'lucide-react'

import { PageHeader } from '@/components/layout/PageHeader'
import { Button } from '@/components/ui/Button'
import { PageSection } from '@/components/ui/PageSection'
import { CreateOptimizationForm } from '@/features/optimization/CreateOptimizationForm'
import { OptimizationJobPanel } from '@/features/optimization/OptimizationJobPanel'
import { OptimizationRuns } from '@/features/optimization/OptimizationRuns'
import { OptimizationTrials } from '@/features/optimization/OptimizationTrials'
import { RunComparison } from '@/features/optimization/RunComparison'
import type {
  JobAcceptedDto,
  RunSummaryDto,
} from '@/types/optimization'

export function OptimizationPage() {
  const [acceptedJob, setAcceptedJob] =
    useState<JobAcceptedDto | null>(null)

  const [botDraft, setBotDraft] =
    useState('')
  const [botName, setBotName] =
    useState<string | undefined>()
  const [skip, setSkip] = useState(0)
  const [take] = useState(50)

  const [selectedRunId, setSelectedRunId] =
    useState<string | null>(null)

  const [loadedRuns, setLoadedRuns] =
    useState<RunSummaryDto[]>([])

  const [leftRunId, setLeftRunId] =
    useState('')
  const [rightRunId, setRightRunId] =
    useState('')

  useEffect(() => {
    if (
      selectedRunId &&
      !loadedRuns.some(
        (run) =>
          run.runId === selectedRunId,
      )
    ) {
      setSelectedRunId(null)
    }
  }, [loadedRuns, selectedRunId])

  function applyBotFilter() {
    setBotName(
      botDraft.trim() || undefined,
    )
    setSkip(0)
    setSelectedRunId(null)
  }

  return (
    <>
      <PageHeader
        title="Optimization"
        description="Parameter search, ranking, walk-forward evaluation and run comparison."
      />

      <main className="space-y-10 p-8">
        <PageSection
          title="Create optimization"
          description="Grid candidates are queued for bounded background execution and persisted trial-by-trial."
        >
          <CreateOptimizationForm
            onAccepted={setAcceptedJob}
          />
        </PageSection>

        {acceptedJob && (
          <OptimizationJobPanel
            job={acceptedJob}
          />
        )}

        <PageSection
          title="Optimization runs"
          description="Select a persisted optimization run to inspect its ranked trials."
          actions={
            <div className="flex gap-2">
              <input
                value={botDraft}
                onChange={(event) =>
                  setBotDraft(
                    event.target.value,
                  )
                }
                onKeyDown={(event) => {
                  if (
                    event.key === 'Enter'
                  ) {
                    applyBotFilter()
                  }
                }}
                placeholder="Filter by bot"
                className="h-8 w-44 rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] px-3 text-xs outline-none focus:border-[#454954]"
              />

              <Button
                size="sm"
                leftIcon={
                  <Search size={14} />
                }
                onClick={applyBotFilter}
              >
                Filter
              </Button>
            </div>
          }
        >
          <OptimizationRuns
            botName={botName}
            skip={skip}
            take={take}
            selectedRunId={
              selectedRunId
            }
            onSelect={setSelectedRunId}
            onRunsLoaded={setLoadedRuns}
            onPrevious={() =>
              setSkip((current) =>
                Math.max(
                  0,
                  current - take,
                ),
              )
            }
            onNext={() =>
              setSkip(
                (current) =>
                  current + take,
              )
            }
          />
        </PageSection>

        <PageSection
          title="Trials"
          description={
            selectedRunId
              ? `Top persisted parameter trials for ${selectedRunId}`
              : 'Select an optimization run first.'
          }
        >
          <OptimizationTrials
            runId={selectedRunId}
          />
        </PageSection>

        <PageSection
          title="Compare runs"
          description="The backend returns metric differences between two persisted runs."
        >
          <RunComparison
            runs={loadedRuns}
            leftRunId={leftRunId}
            rightRunId={rightRunId}
            onLeftChange={setLeftRunId}
            onRightChange={
              setRightRunId
            }
          />
        </PageSection>
      </main>
    </>
  )
}
