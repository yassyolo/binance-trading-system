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
  decisionTone,
  sideTone,
  utcDate,
} from '@/features/history/history-formatters'
import { useSignals } from '@/features/history/history.queries'
import { useHistoryFilterState } from '@/features/history/useHistoryFilterState'
import type { SignalRowDto } from '@/types/trading-history'

const columns: DataTableColumn<SignalRowDto>[] = [
  {
    key: 'time',
    header: 'Time UTC',
    render: (row) => (
      <span className="whitespace-nowrap text-xs">{utcDate(row.timeUtc)}</span>
    ),
  },
  {
    key: 'bot',
    header: 'Bot',
    render: (row) => (
      <div>
        <p className="font-medium text-[var(--color-text-primary)]">{row.botName}</p>
        <p className="mt-1 text-xs text-[var(--color-text-muted)]">{row.strategyVersion}</p>
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
    key: 'price',
    header: 'Price',
    align: 'right',
    render: (row) => decimal(row.price, 4),
  },
  {
    key: 'source',
    header: 'Source',
    render: (row) => (
      <div>
        <p>{row.source}</p>
        <p className="mt-1 text-xs text-[var(--color-text-muted)]">{row.environment}</p>
      </div>
    ),
  },
  {
    key: 'decision',
    header: 'Decision',
    render: (row) => (
      <div className="max-w-[270px]">
        <StatusBadge label={row.decision ?? 'Pending'} tone={decisionTone(row.decision)} />
        {row.blockReason && (
          <p className="mt-2 truncate text-xs text-[var(--color-text-muted)]" title={row.blockReason}>
            {row.blockReason}
          </p>
        )}
      </div>
    ),
  },
  {
    key: 'signalId',
    header: 'Signal ID',
    render: (row) => (
      <span className="block max-w-[150px] truncate font-mono text-[11px]" title={row.signalId}>
        {row.signalId}
      </span>
    ),
  },
]

export function SignalsPage() {
  const filters = useHistoryFilterState(50, { dates: true })
  const query = useSignals(filters.query)

  return (
    <>
      <PageHeader
        title="Signals"
        description="Signal history, strategy decisions and block reasons."
      />

      <main className="space-y-6 p-8">
        <HistoryFilters
          values={filters.draft}
          showDates
          onChange={filters.setDraft}
          onApply={filters.apply}
          onReset={filters.reset}
        />

        <PageSection
          title="Signal history"
          description="Newest signals are returned first."
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
              title="Signals unavailable"
              description={query.error.message}
              action={<Button onClick={() => void query.refetch()}>Retry</Button>}
            />
          )}

          {query.data && query.data.length === 0 && (
            <EmptyState
              title="No signals found"
              description="No signal rows match the selected filters."
            />
          )}

          {query.data && query.data.length > 0 && (
            <>
              <DataTable columns={columns} rows={query.data} rowKey={(row) => `${row.id}-${row.signalId}`} />
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
