import { apiRequest } from '@/api/api-client'
import type {
  HistoryQuery,
  PositionRowDto,
  SignalRowDto,
  TradeHistoryRowDto,
} from '@/types/trading-history'

function queryString(query: HistoryQuery, includeDates = false, includeStatus = false) {
  const parameters = new URLSearchParams()

  parameters.set('skip', String(query.skip))
  parameters.set('take', String(query.take))

  if (query.botName?.trim())
    parameters.set('botName', query.botName.trim())

  if (query.symbol?.trim())
    parameters.set('symbol', query.symbol.trim().toUpperCase())

  if (includeDates && query.fromUtc)
    parameters.set('fromUtc', query.fromUtc)

  if (includeDates && query.toUtc)
    parameters.set('toUtc', query.toUtc)

  if (includeStatus && query.status?.trim())
    parameters.set('status', query.status.trim())

  return parameters.toString()
}

export function getSignals(query: HistoryQuery, signal?: AbortSignal) {
  return apiRequest<SignalRowDto[]>(
    `/signals?${queryString(query, true, false)}`,
    { method: 'GET', signal },
  )
}

export function getPositions(query: HistoryQuery, signal?: AbortSignal) {
  return apiRequest<PositionRowDto[]>(
    `/positions?${queryString(query, false, true)}`,
    { method: 'GET', signal },
  )
}

export function getTrades(query: HistoryQuery, signal?: AbortSignal) {
  return apiRequest<TradeHistoryRowDto[]>(
    `/trades?${queryString(query, false, false)}`,
    { method: 'GET', signal },
  )
}
