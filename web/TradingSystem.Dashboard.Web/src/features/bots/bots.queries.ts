import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'

import {
  getBot,
  getBotCommands,
  getBots,
  sendBotCommand,
  updateBotConfiguration,
} from '@/features/bots/bots.api'
import type {
  BotCommandRequest,
  UpdateBotConfigurationRequest,
} from '@/types/bots'

export const botsQueryKey = ['dashboard', 'bots'] as const

export function botQueryKey(botName: string) {
  return ['dashboard', 'bots', botName] as const
}

export function botCommandsQueryKey(botName: string) {
  return ['dashboard', 'commands', botName] as const
}

export function useBots() {
  return useQuery({
    queryKey: botsQueryKey,
    queryFn: ({ signal }) => getBots(signal),
  })
}

export function useBot(botName: string) {
  return useQuery({
    queryKey: botQueryKey(botName),
    queryFn: ({ signal }) => getBot(botName, signal),
    enabled: Boolean(botName),
  })
}

export function useBotCommands(botName: string) {
  return useQuery({
    queryKey: botCommandsQueryKey(botName),
    queryFn: ({ signal }) => getBotCommands(botName, 25, signal),
    enabled: Boolean(botName),
    refetchInterval: 5_000,
  })
}

export function useUpdateBotConfiguration(botName: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: UpdateBotConfigurationRequest) =>
      updateBotConfiguration(botName, request),
    onSuccess: async (updated) => {
      queryClient.setQueryData(botQueryKey(botName), updated)
      await queryClient.invalidateQueries({ queryKey: botsQueryKey })
    },
  })
}

export function useSendBotCommand(botName: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: BotCommandRequest) =>
      sendBotCommand(botName, request),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: botCommandsQueryKey(botName) }),
        queryClient.invalidateQueries({ queryKey: ['dashboard', 'overview'] }),
      ])
    },
  })
}
