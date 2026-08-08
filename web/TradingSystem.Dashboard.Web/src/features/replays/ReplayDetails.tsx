import {
  useState,
} from 'react'
import {
  Ban,
  CheckCircle2,
  Clock3,
  Copy,
  Fingerprint,
  Layers3,
  RefreshCcw,
  TriangleAlert,
} from 'lucide-react'

import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { ConfirmActionDialog } from '@/components/ui/ConfirmActionDialog'
import {
  DataTable,
  type DataTableColumn,
} from '@/components/ui/DataTable'
import { EmptyState } from '@/components/ui/EmptyState'
import { ErrorState } from '@/components/ui/ErrorState'
import { LoadingSkeleton } from '@/components/ui/LoadingSkeleton'
import { MetricCard } from '@/components/ui/MetricCard'
import { ProgressBar } from '@/components/ui/ProgressBar'
import { StatusBadge } from '@/components/ui/StatusBadge'

import {
  integer,
  replayModeLabel,
  replayStatusLabel,
  replayStatusTone,
  utcDate,
} from '@/features/replays/replay-formatters'
import {
  useCancelReplay,
  useReplay,
} from '@/features/replays/replays.queries'
import type {
  ReplayCheckpointDto,
  ReplayStepDto,
} from '@/types/replays'

const checkpointColumns:
  DataTableColumn<ReplayCheckpointDto>[] =
  [
    {
      key: 'time',
      header: 'Time UTC',
      render: (checkpoint) => (
        <span className="whitespace-nowrap text-xs">
          {utcDate(
            checkpoint.timeUtc,
          )}
        </span>
      ),
    },
    {
      key: 'stage',
      header: 'Stage',
      render: (checkpoint) =>
        checkpoint.stage ?? '—',
    },
    {
      key: 'processed',
      header: 'Processed',
      align: 'right',
      render: (checkpoint) =>
        integer(
          checkpoint.processedEvents,
        ),
    },
    {
      key: 'total',
      header: 'Total',
      align: 'right',
      render: (checkpoint) =>
        checkpoint.totalEvents ===
        undefined
          ? '—'
          : integer(
              checkpoint.totalEvents,
            ),
    },
    {
      key: 'message',
      header: 'Message',
      render: (checkpoint) =>
        checkpoint.message ?? '—',
    },
  ]

const stepColumns:
  DataTableColumn<ReplayStepDto>[] =
  [
    {
      key: 'position',
      header: 'Position',
      align: 'right',
      render: (step) =>
        integer(
          step.globalPosition,
        ),
    },
    {
      key: 'time',
      header: 'Virtual time UTC',
      render: (step) => (
        <span className="whitespace-nowrap text-xs">
          {utcDate(
            step.virtualTimeUtc ??
              step.timeUtc,
          )}
        </span>
      ),
    },
    {
      key: 'eventType',
      header: 'Event type',
      render: (step) => (
        <StatusBadge
          label={
            step.eventType ??
            step.kind ??
            'Unknown'
          }
          tone={
            step.succeeded
              ? 'neutral'
              : 'danger'
          }
          showDot={false}
        />
      ),
    },
    {
      key: 'event',
      header: 'Source event',
      render: (step) => (
        <span
          className="block max-w-[220px] truncate font-mono text-xs"
          title={
            step.sourceEventId
          }
        >
          {step.sourceEventId ??
            '—'}
        </span>
      ),
    },
    {
      key: 'result',
      header: 'Result',
      render: (step) => (
        <span
          className="block max-w-[420px] truncate font-mono text-[11px]"
          title={
            step.error ??
            step.resultJson
          }
        >
          {step.error ??
            step.resultJson ??
            '—'}
        </span>
      ),
    },
  ]

