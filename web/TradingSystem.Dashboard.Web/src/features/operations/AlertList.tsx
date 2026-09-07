import { useState } from 'react'
import { BellRing, Check, Clock3, RefreshCcw } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { ConfirmActionDialog } from '@/components/ui/ConfirmActionDialog'
import { EmptyState } from '@/components/ui/EmptyState'
import { ErrorState } from '@/components/ui/ErrorState'
import { LoadingSkeleton } from '@/components/ui/LoadingSkeleton'
import { StatusBadge } from '@/components/ui/StatusBadge'
import { relativeTime, severityTone, utcDate} from '@/features/operations/operations-formatters'
import { useAcknowledgeAlert, useAlerts } from '@/features/operations/operations.queries'
import type { AlertDto } from '@/types/operations'

interface AlertListProps {
  acknowledged: boolean
}

export function AlertList({acknowledged,}: AlertListProps) {
  const query = useAlerts(acknowledged, 100)
  const acknowledge = useAcknowledgeAlert()
  const [selected, setSelected] = useState<AlertDto | null>(null)

  if (query.isPending) {
    return (
      <div className="space-y-3">
        {Array.from({ length: 6 }, (_, index) => (
          <LoadingSkeleton
            key={index}
            className="h-28 w-full"
          />
        ))}
      </div>
    )
  }

  if (query.isError) {
    return (
      <ErrorState
        title="Alerts unavailable"
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
        title={
          acknowledged
            ? 'No acknowledged alerts'
            : 'No active alerts'
        }
        description={
          acknowledged
            ? 'Acknowledged operational alerts will appear here.'
            : 'There are currently no unacknowledged alerts requiring attention.'
        }
      />
    )
  }

  return (
    <>
      <div className="mb-3 flex justify-end">
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
          onClick={() => void query.refetch()}
        >
          Refresh
        </Button>
      </div>

      <div className="space-y-3">
        {query.data.map((alert) => (
          <Card
            key={alert.alertId}
            className="p-5"
          >
            <div className="flex items-start gap-4">
              <div className="grid size-10 shrink-0 place-items-center rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-strong)]">
                <BellRing
                  size={18}
                  className={
                    severityTone(alert.severity) === 'danger'
                      ? 'text-[var(--color-danger)]'
                      : severityTone(alert.severity) === 'warning'
                        ? 'text-[var(--color-warning)]'
                        : 'text-[var(--color-text-secondary)]'
                  }
                />
              </div>

              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-2">
                  <StatusBadge
                    label={alert.severity}
                    tone={severityTone(alert.severity)}
                  />

                  <StatusBadge
                    label={alert.type}
                    tone="neutral"
                    showDot={false}
                  />

                  {alert.acknowledged && (
                    <StatusBadge
                      label="Acknowledged"
                      tone="success"
                    />
                  )}
                </div>

                <p className="mt-3 text-sm leading-6 text-[var(--color-text-primary)]">
                  {alert.message}
                </p>

                <div className="mt-4 flex flex-wrap items-center gap-x-5 gap-y-2 text-xs text-[var(--color-text-muted)]">
                  <span className="inline-flex items-center gap-1.5">
                    <Clock3 size={12} />
                    {relativeTime(alert.createdAtUtc)}
                  </span>

                  <span>
                    {utcDate(alert.createdAtUtc)} UTC
                  </span>

                  {alert.botName && (
                    <span>
                      Bot: {alert.botName}
                    </span>
                  )}

                  {alert.positionId && (
                    <span
                      className="max-w-[240px] truncate font-mono"
                      title={alert.positionId}
                    >
                      Position: {alert.positionId}
                    </span>
                  )}
                </div>

                {alert.acknowledged && (
                  <p className="mt-3 text-xs text-[var(--color-text-muted)]">
                    Acknowledged by{' '}
                    <span className="text-[var(--color-text-secondary)]">
                      {alert.acknowledgedBy ?? 'Unknown'}
                    </span>
                    {' · '}
                    {utcDate(alert.acknowledgedAtUtc)} UTC
                  </p>
                )}
              </div>

              {!alert.acknowledged && (
                <Button
                  size="sm"
                  leftIcon={<Check size={14} />}
                  onClick={() => setSelected(alert)}
                >
                  Acknowledge
                </Button>
              )}
            </div>
          </Card>
        ))}
      </div>

      <ConfirmActionDialog
        open={selected !== null}
        title="Acknowledge alert?"
        description={
          selected
            ? `${selected.type}: ${selected.message}`
            : ''
        }
        confirmLabel="Acknowledge"
        busy={acknowledge.isPending}
        onClose={() => {
          if (!acknowledge.isPending)
            setSelected(null)
        }}
        onConfirm={() => {
          if (!selected) return

          acknowledge.mutate(
            selected.alertId,
            {
              onSuccess: () =>
                setSelected(null),
            },
          )
        }}
      />
    </>
  )
}
