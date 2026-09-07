import { apiRequest } from '@/api/api-client'
import type { BotCommandDto, BotCommandRequest, BotConfigurationDto, UpdateBotConfigurationRequest} from '@/types/bots'

export function getBots(signal?: AbortSignal) {
  return apiRequest<BotConfigurationDto[]>('/bots', { method: 'GET', signal })
}

export function getBot(botName: string, signal?: AbortSignal) {
  return apiRequest<BotConfigurationDto | null>(`/bots/${encodeURIComponent(botName)}`, { method: 'GET', signal})
}

export function updateBotConfiguration(botName: string, request: UpdateBotConfigurationRequest) {
  return apiRequest<BotConfigurationDto>(`/bots/${encodeURIComponent(botName)}/configuration`, { method: 'PUT', body: JSON.stringify(request)})
}

export function sendBotCommand(botName: string, request: BotCommandRequest) {
  return apiRequest<BotCommandDto>(`/bots/${encodeURIComponent(botName)}/commands`, { method: 'POST', headers: { 'X-Idempotency-Key': crypto.randomUUID() }, body: JSON.stringify(request)})
}

export function getBotCommands(botName: string, take = 25, signal?: AbortSignal,) {
  const query = new URLSearchParams({botName, take: String(take)})

  return apiRequest<BotCommandDto[]>(`/commands?${query}`, { method: 'GET', signal})
}
