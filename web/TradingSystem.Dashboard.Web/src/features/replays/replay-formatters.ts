import type { StatusTone } from '@/components/ui/StatusBadge'
import type { ReplayMode, ReplayStatus } from '@/types/replays'

const statusLabels = [
  'Pending',
  'Processing',
  'Paused',
  'Completed',
  'Failed',
  'Cancelled',
] as const

const modeLabels = [
  'Timeline',
  'Projection',
  'StrategyComparison',
] as const

export function replayStatusLabel(status: ReplayStatus) {
  if (typeof status === 'number')
    return statusLabels[status] ?? `Unknown (${status})`

  return status
}

export function replayModeLabel(mode: ReplayMode) {
  if (typeof mode === 'number')
    return modeLabels[mode] ?? `Unknown (${mode})`

  return mode
}

export function replayStatusTone(status: ReplayStatus): StatusTone {
  const normalized = replayStatusLabel(status).toLowerCase()

  if (normalized === 'completed') return 'success'
  if (normalized === 'processing' || normalized === 'pending') return 'info'
  if (normalized === 'failed') return 'danger'
  if (normalized === 'cancelled' || normalized === 'paused') return 'warning'

  return 'neutral'
}

export function utcDate(value: string | null | undefined) {
  if (!value) return '—'

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value

  return new Intl.DateTimeFormat('en-GB', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
    timeZone: 'UTC',
  }).format(date)
}

export function localInputValue(date: Date) {
  const offset = date.getTimezoneOffset()
  const local = new Date(date.getTime() - offset * 60_000)
  return local.toISOString().slice(0, 16)
}

export function decimal(
  value: number | null | undefined,
  digits = 4,
) {
  if (value === null || value === undefined || !Number.isFinite(value))
    return '—'

  return new Intl.NumberFormat('en-US', {
    maximumFractionDigits: digits,
  }).format(value)
}

export function integer(value: number | null | undefined) {
  if (value === null || value === undefined || !Number.isFinite(value))
    return '0'

  return Math.trunc(value).toLocaleString('en-US')
}
