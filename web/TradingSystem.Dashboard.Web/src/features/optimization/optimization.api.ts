import { apiRequest } from '@/api/api-client'
import type {
  JobAcceptedDto,
  OptimizationRequest,
  OptimizationTrialDto,
  RunSummaryDto,
  StrategyComparisonDto,
} from '@/types/optimization'

export function createOptimization(request: OptimizationRequest) {
  return apiRequest<JobAcceptedDto>('/optimizations', {
    method: 'POST',
    headers: {
      'X-Idempotency-Key': crypto.randomUUID(),
    },
    body: JSON.stringify(request),
  })
}

export function getOptimizationRuns(
  botName: string | undefined,
  skip: number,
  take: number,
  signal?: AbortSignal,
) {
  const parameters = new URLSearchParams({
    skip: String(skip),
    take: String(take),
  })

  if (botName?.trim())
    parameters.set('botName', botName.trim())

  return apiRequest<RunSummaryDto[]>(`/runs?${parameters}`, {
    method: 'GET',
    signal,
  })
}

export function getOptimizationTrials(
  runId: string,
  take = 100,
  signal?: AbortSignal,
) {
  return apiRequest<OptimizationTrialDto[]>(
    `/optimizations/${encodeURIComponent(runId)}/trials?take=${take}`,
    {
      method: 'GET',
      signal,
    },
  )
}

export function compareRuns(
  leftRunId: string,
  rightRunId: string,
  signal?: AbortSignal,
) {
  const parameters = new URLSearchParams({
    leftRunId,
    rightRunId,
  })

  return apiRequest<StrategyComparisonDto>(
    `/comparisons?${parameters}`,
    {
      method: 'GET',
      signal,
    },
  )
}
