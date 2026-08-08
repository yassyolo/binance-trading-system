export type PaperPositionStatus = 'Open' | 'Closed' | 1 | 2
export type PositionSide = 'Long' | 'Short' | 1 | 2 | number

export interface PaperTradingAccountDto {
  initialBalance: number
  realizedPnl: number
  fees: number
  equity: number
  openPositions: number
  closedPositions: number
  calculatedAtUtc: string
}

export interface PaperTradingPositionDto {
  positionId: string
  shortId: string
  botName: string
  symbol: string
  side: PositionSide
  quantity: number
  entryPrice: number
  takeProfitPrice: number
  stopLossPrice: number
  entryFee: number
  exitPrice: number | null
  exitFee: number | null
  realizedPnl: number | null
  status: PaperPositionStatus
  source: string
  openedAtUtc: string
  closedAtUtc: string | null
  closeReason: string | null
  version: number
}
