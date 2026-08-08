import {
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'

import {
  compareRuns,
  createOptimization,
  getOptimizationRuns,
  getOptimizationTrials,
} from '@/features/optimization/optimization.api'
import type { OptimizationRequest } from '@/types/optimization'

export function useCreateOptimization() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: OptimizationRequest) =>
      createOptimization(request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['dashboard', 'optimization-runs'],
      })
    },
  })
}

export function useOptimizationRuns(
  botName: string | undefined,
  skip: number,
  take: number,
) {
  return useQuery({
    queryKey: [
      'dashboard',
      'optimization-runs',
      botName ?? 'all',
      skip,
      take,
    ],
    queryFn: ({ signal }) =>
      getOptimizationRuns(botName, skip, take, signal),
    refetchInterval: 5_000,
  })
}

export function useOptimizationTrials(
  runId: string | null,
  take = 100,
) {
  return useQuery({
    queryKey: [
      'dashboard',
      'optimization-trials',
      runId,
      take,
    ],
    queryFn: ({ signal }) =>
      getOptimizationTrials(runId!, take, signal),
    enabled: Boolean(runId),
  })
}

export function useRunComparison(
  leftRunId: string,
  rightRunId: string,
) {
  return useQuery({
    queryKey: [
      'dashboard',
      'run-comparison',
      leftRunId,
      rightRunId,
    ],
    queryFn: ({ signal }) =>
      compareRuns(leftRunId, rightRunId, signal),
    enabled:
      Boolean(leftRunId) &&
      Boolean(rightRunId) &&
      leftRunId !== rightRunId,
  })
}
