import type {
  JobAcceptedDto,
  RunSummaryDto,
} from '@/types/backtesting'

export interface OptimizationRangeDto {
  name: string
  from: number
  to: number
  step: number
}

export interface OptimizationRequest {
  botName: string
  symbol: string
  fromUtc: string
  toUtc: string
  initialBalance: number
  signalSource: string
  ranges: OptimizationRangeDto[]
  topResults: number
  walkForward: boolean
  trainBars: number | null
  testBars: number | null
  stepBars: number | null
}

export interface OptimizationTrialDto {
  trialId: string
  rank: number
  parameters: Record<string, string>
  score: number
  netProfit: number
  drawdownPercent: number
  winRatePercent: number
  trades: number
  selected: boolean
}

export interface StrategyComparisonDto {
  leftRunId: string
  rightRunId: string
  leftLabel: string
  rightLabel: string
  netProfitDifference: number
  drawdownDifference: number
  winRateDifference: number
  scoreDifference: number
}

export type {
  JobAcceptedDto,
  RunSummaryDto,
}
