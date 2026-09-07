
import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode,} from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { authenticationExpiredEvent, clearAccessToken, getAccessToken, setAccessToken } from '@/api/auth-token'
import { getCurrentDashboardUser, loginDashboard } from '@/features/auth/auth.api'
import type { DashboardLoginRequest,DashboardUser } from '@/features/auth/auth.types'

type AuthenticationStatus =
  | 'loading'
  | 'authenticated'
  | 'anonymous'

interface AuthenticationContextValue {
  status: AuthenticationStatus
  user: DashboardUser | null
  login: (request: DashboardLoginRequest) => Promise<void>
  logout: () => void
}

const AuthenticationContext = createContext<AuthenticationContextValue | null>(null)

export function AuthenticationProvider({
  children,
}: {
  children: ReactNode
}) {
  const queryClient = useQueryClient()
  const [status, setStatus] =
    useState<AuthenticationStatus>(
      getAccessToken()
        ? 'loading'
        : 'anonymous',
    )
  const [user, setUser] =
    useState<DashboardUser | null>(null)

  const becomeAnonymous =
    useCallback(() => {
      clearAccessToken()
      setUser(null)
      setStatus('anonymous')
      queryClient.clear()
    }, [queryClient])

  useEffect(() => {
    function onExpired() {
      becomeAnonymous()
    }

    window.addEventListener(
      authenticationExpiredEvent,
      onExpired,
    )

    return () => {
      window.removeEventListener(
        authenticationExpiredEvent,
        onExpired,
      )
    }
  }, [becomeAnonymous])

  useEffect(() => {
    if (!getAccessToken()) {
      setStatus('anonymous')
      return
    }

    const controller = new AbortController()

    void getCurrentDashboardUser(
      controller.signal,
    )
      .then((currentUser) => {
        setUser(currentUser)
        setStatus('authenticated')
      })
      .catch(() => {
        if (!controller.signal.aborted)
          becomeAnonymous()
      })

    return () => controller.abort()
  }, [becomeAnonymous])

  const login =
    useCallback(
      async (
        request: DashboardLoginRequest,
      ) => {
        const response =
          await loginDashboard(request)

        setAccessToken(response.accessToken)

        setUser({
          userName: response.userName,
          displayName: response.displayName,
          roles: response.roles,
        })

        setStatus('authenticated')
        queryClient.clear()
      },
      [queryClient],
    )

  const value = useMemo(
    () => ({
      status,
      user,
      login,
      logout: becomeAnonymous,
    }),
    [
      status,
      user,
      login,
      becomeAnonymous,
    ],
  )

  return <AuthenticationContext.Provider value={value}> {children} </AuthenticationContext.Provider>
}

export function useAuthentication() {
  const context = useContext(AuthenticationContext)

  if (!context)
    throw new Error('useAuthentication must be used inside AuthenticationProvider.',)

  return context
}
