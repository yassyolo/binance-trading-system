import {
  useState,
} from 'react'
import {
  Search,
} from 'lucide-react'

import { PageHeader } from '@/components/layout/PageHeader'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { PageSection } from '@/components/ui/PageSection'

import { CreateReplayForm } from '@/features/replays/CreateReplayForm'
import { ReplayDetails } from '@/features/replays/ReplayDetails'
import { ReplayList } from '@/features/replays/ReplayList'

import type {
  ReplayAcceptedDto,
} from '@/types/replays'

export function ReplaysPage() {
  const [
    acceptedReplay,
    setAcceptedReplay,
  ] =
    useState<ReplayAcceptedDto | null>(
      null,
    )

  const [
    botDraft,
    setBotDraft,
  ] = useState('')

  const [
    botName,
    setBotName,
  ] =
    useState<string | undefined>()

  const [skip, setSkip] =
    useState(0)

  const [take] =
    useState(50)

  const [
    selectedReplayId,
    setSelectedReplayId,
  ] =
    useState<string | null>(null)

  function applyFilter() {
    setBotName(
      botDraft.trim() ||
        undefined,
    )

    setSkip(0)
  }

  function accepted(
    replay:
      ReplayAcceptedDto,
  ) {
    setAcceptedReplay(
      replay,
    )

    setSelectedReplayId(
      replay.replayId,
    )
  }

  return (
    <>
      <PageHeader
        title="Replays"
        description="Replay historical events through the persisted replay engine and inspect deterministic results."
      />

      <main className="space-y-10 p-8">
        <PageSection
          title="Create replay"
          description="Historical replay is isolated from live execution."
        >
          <CreateReplayForm
            onAccepted={
              accepted
            }
          />
        </PageSection>

        {acceptedReplay && (
          <Card className="border-[rgba(96,165,250,0.18)] bg-[rgba(96,165,250,0.04)] p-5">
            <div>
              <h3 className="text-sm font-semibold">
                Replay accepted
              </h3>

              <p className="mt-2 text-xs text-[var(--color-text-muted)]">
                The replay job was queued successfully.
              </p>

              <p className="mt-2 font-mono text-xs text-[var(--color-text-secondary)]">
                {
                  acceptedReplay.replayId
                }
              </p>
            </div>
          </Card>
        )}

        <PageSection
          title="Replay history"
          description="Running replays are refreshed automatically."
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
                    event.key ===
                    'Enter'
                  ) {
                    applyFilter()
                  }
                }}
                placeholder="Bot hint"
                className="h-8 w-44 rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] px-3 text-xs outline-none focus:border-[#454954]"
              />

              <Button
                size="sm"
                leftIcon={
                  <Search size={14} />
                }
                onClick={
                  applyFilter
                }
              >
                Filter
              </Button>
            </div>
          }
        >
          <ReplayList
            botName={botName}
            skip={skip}
            take={take}
            selectedReplayId={
              selectedReplayId
            }
            onSelect={
              setSelectedReplayId
            }
            onPrevious={() =>
              setSkip(
                (current) =>
                  Math.max(
                    0,
                    current -
                      take,
                  ),
              )
            }
            onNext={() =>
              setSkip(
                (current) =>
                  current +
                  take,
              )
            }
          />
        </PageSection>

        <PageSection
          title="Replay details"
          description={
            selectedReplayId
              ? `Replay ${selectedReplayId}`
              : 'Select a replay first.'
          }
        >
          <ReplayDetails
            replayId={
              selectedReplayId
            }
          />
        </PageSection>
      </main>
    </>
  )
}
