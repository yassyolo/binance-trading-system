export interface SignalRowDto {
  id: number
  signalId: string
  timeUtc: string
  botName: string
  symbol: string
  source: string
  side: string
  price: number | null
  decision: string | null
  blockReason: string | null
  strategyVersion: string
  environment: string
}

export interface PositionRowDto {
  positionId: string
  botName: string
  symbol: string
  side: string
  status: string
  quantity: number
  entryPrice: number | null
  takeProfitPrice: number | null
  currentPrice: number | null
  unrealizedPnl: number
  realizedPnl: number | null
  openedAtUtc: string
  closedAtUtc: string | null
  strategyVersion: string
  environment: string
}

export interface TradeHistoryRowDto {
  positionId: string
  botName: string
  symbol: string
  side: string
  entryPrice: number | null
  exitPrice: number | null
  quantity: number
  realizedPnl: number | null
  fees: number | null
  duration: string | null
  source: string | null
  strategyVersion: string
  environment: string
  closeReason: string | null
  openedAtUtc: string
  closedAtUtc: string | null
}

export interface HistoryQuery {
  botName?: string
  symbol?: string
  status?: string
  fromUtc?: string
  toUtc?: string
  skip: number
  take: number
}
