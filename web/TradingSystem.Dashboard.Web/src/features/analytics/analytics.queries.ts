import { useQuery } from '@tanstack/react-query'
import { getAnalytics, getEquity, getPriceChart } from '@/features/analytics/analytics.api'
import type { AnalyticsFilter, PriceChartFilter } from '@/types/analytics'

export function useAnalytics(filter: AnalyticsFilter) {
  return useQuery({
    queryKey: ['dashboard', 'analytics', filter],
    queryFn: ({ signal }) => getAnalytics(filter, signal),
  })
}

export function useEquity(botName?: string) {
  return useQuery({
    queryKey: ['dashboard', 'analytics', 'equity', botName ?? 'all'],
    queryFn: ({ signal }) => getEquity(botName, signal),
  })
}

export function usePriceChart(filter: PriceChartFilter) {
  return useQuery({
    queryKey: ['dashboard', 'charts', filter],
    queryFn: ({ signal }) => getPriceChart(filter, signal),
    enabled:
      Boolean(filter.symbol.trim()) &&
      Boolean(filter.interval.trim()) &&
      Boolean(filter.fromUtc) &&
      Boolean(filter.toUtc),
  })
}
