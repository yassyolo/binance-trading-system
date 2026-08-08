import { RefreshCcw } from 'lucide-react'

import { Button } from '@/components/ui/Button'
import { DataTable, type DataTableColumn } from '@/components/ui/DataTable'
import { EmptyState } from '@/components/ui/EmptyState'
import { ErrorState } from '@/components/ui/ErrorState'
import { LoadingSkeleton } from '@/components/ui/LoadingSkeleton'
import { StatusBadge } from '@/components/ui/StatusBadge'
import {
  commandLabel,
  commandStatusLabel,
  commandStatusTone,
} from '@/features/bots/bot-enums'
import { useBotCommands } from '@/features/bots/bots.queries'
import type { BotCommandDto } from '@/types/bots'

const columns: DataTableColumn<BotCommandDto>[] = [
  {
    key: 'command',
    header: 'Command',
    render: (command) => (
      <span className="font-medium text-[var(--color-text-primary)]">
        {commandLabel(command.command)}
      </span>
    ),
  },
  {
    key: 'status',
    header: 'Status',
    render: (command) => (
      <StatusBadge
        label={commandStatusLabel(command.status)}
        tone={commandStatusTone(command.status)}
      />
    ),
  },
  {
    key: 'requestedBy',
    header: 'Requested by',
    render: (command) => command.requestedBy,
  },
  {
    key: 'reason',
    header: 'Reason',
    render: (command) => (
      <span className="block max-w-[300px] truncate" title={command.reason}>
        {command.reason}
      </span>
    ),
  },
  {
    key: 'requestedAt',
    header: 'Requested',
    render: (command) => formatDate(command.requestedAtUtc),
  },
  {
    key: 'error',
    header: 'Error',
    render: (command) => (
      <span className={command.error ? 'text-[var(--color-danger)]' : ''}>
        {command.error ?? '—'}
      </span>
    ),
  },
]

export function BotCommandHistory({ botName }: { botName: string }) {
  const query = useBotCommands(botName)

  if (query.isPending) {
    return (
      <div className="space-y-3">
        <LoadingSkeleton className="h-12 w-full" />
        <LoadingSkeleton className="h-12 w-full" />
        <LoadingSkeleton className="h-12 w-full" />
      </div>
    )
  }

  if (query.isError) {
    return (
      <ErrorState
        title="Command history unavailable"
        description={query.error.message}
        action={<Button onClick={() => void query.refetch()}>Retry</Button>}
      />
    )
  }

  if (!query.data.length) {
    return (
      <EmptyState
        title="No commands yet"
        description="Runtime commands for this bot will appear here after they are submitted."
      />
    )
  }

  return (
    <div>
      <div className="mb-3 flex justify-end">
        <Button
          size="sm"
          leftIcon={<RefreshCcw className={query.isFetching ? 'animate-spin' : ''} size={14} />}
          onClick={() => void query.refetch()}
        >
          Refresh
        </Button>
      </div>

      <DataTable
        columns={columns}
        rows={query.data}
        rowKey={(command) => command.commandId}
      />
    </div>
  )
}

function formatDate(value: string) {
  const date = new Date(value)

  if (Number.isNaN(date.getTime())) return value

  return new Intl.DateTimeFormat('en-GB', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  }).format(date)
}
