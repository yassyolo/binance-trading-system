import { apiRequest } from '@/api/api-client'
import type { LiveOverviewDto } from '@/types/dashboard-overview'

export function getOverview(signal?: AbortSignal) {
  return apiRequest<LiveOverviewDto>('/overview', {
    method: 'GET',
    signal,
  })
}
