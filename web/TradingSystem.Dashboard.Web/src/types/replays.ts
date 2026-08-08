export type ReplayStatus =
  | 'Pending'
  | 'Processing'
  | 'Paused'
  | 'Completed'
  | 'Failed'
  | 'Cancelled'
  | number

export type ReplayMode =
  | 'Timeline'
  | 'Projection'
  | 'StrategyComparison'
  | number

export interface ReplayRequest {
  name: string
  mode: ReplayMode
  fromGlobalPosition: number | null
  toGlobalPosition: number | null
  fromUtc: string | null
  toUtc: string | null
  botName: string | null
  symbol: string | null
  correlationId: string | null
  candidateStrategyPluginId: string | null
  candidateStrategyVersion: string | null
  batchSize: number
  stopOnError: boolean
}

export interface ReplayAcceptedDto {
  replayId: string
}

export interface ReplaySummaryDto {
  replayId: string
  name: string
  mode: ReplayMode
  status: ReplayStatus
  requestedBy: string
  request: ReplayRequest
  lastGlobalPosition: number
  processedEvents: number
  failedEvents: number
  progressPercent: number
  progressStage: string | null
  error: string | null
  createdAtUtc: string
  startedAtUtc: string | null
  completedAtUtc: string | null
  deterministicHash: string | null

  // Compatibility with the older ZIP 10 UI.
  symbol?: string
  interval?: string
  stage?: string | null
  totalEvents?: number
  producedRunId?: string | null
}

export interface ReplayCheckpointDto {
  checkpointId?: string
  replayId: string
  timeUtc?: string
  stage?: string
  processedEvents: number
  totalEvents?: number
  message?: string | null
}

export interface ReplayStepDto {
  replayId: string
  globalPosition: number
  sourceEventId: string
  eventType: string
  virtualTimeUtc: string
  succeeded: boolean
  resultJson: string
  error: string | null

  // Compatibility with the older UI.
  stepId?: string
  timeUtc?: string
  kind?: string
  botName?: string
  symbol?: string
  side?: string | null
  price?: number | null
  decision?: string | null
  details?: string | null
}

export interface ReplayResultDto {
  replayId: string
  processedEvents: number
  failedEvents: number
  signals: number
  strategyDecisions: number
  riskAllowed: number
  riskBlocked: number
  executionsCompleted: number
  executionsFailed: number
  positionsOpened: number
  positionsClosed: number
  candidateMatches: number
  candidateDifferences: number
  deterministicHash: string
}

export interface ReplayDetailsDto extends ReplaySummaryDto {
  checkpoints?: ReplayCheckpointDto[]
  steps?: ReplayStepDto[]
  result?: ReplayResultDto | null
}
