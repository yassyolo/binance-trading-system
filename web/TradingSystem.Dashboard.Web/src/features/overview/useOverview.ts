import { useQuery } from '@tanstack/react-query'

import { getOverview } from '@/features/overview/overview.api'

export const overviewQueryKey = ['dashboard', 'overview'] as const

export function useOverview() {
  return useQuery({
    queryKey: overviewQueryKey,
    queryFn: ({ signal }) => getOverview(signal),
    refetchInterval: 15_000,
  })
}
