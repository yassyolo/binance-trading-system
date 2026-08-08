import { apiRequest } from '@/api/api-client'
import type {
  AlertDto,
  AuditEventDto,
  AuditQuery,
  ComponentHealthDto,
} from '@/types/operations'

export function getComponentHealth(signal?: AbortSignal) {
  return apiRequest<ComponentHealthDto[]>('/health/components', {
    method: 'GET',
    signal,
  })
}

export function getAlerts(
  acknowledged: boolean,
  take: number,
  signal?: AbortSignal,
) {
  const parameters = new URLSearchParams({
    acknowledged: String(acknowledged),
    take: String(take),
  })

  return apiRequest<AlertDto[]>(`/alerts?${parameters}`, {
    method: 'GET',
    signal,
  })
}

export function acknowledgeAlert(alertId: number) {
  return apiRequest<void>(
    `/alerts/${alertId}/acknowledge`,
    {
      method: 'POST',
      headers: {
        'X-Idempotency-Key': crypto.randomUUID(),
      },
    },
  )
}

export function getAuditEvents(
  query: AuditQuery,
  signal?: AbortSignal,
) {
  const parameters = new URLSearchParams({
    skip: String(query.skip),
    take: String(query.take),
  })

  if (query.actor?.trim())
    parameters.set('actor', query.actor.trim())

  if (query.action?.trim())
    parameters.set('action', query.action.trim())

  if (query.fromUtc)
    parameters.set('fromUtc', query.fromUtc)

  if (query.toUtc)
    parameters.set('toUtc', query.toUtc)

  return apiRequest<AuditEventDto[]>(`/audit?${parameters}`, {
    method: 'GET',
    signal,
  })
}
