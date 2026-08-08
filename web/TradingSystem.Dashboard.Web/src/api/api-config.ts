const configuredBaseUrl = import.meta.env.VITE_API_BASE_URL as string | undefined

export const apiConfig = {
  baseUrl: configuredBaseUrl?.replace(/\/+$/, '') ?? 'http://localhost:53304/api/v1',
} as const
