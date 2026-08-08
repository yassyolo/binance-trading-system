import { RefreshCcw } from 'lucide-react'

import { PageHeader } from '@/components/layout/PageHeader'
import { Button } from '@/components/ui/Button'
import { DataTable, type DataTableColumn } from '@/components/ui/DataTable'
import { EmptyState } from '@/components/ui/EmptyState'
import { ErrorState } from '@/components/ui/ErrorState'
import { LoadingSkeleton } from '@/components/ui/LoadingSkeleton'
import { PageSection } from '@/components/ui/PageSection'
import { StatusBadge } from '@/components/ui/StatusBadge'
import { HistoryFilters } from '@/features/history/HistoryFilters'
import { HistoryPagination } from '@/features/history/HistoryPagination'
import {
  decimal,
  money,
  pnlClass,
  positionStatusTone,
  sideTone,
  utcDate,
} from '@/features/history/history-formatters'
import { usePositions } from '@/features/history/history.queries'
import { useHistoryFilterState } from '@/features/history/useHistoryFilterState'
import type { PositionRowDto } from '@/types/trading-history'

const columns: DataTableColumn<PositionRowDto>[] = [
  {
    key: 'position',
    header: 'Position',
    render: (row) => (
      <div className="max-w-[180px]">
        <p className="truncate font-mono text-xs text-[var(--color-text-primary)]" title={row.positionId}>
          {row.positionId}
        </p>
        <p className="mt-1 text-xs text-[var(--color-text-muted)]">{row.botName}</p>
      </div>
    ),
  },
  {
    key: 'symbol',
    header: 'Symbol',
    render: (row) => row.symbol,
  },
  {
    key: 'side',
    header: 'Side',
    render: (row) => <StatusBadge label={row.side} tone={sideTone(row.side)} />,
  },
  {
    key: 'status',
    header: 'Status',
    render: (row) => <StatusBadge label={row.status} tone={positionStatusTone(row.status)} />,
  },
  {
    key: 'quantity',
    header: 'Quantity',
    align: 'right',
    render: (row) => decimal(row.quantity),
  },
  {
    key: 'entry',
    header: 'Entry',
    align: 'right',
    render: (row) => decimal(row.entryPrice, 4),
  },
  {
    key: 'tp',
    header: 'Take Profit',
    align: 'right',
    render: (row) => decimal(row.takeProfitPrice, 4),
  },
  {
    key: 'current',
    header: 'Current',
    align: 'right',
    render: (row) => decimal(row.currentPrice, 4),
  },
  {
    key: 'unrealized',
    header: 'Unrealized PnL',
    align: 'right',
    render: (row) => <span className={pnlClass(row.unrealizedPnl)}>{money(row.unrealizedPnl)}</span>,
  },
  {
    key: 'realized',
    header: 'Realized PnL',
    align: 'right',
    render: (row) => <span className={pnlClass(row.realizedPnl)}>{money(row.realizedPnl)}</span>,
  },
  {
    key: 'opened',
    header: 'Opened UTC',
    render: (row) => <span className="whitespace-nowrap text-xs">{utcDate(row.openedAtUtc)}</span>,
  },
]

export function PositionsPage() {
  const filters = useHistoryFilterState(50, { status: true })
  const query = usePositions(filters.query)

  return (
    <>
      <PageHeader
        title="Positions"
        description="Current and historical exposure across trading bots."
      />

      <main className="space-y-6 p-8">
        <HistoryFilters
          values={filters.draft}
          showStatus
          onChange={filters.setDraft}
          onApply={filters.apply}
          onReset={filters.reset}
        />

        <PageSection
          title="Position history"
          description="Filter by bot, symbol or persisted lifecycle status."
          actions={
            <Button
              size="sm"
              leftIcon={<RefreshCcw className={query.isFetching ? 'animate-spin' : ''} size={14} />}
              onClick={() => void query.refetch()}
              disabled={query.isFetching}
            >
              Refresh
            </Button>
          }
        >
          {query.isPending && <TableLoading />}

          {query.isError && (
            <ErrorState
              title="Positions unavailable"
              description={query.error.message}
              action={<Button onClick={() => void query.refetch()}>Retry</Button>}
            />
          )}

          {query.data && query.data.length === 0 && (
            <EmptyState
              title="No positions found"
              description="No position rows match the selected filters."
            />
          )}

          {query.data && query.data.length > 0 && (
            <>
              <div className="overflow-x-auto">
                <div className="min-w-[1450px]">
                  <DataTable
                    columns={columns}
                    rows={query.data}
                    rowKey={(row) => row.positionId}
                  />
                </div>
              </div>
              <HistoryPagination
                skip={filters.query.skip}
                take={filters.query.take}
                returned={query.data.length}
                disabled={query.isFetching}
                onPrevious={filters.previous}
                onNext={filters.next}
              />
            </>
          )}
        </PageSection>
      </main>
    </>
  )
}

function TableLoading() {
  return (
    <div className="space-y-2">
      {Array.from({ length: 7 }, (_, index) => (
        <LoadingSkeleton key={index} className="h-14 w-full" />
      ))}
    </div>
  )
}
