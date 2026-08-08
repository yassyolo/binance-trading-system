import { apiConfig } from '@/api/api-config'
import { ApiError } from '@/api/api-error'
import { getAccessToken } from '@/api/auth-token'
import type { ProblemDetails } from '@/api/problem-details'

interface ApiRequestOptions extends RequestInit {
  authenticated?: boolean
}

export async function apiRequest<T>(path: string, options: ApiRequestOptions = {}): Promise<T> {
  const { authenticated = true, headers, ...requestInit } = options
  const requestHeaders = new Headers(headers)
  const token = authenticated ? getAccessToken() : null
  const correlationId = crypto.randomUUID()

  requestHeaders.set('Accept', 'application/json')
  requestHeaders.set('X-Correlation-ID', correlationId)

  if (requestInit.body && !requestHeaders.has('Content-Type'))
    requestHeaders.set('Content-Type', 'application/json')

  if (token)
    requestHeaders.set('Authorization', `Bearer ${token}`)

  const response = await fetch(`${apiConfig.baseUrl}${path}`, {
    ...requestInit,
    headers: requestHeaders,
  })

  const responseCorrelationId = response.headers.get('X-Correlation-ID') ?? correlationId

  if (!response.ok) {
    let problem: ProblemDetails | null = null

    try {
      problem = await response.json() as ProblemDetails
    } catch {
      problem = null
    }

    throw new ApiError(response.status, problem, responseCorrelationId)
  }

  if (response.status === 204)
    return undefined as T

  const contentLength = response.headers.get('content-length')
  if (contentLength === '0')
    return undefined as T

  return await response.json() as T
}
