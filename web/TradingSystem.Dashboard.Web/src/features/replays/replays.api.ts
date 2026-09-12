import { apiRequest } from '@/api/api-client'

import type {
  ReplayAcceptedDto,
  ReplayDetailsDto,
  ReplayRequest,
  ReplayResultDto,
  ReplayStepDto,
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

export async function getReplay(
  replayId: string,
  signal?: AbortSignal,
): Promise<ReplayDetailsDto> {
  const encodedReplayId =
    encodeURIComponent(replayId)

  const replay =
    await apiRequest<ReplaySummaryDto>(
      `/replays/${encodedReplayId}`,
      {
        method: 'GET',
        signal,
      },
    )

  const [steps, result] =
    await Promise.all([
      apiRequest<ReplayStepDto[]>(
        `/replays/${encodedReplayId}/steps?afterGlobalPosition=0&take=500`,
        {
          method: 'GET',
          signal,
        },
      ),
      getReplayResult(
        encodedReplayId,
        signal,
      ),
    ])

  return {
    ...replay,
    steps,
    result,
  }
}

async function getReplayResult(
  encodedReplayId: string,
  signal?: AbortSignal,
): Promise<ReplayResultDto | null> {
  try {
    return await apiRequest<ReplayResultDto>(
      `/replays/${encodedReplayId}/result`,
      {
        method: 'GET',
        signal,
      },
    )
  } catch {
    return null
  }
}

export function cancelReplay(replayId: string) {
  return apiRequest<void>(`/replays/${encodeURIComponent(replayId)}/cancel`, { method: 'POST', headers: {'X-Idempotency-Key': crypto.randomUUID(),}})
}
