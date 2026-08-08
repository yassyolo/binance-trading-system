export interface BacktestRequest {
  botName: string
  symbol: string
  fromUtc: string
  toUtc: string
  initialBalance: number
  signalSource: string
  commissionPercent: number
  slippagePercent: number
  parameters: Record<string, string>
}

export interface JobAcceptedDto {
  jobId: string
  type: string
  status: string
  createdAtUtc: string
}

export interface RunSummaryDto {
  runId: string
  runType: string
  botName: string
  strategyVersion: string
  symbol: string
  interval: string
  startedAtUtc: string
  completedAtUtc: string | null
  status: string
  netProfit: number | null
  winRatePercent: number | null
  maxDrawdownPercent: number | null
  score: number | null
}

export interface RunsQuery {
  botName?: string
  skip: number
  take: number
}
