import { apiRequest } from '@/api/api-client'
import type { AnalyticsFilter, AnalyticsSummaryDto, EquityPointDto, PriceChartDto, PriceChartFilter } from '@/types/analytics'

function appendOptional(parameters: URLSearchParams, name: string, value: string | undefined) {
  if (value?.trim())
    parameters.set(name, value.trim())
}

export function getAnalytics(filter: AnalyticsFilter, signal?: AbortSignal,) {
  const parameters = new URLSearchParams()

  appendOptional(parameters, 'botName', filter.botName)
  appendOptional(parameters, 'symbol', filter.symbol?.toUpperCase())
  appendOptional(parameters, 'fromUtc', filter.fromUtc)
  appendOptional(parameters, 'toUtc', filter.toUtc)

  const suffix = parameters.size ? `?${parameters}` : ''

  return apiRequest<AnalyticsSummaryDto>(`/analytics${suffix}`, { method: 'GET', signal })
}

export function getEquity(botName?: string, signal?: AbortSignal) {
  const parameters = new URLSearchParams()
  appendOptional(parameters, 'botName', botName)

  const suffix = parameters.size ? `?${parameters}` : ''

  return apiRequest<EquityPointDto[]>(`/analytics/equity${suffix}`, { method: 'GET', signal})
}

export function getPriceChart(filter: PriceChartFilter, signal?: AbortSignal,) {
  const parameters = new URLSearchParams({fromUtc: filter.fromUtc, toUtc: filter.toUtc,})

  return apiRequest<PriceChartDto>(`/charts/${encodeURIComponent(filter.symbol.toUpperCase())}/${encodeURIComponent(filter.interval.toLowerCase())}?${parameters}`, { method: 'GET', signal },)
}
