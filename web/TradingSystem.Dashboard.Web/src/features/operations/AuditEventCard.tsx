import { useState } from 'react'
import {
  ChevronDown,
  ChevronUp,
  Copy,
  GitCompare,
} from 'lucide-react'

import { Card } from '@/components/ui/Card'
import { StatusBadge } from '@/components/ui/StatusBadge'
import {
  prettyJson,
  utcDate,
} from '@/features/operations/operations-formatters'
import type { AuditEventDto } from '@/types/operations'

export function AuditEventCard({
  event,
}: {
  event: AuditEventDto
}) {
  const [expanded, setExpanded] =
    useState(false)

  const oldValue =
    prettyJson(event.oldValueJson)

  const newValue =
    prettyJson(event.newValueJson)

  async function copyCorrelation() {
    if (!event.correlationId) return

    await navigator.clipboard.writeText(
      event.correlationId,
    )
  }

  return (
    <Card className="overflow-hidden">
      <div className="p-5">
        <div className="flex items-start justify-between gap-6">
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2">
              <StatusBadge
                label={event.action}
                tone="info"
                showDot={false}
              />

              <StatusBadge
                label={event.entityType}
                tone="neutral"
                showDot={false}
              />
            </div>

            <h3 className="mt-4 text-sm font-semibold">
              {event.actor}
            </h3>

            <p className="mt-1 text-xs text-[var(--color-text-muted)]">
              {utcDate(event.occurredAtUtc)} UTC
            </p>
          </div>

          {(oldValue || newValue) && (
            <button
              type="button"
              onClick={() =>
                setExpanded((current) => !current)
              }
              className="inline-flex h-8 items-center gap-2 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-strong)] px-3 text-xs text-[var(--color-text-secondary)] transition hover:bg-[var(--color-surface-hover)] hover:text-[var(--color-text-primary)]"
            >
              <GitCompare size={13} />
              Changes
              {expanded
                ? <ChevronUp size={13} />
                : <ChevronDown size={13} />}
            </button>
          )}
        </div>

        <div className="mt-5 grid grid-cols-2 gap-x-8 gap-y-4 border-t border-[var(--color-border)] pt-5 text-xs">
          <Info
            label="Entity ID"
            value={event.entityId}
            mono
          />

          <Info
            label="Reason"
            value={event.reason}
          />

          <div>
            <p className="text-[10px] uppercase tracking-[0.08em] text-[var(--color-text-muted)]">
              Correlation ID
            </p>

            {event.correlationId ? (
              <button
                type="button"
                onClick={() =>
                  void copyCorrelation()
                }
                className="mt-1 inline-flex max-w-full items-center gap-2 font-mono text-xs text-[var(--color-text-secondary)] transition hover:text-[var(--color-text-primary)]"
              >
                <Copy
                  className="shrink-0"
                  size={12}
                />
                <span className="truncate">
                  {event.correlationId}
                </span>
              </button>
            ) : (
              <p className="mt-1 text-[var(--color-text-muted)]">
                —
              </p>
            )}
          </div>

          <Info
            label="IP address"
            value={event.ipAddress}
            mono
          />
        </div>
      </div>

      {expanded && (
        <div className="grid grid-cols-2 gap-px border-t border-[var(--color-border)] bg-[var(--color-border)]">
          <JsonPanel
            title="Old value"
            value={oldValue}
          />

          <JsonPanel
            title="New value"
            value={newValue}
          />
        </div>
      )}
    </Card>
  )
}

function Info({
  label,
  value,
  mono = false,
}: {
  label: string
  value: string | null
  mono?: boolean
}) {
  return (
    <div className="min-w-0">
      <p className="text-[10px] uppercase tracking-[0.08em] text-[var(--color-text-muted)]">
        {label}
      </p>

      <p
        className={`mt-1 truncate text-[var(--color-text-secondary)] ${
          mono ? 'font-mono' : ''
        }`}
        title={value ?? undefined}
      >
        {value || '—'}
      </p>
    </div>
  )
}

function JsonPanel({
  title,
  value,
}: {
  title: string
  value: string | null
}) {
  return (
    <div className="min-w-0 bg-[var(--color-app)] p-5">
      <p className="text-[10px] font-medium uppercase tracking-[0.10em] text-[var(--color-text-muted)]">
        {title}
      </p>

      {value ? (
        <pre className="mt-3 max-h-96 overflow-auto whitespace-pre-wrap break-words rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-4 font-mono text-[11px] leading-5 text-[var(--color-text-secondary)]">
          {value}
        </pre>
      ) : (
        <p className="mt-3 text-xs text-[var(--color-text-muted)]">
          No value recorded.
        </p>
      )}
    </div>
  )
}
