import { apiRequest } from '@/api/api-client'
import type {
  ReplayAcceptedDto,
  ReplayDetailsDto,
  ReplayRequest,
  ReplaySummaryDto,
} from '@/types/replays'

export function createReplay(request: ReplayRequest) {
  return apiRequest<ReplayAcceptedDto>('/replays', {
    method: 'POST',
    headers: {
      'X-Idempotency-Key': crypto.randomUUID(),
    },
    body: JSON.stringify(request),
  })
}

export function getReplays(botName: string | undefined, skip: number, take: number, signal?: AbortSignal) {
  const parameters = new URLSearchParams({skip: String(skip), take: String(take)})

  if (botName?.trim())
    parameters.set('botName', botName.trim())

  return apiRequest<ReplaySummaryDto[]>(`/replays?${parameters}`, { method: 'GET', signal })
}

export function getReplay(replayId: string, signal?: AbortSignal) {
  return apiRequest<ReplayDetailsDto>(`/replays/${encodeURIComponent(replayId)}`, { method: 'GET', signal })
}

export function cancelReplay(replayId: string) {
  return apiRequest<void>(`/replays/${encodeURIComponent(replayId)}/cancel`, { method: 'POST', headers: {'X-Idempotency-Key': crypto.randomUUID(),}})
}
