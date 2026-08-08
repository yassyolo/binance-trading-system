export type BotRuntimeStatus =
  | 'Stopped'
  | 'Running'
  | 'Paused'
  | 'EmergencyStopped'
  | 'Faulted'
  | number

export type TradingEnvironment =
  | 'Demo'
  | 'Production'
  | 'Paper'
  | number

export interface BotOverviewDto {
  botName: string
  status: BotRuntimeStatus
  environment: TradingEnvironment
  signalSource: string
  lastSignalSide: string | null
  lastSignalAtUtc: string | null
  lastDecision: string | null
  lastDecisionReason: string | null
  lastDecisionAtUtc: string | null
  openPositions: number
  unrealizedPnl: number
  realizedPnlToday: number
  strategyVersion: string
  lastHeartbeatUtc: string | null
}

export interface ComponentHealthDto {
  component: string
  status: string
  lastSeenUtc: string | null
  details: string | null
}

export interface LiveOverviewDto {
  generatedAtUtc: string
  bots: BotOverviewDto[]
  components: ComponentHealthDto[]
  realizedPnlToday: number
  unrealizedPnl: number
  openPositions: number
  criticalAlerts: number
}
