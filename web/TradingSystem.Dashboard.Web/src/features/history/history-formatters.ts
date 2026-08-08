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

export function decimal(value: number | null, maximumFractionDigits = 8) {
  if (value === null) return '—'

  return new Intl.NumberFormat('en-US', {
    minimumFractionDigits: 0,
    maximumFractionDigits,
  }).format(value)
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

export function sideTone(side: string): StatusTone {
  const normalized = side.toLowerCase()
  if (normalized.includes('long') || normalized.includes('buy')) return 'success'
  if (normalized.includes('short') || normalized.includes('sell')) return 'danger'
  return 'neutral'
}

export function positionStatusTone(status: string): StatusTone {
  const normalized = status.toLowerCase()
  if (normalized === 'open' || normalized === 'active') return 'success'
  if (normalized === 'closed' || normalized === 'completed') return 'neutral'
  if (normalized.includes('cancel')) return 'warning'
  if (normalized.includes('fail') || normalized.includes('error')) return 'danger'
  return 'info'
}

export function decisionTone(decision: string | null): StatusTone {
  if (!decision) return 'neutral'

  const normalized = decision.toLowerCase()
  if (normalized.includes('open') || normalized.includes('allow') || normalized.includes('accepted'))
    return 'success'

  if (normalized.includes('block') || normalized.includes('reject') || normalized.includes('ignore'))
    return 'warning'

  return 'info'
}

export function pnlClass(value: number | null) {
  if (value === null || value === 0) return ''
  return value > 0 ? 'text-[var(--color-success)]' : 'text-[var(--color-danger)]'
}

export function duration(value: string | null) {
  if (!value) return '—'

  const match = value.match(/^(?:(\d+)\.)?(\d{2}):(\d{2}):(\d{2})(?:\.\d+)?$/)
  if (!match) return value

  const days = Number(match[1] ?? 0)
  const hours = Number(match[2])
  const minutes = Number(match[3])
  const seconds = Number(match[4])

  const parts: string[] = []
  if (days) parts.push(`${days}d`)
  if (hours) parts.push(`${hours}h`)
  if (minutes) parts.push(`${minutes}m`)
  if (!days && !hours && seconds) parts.push(`${seconds}s`)

  return parts.join(' ') || '0s'
}
