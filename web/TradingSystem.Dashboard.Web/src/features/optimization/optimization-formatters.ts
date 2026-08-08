import type { StatusTone } from '@/components/ui/StatusBadge'

export function money(value: number | null) {
  if (value === null) return '—'

  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value)
}

export function percent(value: number | null) {
  if (value === null) return '—'
  return `${value.toFixed(2)}%`
}

export function score(value: number | null) {
  if (value === null) return '—'
  return value.toFixed(4)
}

export function utcDate(value: string | null) {
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

export function runStatusTone(status: string): StatusTone {
  const normalized = status.toLowerCase()

  if (normalized === 'completed') return 'success'
  if (
    normalized === 'running' ||
    normalized === 'processing' ||
    normalized === 'pending'
  ) return 'info'

  if (normalized === 'failed') return 'danger'
  if (normalized === 'cancelled') return 'warning'

  return 'neutral'
}

export function pnlClass(value: number) {
  if (value === 0) return ''
  return value > 0
    ? 'text-[var(--color-success)]'
    : 'text-[var(--color-danger)]'
}

export function localInputValue(date: Date) {
  const offset = date.getTimezoneOffset()
  const local = new Date(date.getTime() - offset * 60_000)
  return local.toISOString().slice(0, 16)
}

export function combinationsForRange(
  from: number,
  to: number,
  step: number,
) {
  if (
    !Number.isFinite(from) ||
    !Number.isFinite(to) ||
    !Number.isFinite(step) ||
    step <= 0 ||
    to < from
  ) {
    return 0
  }

  return Math.floor((to - from) / step) + 1
}
