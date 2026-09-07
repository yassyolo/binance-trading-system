import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getPaperPositions, getPaperTradingAccount, resetPaperTrading } from '@/features/paper/paper.api'

export function usePaperSummary() {
  return useQuery({
    queryKey: ['dashboard', 'paper', 'account'],
    queryFn: ({ signal }) => getPaperTradingAccount(signal),
    refetchInterval: 10_000,
  })
}

export function usePaperPositions(botName?: string) {
  return useQuery({
    queryKey: ['dashboard', 'paper', 'positions', 'open', botName ?? 'all'],
    queryFn: ({ signal }) => getPaperPositions({ botName, status: 'Open', take: 100 }, signal),
    refetchInterval: 5_000,
  })
}

export function usePaperTrades(botName?: string, take = 100) {
  return useQuery({
    queryKey: ['dashboard', 'paper', 'positions', 'closed', botName ?? 'all', take],
    queryFn: ({ signal }) => getPaperPositions({ botName, status: 'Closed', take }, signal),
    refetchInterval: 10_000,
  })
}

export function useResetPaperTrading() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => resetPaperTrading(),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['dashboard', 'paper'] }),
        queryClient.invalidateQueries({ queryKey: ['dashboard', 'overview'] }),
      ])
    },
  })
}
