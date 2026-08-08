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
  decimal,
  money,
  pnlClass,
  sideTone,
  utcDate,
} from '@/features/paper/paper-formatters'
import { usePaperTrades } from '@/features/paper/paper.queries'
import type { PaperTradingPositionDto } from '@/types/paper-trading'

function sideLabel(
  side: PaperTradingPositionDto['side'],
) {
  if (side === 1) return 'Long'
  if (side === 2) return 'Short'
  return String(side)
}

const columns:
  DataTableColumn<PaperTradingPositionDto>[] =
  [
    {
      key: 'position',
      header: 'Position',
      render: (position) => (
        <div className="max-w-[180px]">
          <p
            className="truncate font-mono text-xs text-[var(--color-text-primary)]"
            title={position.positionId}
          >
            {position.shortId || position.positionId}
          </p>

          <p className="mt-1 text-xs text-[var(--color-text-muted)]">
            {position.botName}
          </p>
        </div>
      ),
    },
    {
      key: 'symbol',
      header: 'Symbol',
      render: (position) => position.symbol,
    },
    {
      key: 'side',
      header: 'Side',
      render: (position) => {
        const label = sideLabel(position.side)

        return (
          <StatusBadge
            label={label}
            tone={sideTone(label)}
          />
        )
      },
    },
    {
      key: 'quantity',
      header: 'Quantity',
      align: 'right',
      render: (position) =>
        decimal(position.quantity, 8),
    },
    {
      key: 'entry',
      header: 'Entry',
      align: 'right',
      render: (position) =>
        decimal(position.entryPrice, 4),
    },
    {
      key: 'exit',
      header: 'Exit',
      align: 'right',
      render: (position) =>
        decimal(position.exitPrice, 4),
    },
    {
      key: 'pnl',
      header: 'Realized PnL',
      align: 'right',
      render: (position) => {
        const pnl = position.realizedPnl ?? 0

        return (
          <span className={pnlClass(pnl)}>
            {money(pnl)}
          </span>
        )
      },
    },
    {
      key: 'fees',
      header: 'Fees',
      align: 'right',
      render: (position) =>
        money(
          position.entryFee +
            (position.exitFee ?? 0),
        ),
    },
    {
      key: 'reason',
      header: 'Close reason',
      render: (position) =>
        position.closeReason ?? '—',
    },
    {
      key: 'closed',
      header: 'Closed UTC',
      render: (position) => (
        <span className="whitespace-nowrap text-xs">
          {utcDate(position.closedAtUtc)}
        </span>
      ),
    },
  ]

export function PaperTradesTable({
  botName,
}: {
  botName?: string
}) {
  const query = usePaperTrades(botName, 100)

  if (query.isPending) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 6 }, (_, index) => (
          <LoadingSkeleton
            key={index}
            className="h-14"
          />
        ))}
      </div>
    )
  }

  if (query.isError) {
    return (
      <ErrorState
        title="Closed paper positions unavailable"
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
        title="No closed paper positions"
        description="Completed simulated positions will appear here."
      />
    )
  }

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
        <div className="min-w-[1250px]">
          <DataTable
            columns={columns}
            rows={query.data}
            rowKey={(position) => position.positionId}
          />
        </div>
      </div>
    </div>
  )
}
