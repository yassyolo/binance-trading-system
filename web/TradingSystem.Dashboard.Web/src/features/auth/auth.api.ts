
import { apiRequest } from '@/api/api-client'
import type {
  DashboardLoginRequest,
  DashboardLoginResponse,
  DashboardUser,
} from '@/features/auth/auth.types'

export function loginDashboard(
  request: DashboardLoginRequest,
) {
  return apiRequest<DashboardLoginResponse>(
    '/auth/login',
    {
      method: 'POST',
      authenticated: false,
      body: JSON.stringify(request),
    },
  )
}

export function getCurrentDashboardUser(
  signal?: AbortSignal,
) {
  return apiRequest<DashboardUser>(
    '/auth/me',
    {
      method: 'GET',
      signal,
    },
  )
}
