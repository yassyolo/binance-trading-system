import { ArrowRight, Bot, RefreshCcw } from 'lucide-react'
import { Link } from 'react-router'

import { ApiError } from '@/api/api-error'
import { PageHeader } from '@/components/layout/PageHeader'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { EmptyState } from '@/components/ui/EmptyState'
import { ErrorState } from '@/components/ui/ErrorState'
import { LoadingSkeleton } from '@/components/ui/LoadingSkeleton'
import { PageSection } from '@/components/ui/PageSection'
import { StatusBadge } from '@/components/ui/StatusBadge'
import { environmentLabel } from '@/features/bots/bot-enums'
import { useBots } from '@/features/bots/bots.queries'

export function BotsPage() {
  const query = useBots()
  const error = query.error instanceof ApiError ? query.error : null

  return (
    <>
      <PageHeader
        title="Bots"
        description="Trading bot configuration, runtime controls and command history."
      />

      <main className="p-8">
        <PageSection
          title="Configured bots"
          description="Each configuration is versioned and updated through optimistic concurrency."
          actions={
            <Button
              size="sm"
              leftIcon={
                <RefreshCcw
                  className={query.isFetching ? 'animate-spin' : ''}
                  size={14}
                />
              }
              onClick={() => void query.refetch()}
              disabled={query.isFetching}
            >
              Refresh
            </Button>
          }
        >
          {query.isPending && (
            <div className="grid grid-cols-3 gap-4">
              {Array.from({ length: 6 }, (_, index) => (
                <Card key={index} className="p-5">
                  <LoadingSkeleton className="h-4 w-28" />
                  <LoadingSkeleton className="mt-4 h-3 w-20" />
                  <LoadingSkeleton className="mt-8 h-8 w-full" />
                </Card>
              ))}
            </div>
          )}

          {query.isError && (
            <ErrorState
              title={error?.status === 401 ? 'Authentication required' : 'Bots unavailable'}
              description={error?.message ?? query.error.message}
              action={<Button onClick={() => void query.refetch()}>Retry</Button>}
            />
          )}

          {query.data && query.data.length === 0 && (
            <EmptyState
              title="No bot configurations"
              description="Dashboard API returned no configured trading bots."
            />
          )}

          {query.data && query.data.length > 0 && (
            <div className="grid grid-cols-3 gap-4">
              {query.data.map((bot) => (
                <Link key={bot.botName} to={`/bots/${encodeURIComponent(bot.botName)}`}>
                  <Card className="group h-full p-5 transition hover:border-[#3a3e47] hover:bg-[var(--color-surface-hover)]">
                    <div className="flex items-start justify-between">
                      <div className="grid size-10 place-items-center rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-strong)] text-[var(--color-text-secondary)]">
                        <Bot size={18} />
                      </div>

                      {bot.restartRequired ? (
                        <StatusBadge label="Restart required" tone="warning" />
                      ) : (
                        <StatusBadge label="Configured" tone="success" />
                      )}
                    </div>

                    <h2 className="mt-7 text-base font-semibold">{bot.botName}</h2>
                    <p className="mt-1 text-xs text-[var(--color-text-muted)]">
                      {bot.strategyType} · {bot.symbol}
                    </p>

                    <div className="mt-6 grid grid-cols-3 gap-3 border-t border-[var(--color-border)] pt-4">
                      <Value label="Environment" value={environmentLabel(bot.environment)} />
                      <Value label="Quantity" value={String(bot.quantity)} />
                      <Value label="Leverage" value={`${bot.leverage}x`} />
                    </div>

                    <div className="mt-5 flex items-center justify-between text-xs text-[var(--color-text-muted)]">
                      <span>Version {bot.version}</span>
                      <ArrowRight
                        className="transition-transform group-hover:translate-x-1"
                        size={15}
                      />
                    </div>
                  </Card>
                </Link>
              ))}
            </div>
          )}
        </PageSection>
      </main>
    </>
  )
}

function Value({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[10px] uppercase tracking-[0.08em] text-[var(--color-text-muted)]">
        {label}
      </p>
      <p className="mt-1 truncate text-xs text-[var(--color-text-secondary)]">
        {value}
      </p>
    </div>
  )
}
