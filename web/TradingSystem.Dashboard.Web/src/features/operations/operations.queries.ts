import {
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'

import {
  acknowledgeAlert,
  getAlerts,
  getAuditEvents,
  getComponentHealth,
} from '@/features/operations/operations.api'
import type { AuditQuery } from '@/types/operations'

export function useComponentHealth() {
  return useQuery({
    queryKey: ['dashboard', 'health', 'components'],
    queryFn: ({ signal }) => getComponentHealth(signal),
    refetchInterval: 10_000,
  })
}

export function useAlerts(acknowledged: boolean, take = 100,) {
  return useQuery({
    queryKey: ['dashboard', 'alerts', acknowledged, take],
    queryFn: ({ signal }) => getAlerts(acknowledged, take, signal),
    refetchInterval: acknowledged ? 30_000 : 10_000,
  })
}

export function useAcknowledgeAlert() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (alertId: number) =>
      acknowledgeAlert(alertId),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ['dashboard', 'alerts'],
        }),
        queryClient.invalidateQueries({
          queryKey: ['dashboard', 'overview'],
        }),
      ])
    },
  })
}

export function useAuditEvents(query: AuditQuery) {
  return useQuery({
    queryKey: ['dashboard', 'audit', query],
    queryFn: ({ signal }) =>
      getAuditEvents(query, signal),
  })
}
