import type { StatusTone } from '@/components/ui/StatusBadge'

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

export function relativeTime(value: string | null) {
  if (!value) return 'Never'

  const timestamp = new Date(value).getTime()
  if (Number.isNaN(timestamp)) return value

  const seconds = Math.max(0, Math.floor((Date.now() - timestamp) / 1000),)

  if (seconds < 60) 
    return `${seconds}s ago`
  if (seconds < 3600)
    return `${Math.floor(seconds / 60)}m ago`
  if (seconds < 86400)
    return `${Math.floor(seconds / 3600)}h ago`

  return `${Math.floor(seconds / 86400)}d ago`
}

export function healthTone(status: string): StatusTone {
  const value = status.toLowerCase()

  if (value === 'healthy') 
    return 'success'

  if (value.includes('degraded') || value.includes('warning') || value.includes('stale')) 
    return 'warning'

  if (value.includes('unhealthy') || value.includes('critical') || value.includes('down')) 
    return 'danger'

  return 'neutral'
}

export function severityTone(severity: string): StatusTone {
  const value = severity.toLowerCase()

  if (
    value === 'critical' ||
    value === 'fatal' ||
    value === 'error'
  ) {
    return 'danger'
  }

  if (
    value === 'warning' ||
    value === 'warn'
  ) {
    return 'warning'
  }

  if (
    value === 'info' ||
    value === 'information'
  ) {
    return 'info'
  }

  if (
    value === 'resolved' ||
    value === 'healthy'
  ) {
    return 'success'
  }

  return 'neutral'
}

export function prettyJson(value: string | null) {
  if (!value) return null

  try {
    return JSON.stringify(
      JSON.parse(value),
      null,
      2,
    )
  } catch {
    return value
  }
}

export function localInputValue(date: Date) {
  const offset = date.getTimezoneOffset()
  const local = new Date(
    date.getTime() - offset * 60_000,
  )

  return local.toISOString().slice(0, 16)
}

export function toUtc(value: string) {
  if (!value) return undefined

  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? undefined
    : date.toISOString()
}
