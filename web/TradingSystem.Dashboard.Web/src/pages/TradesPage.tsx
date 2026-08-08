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
  duration,
  money,
  pnlClass,
  sideTone,
  utcDate,
} from '@/features/history/history-formatters'
import { useTrades } from '@/features/history/history.queries'
import { useHistoryFilterState } from '@/features/history/useHistoryFilterState'
import type { TradeHistoryRowDto } from '@/types/trading-history'

const columns: DataTableColumn<TradeHistoryRowDto>[] = [
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
    key: 'exit',
    header: 'Exit',
    align: 'right',
    render: (row) => decimal(row.exitPrice, 4),
  },
  {
    key: 'pnl',
    header: 'Realized PnL',
    align: 'right',
    render: (row) => (
      <span className={pnlClass(row.realizedPnl)}>
        {money(row.realizedPnl)}
      </span>
    ),
  },
  {
    key: 'fees',
    header: 'Fees',
    align: 'right',
    render: (row) => money(row.fees),
  },
  {
    key: 'duration',
    header: 'Duration',
    render: (row) => duration(row.duration),
  },
  {
    key: 'reason',
    header: 'Close reason',
    render: (row) => (
      <div className="max-w-[200px]">
        <p className="truncate text-[var(--color-text-primary)]" title={row.closeReason ?? undefined}>
          {row.closeReason ?? '—'}
        </p>
        <p className="mt-1 text-xs text-[var(--color-text-muted)]">
          {row.source ?? 'Unknown source'}
        </p>
      </div>
    ),
  },
  {
    key: 'closed',
    header: 'Closed UTC',
    render: (row) => <span className="whitespace-nowrap text-xs">{utcDate(row.closedAtUtc)}</span>,
  },
]

export function TradesPage() {
  const filters = useHistoryFilterState(50)
  const query = useTrades(filters.query)

  const totalPnl = query.data?.reduce(
    (sum, trade) => sum + (trade.realizedPnl ?? 0),
    0,
  ) ?? 0

  const totalFees = query.data?.reduce(
    (sum, trade) => sum + (trade.fees ?? 0),
    0,
  ) ?? 0

  return (
    <>
      <PageHeader
        title="Trades"
        description="Closed-position trade history and realized economic result."
      />

      <main className="space-y-6 p-8">
        <HistoryFilters
          values={filters.draft}
          onChange={filters.setDraft}
          onApply={filters.apply}
          onReset={filters.reset}
        />

        {query.data && query.data.length > 0 && (
          <div className="grid grid-cols-3 gap-4">
            <Summary label="Rows on page" value={String(query.data.length)} />
            <Summary
              label="Realized PnL on page"
              value={money(totalPnl)}
              className={pnlClass(totalPnl)}
            />
            <Summary label="Fees on page" value={money(totalFees)} />
          </div>
        )}

        <PageSection
          title="Trade history"
          description="Closed positions returned newest first by the persistence store."
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
              title="Trades unavailable"
              description={query.error.message}
              action={<Button onClick={() => void query.refetch()}>Retry</Button>}
            />
          )}

          {query.data && query.data.length === 0 && (
            <EmptyState
              title="No trades found"
              description="No closed trade rows match the selected filters."
            />
          )}

          {query.data && query.data.length > 0 && (
            <>
              <div className="overflow-x-auto">
                <div className="min-w-[1450px]">
                  <DataTable
                    columns={columns}
                    rows={query.data}
                    rowKey={(row) => `${row.positionId}-${row.closedAtUtc ?? ''}`}
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

function Summary({
  label,
  value,
  className = '',
}: {
  label: string
  value: string
  className?: string
}) {
  return (
    <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] p-5">
      <p className="text-[10px] font-medium uppercase tracking-[0.12em] text-[var(--color-text-muted)]">
        {label}
      </p>
      <p className={`mt-3 text-lg font-semibold ${className}`}>{value}</p>
    </div>
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