export function ReplayDetails({
  replayId,
}: {
  replayId:
    | string
    | null
}) {
  const [
    cancelOpen,
    setCancelOpen,
  ] = useState(false)

  const query =
    useReplay(replayId)

  const cancel =
    useCancelReplay(
      replayId ?? '',
    )

  if (!replayId) {
    return (
      <EmptyState
        title="Select a replay"
        description="Choose a replay from the list to inspect its persisted execution state."
      />
    )
  }

  if (query.isPending) {
    return (
      <div className="space-y-4">
        <LoadingSkeleton className="h-28 w-full" />
        <LoadingSkeleton className="h-72 w-full" />
      </div>
    )
  }

  if (query.isError) {
    return (
      <ErrorState
        title="Replay details unavailable"
        description={
          query.error.message
        }
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

  const replay = query.data
  const status =
    replayStatusLabel(
      replay.status,
    )

  const canCancel =
    status === 'Pending' ||
    status === 'Processing'

  const checkpoints =
    replay.checkpoints ?? []

  const steps =
    replay.steps ?? []

  const result =
    replay.result ?? null

  async function copyId() {
    await navigator.clipboard.writeText(
      replay.replayId,
    )
  }

  return (
    <div className="space-y-5">
      <Card className="p-6">
        <div className="flex items-start justify-between gap-6">
          <div>
            <div className="flex items-center gap-3">
              <h3 className="text-base font-semibold">
                {replay.name}
              </h3>

              <StatusBadge
                label={status}
                tone={
                  replayStatusTone(
                    replay.status,
                  )
                }
              />

              <StatusBadge
                label={
                  replayModeLabel(
                    replay.mode,
                  )
                }
                tone="neutral"
                showDot={false}
              />
            </div>

            <p className="mt-1 text-xs text-[var(--color-text-muted)]">
              {replay.request
                ?.botName ??
                'All bots'}
              {' · '}
              {replay.request
                ?.symbol ??
                'All symbols'}
            </p>

            <button
              type="button"
              onClick={() =>
                void copyId()
              }
              className="mt-3 inline-flex items-center gap-2 font-mono text-[11px] text-[var(--color-text-muted)] transition hover:text-[var(--color-text-primary)]"
            >
              <Copy size={12} />
              {replay.replayId}
            </button>
          </div>

          <div className="flex gap-2">
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

            {canCancel && (
              <Button
                size="sm"
                variant="danger"
                leftIcon={
                  <Ban size={14} />
                }
                onClick={() =>
                  setCancelOpen(
                    true,
                  )
                }
              >
                Cancel
              </Button>
            )}
          </div>
        </div>

        <div className="mt-6">
          <div className="mb-2 flex items-center justify-between text-xs">
            <span className="text-[var(--color-text-secondary)]">
              {replay.progressStage ??
                'Waiting'}
            </span>

            <span className="font-mono text-[var(--color-text-muted)]">
              {replay.progressPercent}%
            </span>
          </div>

          <ProgressBar
            value={
              Number.isFinite(
                replay.progressPercent,
              )
                ? Math.min(
                    100,
                    Math.max(
                      0,
                      replay.progressPercent,
                    ),
                  )
                : 0
            }
          />

          <div className="mt-2 flex gap-5 text-[10px] text-[var(--color-text-muted)]">
            <span>
              {integer(
                replay.processedEvents,
              )}{' '}
              processed
            </span>
            <span>
              {integer(
                replay.failedEvents,
              )}{' '}
              failed
            </span>
            <span>
              Last global position{' '}
              {integer(
                replay.lastGlobalPosition,
              )}
            </span>
          </div>
        </div>

        {replay.error && (
          <div className="mt-5 flex gap-3 rounded-xl border border-[rgba(248,113,113,0.18)] bg-[rgba(248,113,113,0.05)] px-4 py-3 text-xs text-[var(--color-danger)]">
            <TriangleAlert
              size={15}
              className="mt-0.5 shrink-0"
            />
            {replay.error}
          </div>
        )}
      </Card>

      <div className="grid grid-cols-4 gap-4">
        <MetricCard
          label="Processed events"
          value={integer(
            replay.processedEvents,
          )}
          helper={`${integer(
            replay.failedEvents,
          )} failed`}
          icon={
            <Layers3 size={18} />
          }
        />

        <MetricCard
          label="Started"
          value={
            replay.startedAtUtc
              ? utcDate(
                  replay.startedAtUtc,
                )
              : 'Not started'
          }
          helper="UTC"
          icon={
            <Clock3 size={18} />
          }
        />

        <MetricCard
          label="Completed"
          value={
            replay.completedAtUtc
              ? utcDate(
                  replay.completedAtUtc,
                )
              : '—'
          }
          helper="UTC"
          icon={
            <CheckCircle2
              size={18}
            />
          }
        />

        <MetricCard
          label="Deterministic hash"
          value={
            replay.deterministicHash
              ? 'Available'
              : '—'
          }
          helper={
            replay.deterministicHash ??
            'Not produced yet'
          }
          icon={
            <Fingerprint
              size={18}
            />
          }
        />
      </div>

      {result && (
        <Card className="p-5">
          <h4 className="text-sm font-semibold">
            Replay result
          </h4>

          <div className="mt-5 grid grid-cols-4 gap-4 text-xs">
            <ResultItem
              label="Signals"
              value={integer(
                result.signals,
              )}
            />
            <ResultItem
              label="Strategy decisions"
              value={integer(
                result.strategyDecisions,
              )}
            />
            <ResultItem
              label="Risk allowed"
              value={integer(
                result.riskAllowed,
              )}
            />
            <ResultItem
              label="Risk blocked"
              value={integer(
                result.riskBlocked,
              )}
            />
            <ResultItem
              label="Executions completed"
              value={integer(
                result.executionsCompleted,
              )}
            />
            <ResultItem
              label="Executions failed"
              value={integer(
                result.executionsFailed,
              )}
            />
            <ResultItem
              label="Positions opened"
              value={integer(
                result.positionsOpened,
              )}
            />
            <ResultItem
              label="Positions closed"
              value={integer(
                result.positionsClosed,
              )}
            />
          </div>
        </Card>
      )}

      {checkpoints.length > 0 && (
        <Card className="p-5">
          <h4 className="text-sm font-semibold">
            Checkpoints
          </h4>

          <div className="mt-5">
            <DataTable
              columns={
                checkpointColumns
              }
              rows={checkpoints}
              rowKey={(
                checkpoint,
              ) =>
                checkpoint.checkpointId ??
                `${checkpoint.replayId}-${checkpoint.processedEvents}-${checkpoint.timeUtc ?? ''}`
              }
            />
          </div>
        </Card>
      )}

      <Card className="p-5">
        <h4 className="text-sm font-semibold">
          Replay steps
        </h4>

        <p className="mt-1 text-xs text-[var(--color-text-muted)]">
          Persisted replay events, when returned by the details endpoint.
        </p>

        <div className="mt-5 overflow-x-auto">
          {steps.length === 0 ? (
            <p className="text-xs text-[var(--color-text-muted)]">
              No replay steps returned by this endpoint.
            </p>
          ) : (
            <div className="min-w-[1100px]">
              <DataTable
                columns={
                  stepColumns
                }
                rows={steps}
                rowKey={(
                  step,
                ) =>
                  step.stepId ??
                  `${step.replayId}-${step.globalPosition}-${step.sourceEventId}`
                }
              />
            </div>
          )}
        </div>
      </Card>

      <ConfirmActionDialog
        open={cancelOpen}
        title="Cancel replay?"
        description="The replay worker will stop processing this replay at its next cancellation boundary."
        confirmLabel="Cancel replay"
        dangerous
        busy={
          cancel.isPending
        }
        onClose={() =>
          setCancelOpen(false)
        }
        onConfirm={() =>
          cancel.mutate(
            undefined,
            {
              onSuccess: () =>
                setCancelOpen(
                  false,
                ),
            },
          )
        }
      />
    </div>
  )
}

function ResultItem({
  label,
  value,
}: {
  label: string
  value: string
}) {
  return (
    <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] p-4">
      <p className="text-[10px] uppercase tracking-[0.08em] text-[var(--color-text-muted)]">
        {label}
      </p>
      <p className="mt-2 font-mono text-sm font-semibold">
        {value}
      </p>
    </div>
  )
}
