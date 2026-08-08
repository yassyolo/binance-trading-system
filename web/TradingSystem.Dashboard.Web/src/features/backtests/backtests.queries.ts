import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'

import {
  createBacktest,
  getRuns,
} from '@/features/backtests/backtests.api'
import type {
  BacktestRequest,
  RunsQuery,
} from '@/types/backtesting'

export function runsQueryKey(query: RunsQuery) {
  return ['dashboard', 'runs', query] as const
}

export function useRuns(query: RunsQuery) {
  return useQuery({
    queryKey: runsQueryKey(query),
    queryFn: ({ signal }) => getRuns(query, signal),
    refetchInterval: 5_000,
  })
}

export function useCreateBacktest() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: BacktestRequest) => createBacktest(request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['dashboard', 'runs'],
      })
    },
  })
}
