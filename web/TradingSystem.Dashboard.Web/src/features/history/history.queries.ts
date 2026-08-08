import { useQuery } from '@tanstack/react-query'

import {
  getPositions,
  getSignals,
  getTrades,
} from '@/features/history/history.api'
import type { HistoryQuery } from '@/types/trading-history'

export function useSignals(query: HistoryQuery) {
  return useQuery({
    queryKey: ['dashboard', 'signals', query],
    queryFn: ({ signal }) => getSignals(query, signal),
  })
}

export function usePositions(query: HistoryQuery) {
  return useQuery({
    queryKey: ['dashboard', 'positions', query],
    queryFn: ({ signal }) => getPositions(query, signal),
  })
}

export function useTrades(query: HistoryQuery) {
  return useQuery({
    queryKey: ['dashboard', 'trades', query],
    queryFn: ({ signal }) => getTrades(query, signal),
  })
}
