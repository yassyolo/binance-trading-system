export type TradingEnvironment =
  | 'Paper'
  | 'Demo'
  | 'Production'
  | 0
  | 1
  | 2
  
export type BotCommandType =
  | 'Start'
  | 'Stop'
  | 'Pause'
  | 'Resume'
  | 'EmergencyStop'
  | 'ClosePosition'
  | 'CancelTakeProfit'
  | 'RecreateTakeProfit'
  | number

export type BotCommandStatus =
  | 'Pending'
  | 'Processing'
  | 'Completed'
  | 'Failed'
  | 'Rejected'
  | number

export interface BotConfigurationDto {
  botName: string
  strategyType: string
  symbol: string
  environment: TradingEnvironment
  signalSource: string
  enableLong: boolean
  enableShort: boolean
  quantity: number
  leverage: number
  priceDistance: number | null
  profitDistance: number | null
  orderSideLimit: number | null
  cooldownSeconds: number
  version: number
  updatedAtUtc: string
  updatedBy: string
  restartRequired: boolean
}

export interface UpdateBotConfigurationRequest {
  expectedVersion: number
  strategyType: string
  symbol: string
  environment: number
  signalSource: string
  enableLong: boolean
  enableShort: boolean
  quantity: number
  leverage: number
  priceDistance: number | null
  profitDistance: number | null
  orderSideLimit: number | null
  cooldownSeconds: number
  reason: string
}

export interface BotCommandRequest {
  command: number
  reason: string
  confirmed: boolean
  cancelOpenOrders: boolean
  closeOpenPositions: boolean
  positionId: string | null
}

export interface BotCommandDto {
  commandId: string
  botName: string
  command: BotCommandType
  status: BotCommandStatus
  requestedBy: string
  reason: string
  requestedAtUtc: string
  completedAtUtc: string | null
  error: string | null
}
