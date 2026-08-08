export function money(value: number) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value)
}

export function percent(value: number, digits = 2) {
  return `${new Intl.NumberFormat('en-US', {
    minimumFractionDigits: digits,
    maximumFractionDigits: digits,
  }).format(value)}%`
}

export function minutes(value: number) {
  if (value < 60)
    return `${value.toFixed(1)} min`

  const hours = value / 60
  if (hours < 24)
    return `${hours.toFixed(1)} h`

  return `${(hours / 24).toFixed(1)} d`
}

export function compactDate(value: string) {
  const date = new Date(value)

  if (Number.isNaN(date.getTime()))
    return value

  return new Intl.DateTimeFormat('en-GB', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
    timeZone: 'UTC',
  }).format(date)
}

export function toUtc(localDateTime: string) {
  if (!localDateTime)
    return undefined

  const date = new Date(localDateTime)

  return Number.isNaN(date.getTime())
    ? undefined
    : date.toISOString()
}

export function localInputValue(date: Date) {
  const offset = date.getTimezoneOffset()
  const local = new Date(date.getTime() - offset * 60_000)

  return local.toISOString().slice(0, 16)
}
