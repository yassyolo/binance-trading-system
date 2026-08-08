import { apiRequest } from '@/api/api-client'
import type {
  BacktestRequest,
  JobAcceptedDto,
  RunsQuery,
  RunSummaryDto,
} from '@/types/backtesting'

export function createBacktest(request: BacktestRequest) {
  return apiRequest<JobAcceptedDto>('/backtests', {
    method: 'POST',
    headers: {
      'X-Idempotency-Key': crypto.randomUUID(),
    },
    body: JSON.stringify(request),
  })
}

export function getRuns(
  query: RunsQuery,
  signal?: AbortSignal,
) {
  const parameters = new URLSearchParams({
    skip: String(query.skip),
    take: String(query.take),
  })

  if (query.botName?.trim())
    parameters.set('botName', query.botName.trim())

  return apiRequest<RunSummaryDto[]>(`/runs?${parameters}`, {
    method: 'GET',
    signal,
  })
}
