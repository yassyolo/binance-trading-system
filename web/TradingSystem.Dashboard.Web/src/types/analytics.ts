export interface AnalyticsSummaryDto {
  totalPnl: number
  dailyPnl: number
  weeklyPnl: number
  monthlyPnl: number
  winRate: number
  averageWin: number
  averageLoss: number
  maxDrawdownPercent: number
  averageHoldingMinutes: number
  signals: number
  openedSignals: number
  blockedSignals: number
  blockReasons: Record<string, number>
  longPnl: number
  shortPnl: number
}

export interface EquityPointDto {
  timeUtc: string
  equity: number
  drawdownPercent: number
}

export interface PriceCandleDto {
  openTimeUtc: string
  open: number
  high: number
  low: number
  close: number
  volume: number
}

export interface ChartMarkerDto {
  timeUtc: string
  kind: string
  side: string
  price: number
  label: string | null
}

export interface PriceChartDto {
  candles: PriceCandleDto[]
  markers: ChartMarkerDto[]
}

export interface AnalyticsFilter {
  botName?: string
  symbol?: string
  fromUtc?: string
  toUtc?: string
}

export interface PriceChartFilter {
  symbol: string
  interval: string
  fromUtc: string
  toUtc: string
}
