import type { BotRuntimeStatus, TradingEnvironment } from '@/types/dashboard-overview'
import type { StatusTone } from '@/components/ui/StatusBadge'

const runtimeStatuses = ['Stopped', 'Running', 'Paused', 'EmergencyStopped', 'Faulted'] as const
const environments = ['Demo', 'Production', 'Paper'] as const

export function runtimeStatusLabel(status: BotRuntimeStatus) {
  if (typeof status === 'number')
    return runtimeStatuses[status] ?? `Unknown (${status})`

  return status
}

export function environmentLabel(environment: TradingEnvironment) {
  if (typeof environment === 'number')
    return environments[environment] ?? `Unknown (${environment})`

  return environment
}

export function runtimeStatusTone(status: BotRuntimeStatus): StatusTone {
  const value = runtimeStatusLabel(status).toLowerCase()

  if (value === 'running') return 'success'
  if (value === 'paused' || value === 'stopped') return 'warning'
  if (value === 'faulted' || value === 'emergencystopped') return 'danger'
  return 'neutral'
}

export function healthTone(status: string): StatusTone {
  const value = status.toLowerCase()

  if (value.includes('healthy')) return 'success'
  if (value.includes('degraded') || value.includes('warning') || value.includes('stale')) return 'warning'
  if (value.includes('unhealthy') || value.includes('critical') || value.includes('down')) return 'danger'
  return 'neutral'
}

export function money(value: number) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value)
}

export function shortUtc(value: string | null) {
  if (!value) return 'Never'

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value

  return new Intl.DateTimeFormat('en-GB', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  }).format(date)
}

export function relativeTime(value: string | null) {
  if (!value) return 'Never'

  const timestamp = new Date(value).getTime()
  if (Number.isNaN(timestamp)) return value

  const seconds = Math.max(0, Math.floor((Date.now() - timestamp) / 1000))

  if (seconds < 60) return `${seconds}s ago`
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`
  if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`

  return `${Math.floor(seconds / 86400)}d ago`
}
