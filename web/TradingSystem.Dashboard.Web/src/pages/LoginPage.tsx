
import {
  LockKeyhole,
  Orbit,
} from 'lucide-react'
import {
  useState,
  type FormEvent,
} from 'react'
import {
  Navigate,
  useLocation,
  useNavigate,
} from 'react-router'

import { ApiError } from '@/api/api-error'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { useAuthentication } from '@/features/auth/auth-context'

interface LoginLocationState {
  from?: string
}

export function LoginPage() {
  const authentication = useAuthentication()
  const location = useLocation()
  const navigate = useNavigate()

  const [userName, setUserName] =
    useState('')
  const [password, setPassword] =
    useState('')
  const [error, setError] =
    useState<string | null>(null)
  const [submitting, setSubmitting] =
    useState(false)

  if (
    authentication.status ===
    'authenticated'
  ) {
    return (
      <Navigate
        to="/overview"
        replace
      />
    )
  }

  async function submit(
    event: FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)

    try {
      await authentication.login({
        userName,
        password,
      })

      const state =
        location.state as
          | LoginLocationState
          | null

      navigate(
        state?.from ?? '/overview',
        { replace: true },
      )
    } catch (caught) {
      const apiError =
        caught instanceof ApiError
          ? caught
          : null

      setError(
        apiError?.status === 401
          ? 'Invalid dashboard user name or password.'
          : apiError?.message ??
              'Dashboard login failed.',
      )
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="grid min-h-screen place-items-center bg-[var(--color-app)] px-8 text-[var(--color-text-primary)]">
      <Card className="w-full max-w-md overflow-hidden">
        <div className="border-b border-[var(--color-border)] p-7">
          <div className="flex items-center gap-4">
            <div className="grid size-11 place-items-center rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface-strong)]">
              <Orbit size={21} />
            </div>
            <div>
              <h1 className="text-lg font-semibold">
                Trading System
              </h1>
              <p className="mt-1 text-sm text-[var(--color-text-muted)]">
                Operations Dashboard
              </p>
            </div>
          </div>
        </div>

        <form
          onSubmit={submit}
          className="space-y-5 p-7"
        >
          <div>
            <div className="mb-5 flex items-start gap-3">
              <LockKeyhole
                className="mt-0.5 text-[var(--color-text-secondary)]"
                size={18}
              />
              <div>
                <h2 className="text-sm font-semibold">
                  Operator sign in
                </h2>
                <p className="mt-1 text-xs leading-5 text-[var(--color-text-muted)]">
                  Authenticate before viewing or controlling the trading system.
                </p>
              </div>
            </div>

            <label className="block text-xs font-medium text-[var(--color-text-secondary)]">
              User name
            </label>
            <input
              value={userName}
              onChange={(event) =>
                setUserName(
                  event.target.value,
                )
              }
              autoComplete="username"
              autoFocus
              required
              maxLength={100}
              className="mt-2 h-11 w-full rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] px-3 text-sm outline-none transition focus:border-[#444850]"
            />
          </div>

          <div>
            <label className="block text-xs font-medium text-[var(--color-text-secondary)]">
              Password
            </label>
            <input
              type="password"
              value={password}
              onChange={(event) =>
                setPassword(
                  event.target.value,
                )
              }
              autoComplete="current-password"
              required
              maxLength={256}
              className="mt-2 h-11 w-full rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] px-3 text-sm outline-none transition focus:border-[#444850]"
            />
          </div>

          {error && (
            <div className="rounded-xl border border-[rgba(248,113,113,0.25)] bg-[rgba(248,113,113,0.08)] px-3 py-2.5 text-xs text-[var(--color-danger)]">
              {error}
            </div>
          )}

          <Button
            type="submit"
            variant="primary"
            className="w-full"
            disabled={submitting}
          >
            {submitting
              ? 'Signing in…'
              : 'Sign in'}
          </Button>

          <p className="text-center text-[11px] leading-5 text-[var(--color-text-muted)]">
            Production access should be used through HTTPS. Never paste API keys or signing secrets into the browser.
          </p>
        </form>
      </Card>
    </main>
  )
}
