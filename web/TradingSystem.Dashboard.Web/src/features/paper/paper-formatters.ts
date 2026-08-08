import type {
  StatusTone,
} from '@/components/ui/StatusBadge'

export function money(
  value: number,
  currency = 'USD',
) {
  return new Intl.NumberFormat(
    'en-US',
    {
      style: 'currency',
      currency,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    },
  ).format(value)
}

export function decimal(
  value: number | null,
  digits = 4,
) {
  if (value === null) return '—'

  return new Intl.NumberFormat(
    'en-US',
    {
      maximumFractionDigits:
        digits,
    },
  ).format(value)
}

export function percent(
  value: number,
) {
  return `${value.toFixed(2)}%`
}

export function utcDate(
  value: string | null,
) {
  if (!value) return '—'

  const date = new Date(value)

  if (Number.isNaN(date.getTime()))
    return value

  return new Intl.DateTimeFormat(
    'en-GB',
    {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: false,
      timeZone: 'UTC',
    },
  ).format(date)
}

export function sideTone(
  side: string,
): StatusTone {
  const value =
    side.toLowerCase()

  if (
    value.includes('long') ||
    value.includes('buy')
  ) {
    return 'success'
  }

  if (
    value.includes('short') ||
    value.includes('sell')
  ) {
    return 'danger'
  }

  return 'neutral'
}

export function positionStatusTone(
  status: string,
): StatusTone {
  const value =
    status.toLowerCase()

  if (
    value === 'open' ||
    value === 'active'
  ) {
    return 'success'
  }

  if (
    value === 'closed' ||
    value === 'completed'
  ) {
    return 'neutral'
  }

  if (
    value.includes('cancel')
  ) {
    return 'warning'
  }

  return 'info'
}

export function pnlClass(
  value: number,
) {
  if (value === 0) return ''

  return value > 0
    ? 'text-[var(--color-success)]'
    : 'text-[var(--color-danger)]'
}
