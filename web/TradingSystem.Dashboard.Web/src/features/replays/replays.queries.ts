import {
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'

import {
  cancelReplay,
  createReplay,
  getReplay,
  getReplays,
} from '@/features/replays/replays.api'
import type { ReplayRequest } from '@/types/replays'

export function replaysQueryKey(
  botName: string | undefined,
  skip: number,
  take: number,
) {
  return [
    'dashboard',
    'replays',
    botName ?? 'all',
    skip,
    take,
  ] as const
}

export function replayQueryKey(replayId: string) {
  return [
    'dashboard',
    'replays',
    replayId,
  ] as const
}

export function useCreateReplay() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: ReplayRequest) =>
      createReplay(request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['dashboard', 'replays'],
      })
    },
  })
}

export function useReplays(
  botName: string | undefined,
  skip: number,
  take: number,
) {
  return useQuery({
    queryKey: replaysQueryKey(
      botName,
      skip,
      take,
    ),
    queryFn: ({ signal }) =>
      getReplays(
        botName,
        skip,
        take,
        signal,
      ),
    refetchInterval: 5_000,
  })
}

export function useReplay(
  replayId: string | null,
) {
  return useQuery({
    queryKey: replayId
      ? replayQueryKey(replayId)
      : ['dashboard', 'replays', 'none'],
    queryFn: ({ signal }) =>
      getReplay(replayId!, signal),
    enabled: Boolean(replayId),
    refetchInterval: (query) => {
      const status = query.state.data?.status

      if (
        status === 'Completed' ||
        status === 'Failed' ||
        status === 'Cancelled'
      ) {
        return false
      }

      return 2_000
    },
  })
}

export function useCancelReplay(
  replayId: string,
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () =>
      cancelReplay(replayId),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: replayQueryKey(
            replayId,
          ),
        }),
        queryClient.invalidateQueries({
          queryKey: [
            'dashboard',
            'replays',
          ],
        }),
      ])
    },
  })
}
