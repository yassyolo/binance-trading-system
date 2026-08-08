import { RefreshCcw } from 'lucide-react'

import { Button } from '@/components/ui/Button'
import {
  DataTable,
  type DataTableColumn,
} from '@/components/ui/DataTable'
import { EmptyState } from '@/components/ui/EmptyState'
import { ErrorState } from '@/components/ui/ErrorState'
import { LoadingSkeleton } from '@/components/ui/LoadingSkeleton'
import { ProgressBar } from '@/components/ui/ProgressBar'
import { StatusBadge } from '@/components/ui/StatusBadge'

import {
  integer,
  replayModeLabel,
  replayStatusLabel,
  replayStatusTone,
  utcDate,
} from '@/features/replays/replay-formatters'
import { useReplays } from '@/features/replays/replays.queries'
import type { ReplaySummaryDto } from '@/types/replays'

interface ReplayListProps {
  botName?: string
  skip: number
  take: number
  selectedReplayId: string | null
  onSelect: (replayId: string) => void
  onPrevious: () => void
  onNext: () => void
}

export function ReplayList({
  botName: _botName,
  skip,
  take,
  selectedReplayId,
  onSelect,
  onPrevious,
  onNext,
}: ReplayListProps) {
  // Current backend list endpoint is status/skip/take based.
  // `botName` is retained only for compatibility with the existing page.
  const query = useReplays(undefined, skip, take)

  if (query.isPending) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 6 }, (_, index) => (
          <LoadingSkeleton
            key={index}
            className="h-14 w-full"
          />
        ))}
      </div>
    )
  }

  if (query.isError) {
    return (
      <ErrorState
        title="Replays unavailable"
        description={query.error.message}
        action={
          <Button onClick={() => void query.refetch()}>
            Retry
          </Button>
        }
      />
    )
  }

  if (!query.data.length) {
    return (
      <EmptyState
        title="No replays"
        description="Replay jobs will appear here after they are queued."
      />
    )
  }

  const columns: DataTableColumn<ReplaySummaryDto>[] = [
    {
      key: 'replay',
      header: 'Replay',
      render: (replay) => (
        <button
          type="button"
          onClick={() => onSelect(replay.replayId)}
          className="max-w-[230px] text-left"
        >
          <p
            className={`truncate text-sm font-medium ${
              selectedReplayId === replay.replayId
                ? 'text-[var(--color-info)]'
                : 'text-[var(--color-text-primary)]'
            }`}
            title={replay.name}
          >
            {replay.name}
          </p>
          <p
            className="mt-1 truncate font-mono text-[10px] text-[var(--color-text-muted)]"
            title={replay.replayId}
          >
            {replay.replayId}
          </p>
        </button>
      ),
    },
    {
      key: 'market',
      header: 'Market',
      render: (replay) => (
        <div>
          <p>{replay.request?.symbol ?? '—'}</p>
          <p className="mt-1 text-xs text-[var(--color-text-muted)]">
            {replay.request?.botName ?? 'All bots'}
          </p>
        </div>
      ),
    },
    {
      key: 'mode',
      header: 'Mode',
      render: (replay) => (
        <span className="text-xs">
          {replayModeLabel(replay.mode)}
        </span>
      ),
    },
    {
      key: 'status',
      header: 'Status',
      render: (replay) => (
        <StatusBadge
          label={replayStatusLabel(replay.status)}
          tone={replayStatusTone(replay.status)}
        />
      ),
    },
    {
      key: 'progress',
      header: 'Progress',
      width: '220px',
      render: (replay) => {
        const progress = Number.isFinite(replay.progressPercent)
          ? Math.min(100, Math.max(0, replay.progressPercent))
          : 0

        return (
          <div>
            <ProgressBar value={progress} />
            <div className="mt-1.5 flex justify-between gap-3 text-[10px] text-[var(--color-text-muted)]">
              <span>{progress}%</span>
              <span>{integer(replay.processedEvents)} processed</span>
            </div>
          </div>
        )
      },
    },
    {
      key: 'failed',
      header: 'Failed',
      render: (replay) => (
        <span className="font-mono text-xs">
          {integer(replay.failedEvents)}
        </span>
      ),
    },
    {
      key: 'stage',
      header: 'Stage',
      render: (replay) => (
        <span
          className="block max-w-[180px] truncate text-xs"
          title={replay.progressStage ?? undefined}
        >
          {replay.progressStage ?? '—'}
        </span>
      ),
    },
    {
      key: 'started',
      header: 'Started UTC',
      render: (replay) => (
        <span className="whitespace-nowrap text-xs">
          {utcDate(replay.startedAtUtc ?? replay.createdAtUtc)}
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
              className={query.isFetching ? 'animate-spin' : ''}
            />
          }
          onClick={() => void query.refetch()}
        >
          Refresh
        </Button>
      </div>

      <div className="overflow-x-auto">
        <div className="min-w-[1240px]">
          <DataTable
            columns={columns}
            rows={query.data}
            rowKey={(replay) => replay.replayId}
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
            disabled={skip === 0 || query.isFetching}
          >
            Previous
          </Button>
          <Button
            size="sm"
            onClick={onNext}
            disabled={query.isFetching || query.data.length < take}
          >
            Next
          </Button>
        </div>
      </div>
    </div>
  )
}
