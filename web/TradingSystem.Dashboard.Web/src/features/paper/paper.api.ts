import { apiRequest } from '@/api/api-client'
import type {
  PaperPositionStatus,
  PaperTradingAccountDto,
  PaperTradingPositionDto,
} from '@/types/paper-trading'

export function getPaperTradingAccount(signal?: AbortSignal) {
  return apiRequest<PaperTradingAccountDto>('/paper/account', {
    method: 'GET',
    signal,
  })
}

export function getPaperPositions(
  options: {
    botName?: string
    symbol?: string
    status?: PaperPositionStatus
    skip?: number
    take?: number
  } = {},
  signal?: AbortSignal,
) {
  const parameters = new URLSearchParams({
    skip: String(options.skip ?? 0),
    take: String(options.take ?? 100),
  })

  if (options.botName?.trim())
    parameters.set('botName', options.botName.trim())

  if (options.symbol?.trim())
    parameters.set('symbol', options.symbol.trim().toUpperCase())

  if (options.status !== undefined)
    parameters.set('status', String(options.status))

  return apiRequest<PaperTradingPositionDto[]>(
    `/paper/positions?${parameters}`,
    { method: 'GET', signal },
  )
}

export function resetPaperTrading() {
  return apiRequest<void>('/paper/reset', {
    method: 'POST',
    headers: {
      'X-Idempotency-Key': crypto.randomUUID(),
    },
  })
}
