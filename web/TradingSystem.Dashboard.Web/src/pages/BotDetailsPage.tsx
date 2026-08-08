import { ArrowLeft, RefreshCcw, TriangleAlert } from 'lucide-react'
import { Link, useParams } from 'react-router'

import { ApiError } from '@/api/api-error'
import { PageHeader } from '@/components/layout/PageHeader'
import { BotCommandHistory } from '@/features/bots/BotCommandHistory'
import { BotCommandPanel } from '@/features/bots/BotCommandPanel'
import { BotConfigurationForm } from '@/features/bots/BotConfigurationForm'
import { environmentLabel } from '@/features/bots/bot-enums'
import { useBot } from '@/features/bots/bots.queries'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { ErrorState } from '@/components/ui/ErrorState'
import { LoadingSkeleton } from '@/components/ui/LoadingSkeleton'
import { PageSection } from '@/components/ui/PageSection'
import { StatusBadge } from '@/components/ui/StatusBadge'

export function BotDetailsPage() {
  const { botName = '' } = useParams()
  const query = useBot(botName)
  const error = query.error instanceof ApiError ? query.error : null

  if (query.isPending) {
    return (
      <>
        <PageHeader title={botName || 'Bot'} description="Loading bot configuration…" />
        <main className="space-y-4 p-8">
          <LoadingSkeleton className="h-28 w-full" />
          <LoadingSkeleton className="h-72 w-full" />
        </main>
      </>
    )
  }

  if (query.isError || !query.data) {
    return (
      <>
        <PageHeader title={botName || 'Bot'} description="Bot details" />
        <main className="p-8">
          <ErrorState
            title={query.data === null ? 'Bot not found' : 'Bot details unavailable'}
            description={error?.message ?? 'The bot configuration could not be loaded.'}
            action={<Button onClick={() => void query.refetch()}>Retry</Button>}
          />
        </main>
      </>
    )
  }

  const bot = query.data

  return (
    <>
      <PageHeader
        title={bot.botName}
        description={`${bot.strategyType} · ${bot.symbol} · ${environmentLabel(bot.environment)}`}
      />

      <main className="space-y-10 p-8">
        <div className="flex items-center justify-between">
          <Link
            to="/bots"
            className="inline-flex items-center gap-2 text-sm text-[var(--color-text-secondary)] transition hover:text-[var(--color-text-primary)]"
          >
            <ArrowLeft size={15} />
            All bots
          </Link>

          <Button
            size="sm"
            leftIcon={
              <RefreshCcw
                className={query.isFetching ? 'animate-spin' : ''}
                size={14}
              />
            }
            onClick={() => void query.refetch()}
          >
            Refresh
          </Button>
        </div>

        {bot.restartRequired && (
          <Card className="border-[rgba(251,191,36,0.22)] bg-[rgba(251,191,36,0.05)] p-5">
            <div className="flex items-start gap-3">
              <TriangleAlert className="mt-0.5 text-[var(--color-warning)]" size={18} />
              <div>
                <p className="text-sm font-semibold text-[var(--color-warning)]">
                  Configuration saved — restart required
                </p>
                <p className="mt-1 text-xs leading-5 text-[var(--color-text-secondary)]">
                  At least one persisted setting requires a service restart before it becomes active.
                </p>
              </div>
            </div>
          </Card>
        )}

        <div className="grid grid-cols-4 gap-4">
          <SummaryCard label="Environment" value={environmentLabel(bot.environment)} />
          <SummaryCard label="Signal source" value={bot.signalSource} />
          <SummaryCard label="Version" value={String(bot.version)} />
          <SummaryCard
            label="Last updated"
            value={formatDate(bot.updatedAtUtc)}
            helper={`by ${bot.updatedBy}`}
          />
        </div>

        <PageSection
          title="Runtime controls"
          description="State-changing commands are persisted and processed asynchronously."
        >
          <BotCommandPanel botName={bot.botName} />
        </PageSection>

        <PageSection
          title="Configuration"
          description="PUT replaces the full editable configuration using the current version."
        >
          <BotConfigurationForm configuration={bot} />
        </PageSection>

        <PageSection
          title="Command history"
          description="Recent queued and processed runtime commands."
        >
          <BotCommandHistory botName={bot.botName} />
        </PageSection>
      </main>
    </>
  )
}

function SummaryCard({
  label,
  value,
  helper,
}: {
  label: string
  value: string
  helper?: string
}) {
  return (
    <Card className="p-5">
      <p className="text-[10px] font-medium uppercase tracking-[0.12em] text-[var(--color-text-muted)]">
        {label}
      </p>
      <p className="mt-3 truncate text-sm font-semibold">{value}</p>
      {helper && (
        <p className="mt-2 truncate text-xs text-[var(--color-text-muted)]">
          {helper}
        </p>
      )}
    </Card>
  )
}

function formatDate(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value

  return new Intl.DateTimeFormat('en-GB', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).format(date)
}
