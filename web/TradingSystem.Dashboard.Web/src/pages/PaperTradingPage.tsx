import { useState } from 'react'
import { RefreshCcw, Search } from 'lucide-react'

import { PageHeader } from '@/components/layout/PageHeader'
import { Button } from '@/components/ui/Button'
import { EmptyState } from '@/components/ui/EmptyState'
import { ErrorState } from '@/components/ui/ErrorState'
import { LoadingSkeleton } from '@/components/ui/LoadingSkeleton'
import { PageSection } from '@/components/ui/PageSection'
import { PaperAccountSummary } from '@/features/paper/PaperAccountSummary'
import { PaperPositionsTable } from '@/features/paper/PaperPositionsTable'
import { PaperTradesTable } from '@/features/paper/PaperTradesTable'
import { ResetPaperAccount } from '@/features/paper/ResetPaperAccount'
import { utcDate } from '@/features/paper/paper-formatters'
import { usePaperSummary } from '@/features/paper/paper.queries'

export function PaperTradingPage() {
  const [draft, setDraft] = useState('')
  const [botName, setBotName] = useState<string | undefined>()
  const summary = usePaperSummary()

  const apply = () => setBotName(draft.trim() || undefined)

  return (
    <>
      <PageHeader title="Paper Trading" description="Simulated execution backed by the real paper trading persistence model." />
      <main className="space-y-10 p-8">
        <PageSection
          title="Paper account"
          description="Account values are calculated from persisted non-archived paper positions."
          actions={
            <Button size="sm" leftIcon={<RefreshCcw size={14} className={summary.isFetching ? 'animate-spin' : ''} />} onClick={() => void summary.refetch()}>
              Refresh
            </Button>
          }
        >
          {summary.isPending && <div className="grid grid-cols-3 gap-4">{Array.from({ length: 6 }, (_, i) => <LoadingSkeleton key={i} className="h-36" />)}</div>}
          {summary.isError && <ErrorState title="Paper account unavailable" description={summary.error.message} action={<Button onClick={() => void summary.refetch()}>Retry</Button>} />}
          {summary.data && (
            <div className="space-y-4">
              <PaperAccountSummary summary={summary.data} />
              <p className="text-right text-[10px] text-[var(--color-text-muted)]">
                Calculated {utcDate(summary.data.calculatedAtUtc)} UTC
              </p>
            </div>
          )}
          {!summary.isPending && !summary.isError && !summary.data && <EmptyState title="No paper account" description="No paper account state is available." />}
        </PageSection>

        <PageSection
          title="Position filter"
          description="Applies to open and closed paper positions."
          actions={
            <div className="flex gap-2">
              <input value={draft} onChange={(e) => setDraft(e.target.value)} onKeyDown={(e) => { if (e.key === 'Enter') apply() }} placeholder="Filter by bot" className="h-8 w-44 rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] px-3 text-xs outline-none focus:border-[#454954]" />
              <Button size="sm" leftIcon={<Search size={14} />} onClick={apply}>Filter</Button>
            </div>
          }
        >
          <p className="text-xs text-[var(--color-text-muted)]">{botName ? `Showing ${botName}.` : 'Showing all bots.'}</p>
        </PageSection>

        <PageSection title="Open positions" description="Current simulated exposure.">
          <PaperPositionsTable botName={botName} />
        </PageSection>

        <PageSection title="Closed positions" description="Closed paper positions are the current backend equivalent of paper trade history.">
          <PaperTradesTable botName={botName} />
        </PageSection>

        <PageSection title="Account maintenance" description="Administrator-only simulation reset.">
          <ResetPaperAccount />
        </PageSection>
      </main>
    </>
  )
}
