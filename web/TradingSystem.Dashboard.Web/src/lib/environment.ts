export const appEnvironment = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7001/api/v1',
  mode: import.meta.env.MODE,
  isDevelopment: import.meta.env.DEV,
} as const
