import { QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router'

import { queryClient } from '@/app/query-client'
import { router } from '@/app/router'
import { AuthenticationProvider } from '@/features/auth/auth-context'

export function AppProviders() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthenticationProvider>
        <RouterProvider router={router} />
      </AuthenticationProvider>
    </QueryClientProvider>
  )
}
