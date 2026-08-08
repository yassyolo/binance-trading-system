import type {
  BotCommandStatus,
  BotCommandType,
  TradingEnvironment,
} from '@/types/bots'
import type { StatusTone } from '@/components/ui/StatusBadge'

export const tradingEnvironmentValues = {
  Demo: 0,
  Production: 1,
} as const

export const botCommandValues = {
  Start: 0,
  Stop: 1,
  Pause: 2,
  Resume: 3,
  EmergencyStop: 4,
  ClosePosition: 5,
  CancelTakeProfit: 6,
  RecreateTakeProfit: 7,
} as const

const commandLabels = [
  'Start',
  'Stop',
  'Pause',
  'Resume',
  'Emergency Stop',
  'Close Position',
  'Cancel Take Profit',
  'Recreate Take Profit',
] as const

const commandStatusLabels = [
  'Pending',
  'Processing',
  'Completed',
  'Failed',
  'Rejected',
] as const

export function environmentLabel(value: TradingEnvironment) {
  if (typeof value === 'number')
    return value === 0 ? 'Demo' : value === 1 ? 'Production' : `Unknown (${value})`

  return value
}

export function environmentValue(value: TradingEnvironment) {
  if (typeof value === 'number') return value
  return value.toLowerCase() === 'production' ? 1 : 0
}

export function commandLabel(value: BotCommandType) {
  if (typeof value === 'number')
    return commandLabels[value] ?? `Unknown (${value})`

  return value.replace(/([a-z])([A-Z])/g, '$1 $2')
}

export function commandStatusLabel(value: BotCommandStatus) {
  if (typeof value === 'number')
    return commandStatusLabels[value] ?? `Unknown (${value})`

  return value
}

export function commandStatusTone(value: BotCommandStatus): StatusTone {
  const label = commandStatusLabel(value).toLowerCase()

  if (label === 'completed') return 'success'
  if (label === 'pending' || label === 'processing') return 'info'
  if (label === 'failed') return 'danger'
  if (label === 'rejected') return 'warning'
  return 'neutral'
}
