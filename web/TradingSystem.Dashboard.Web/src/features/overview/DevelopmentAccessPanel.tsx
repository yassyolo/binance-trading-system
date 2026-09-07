import { useState } from 'react'
import { KeyRound, Trash2 } from 'lucide-react'

import { clearAccessToken, getAccessToken, setAccessToken } from '@/api/auth-token'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'

interface DevelopmentAccessPanelProps {
  onSaved: () => void
}

export function DevelopmentAccessPanel({ onSaved }: DevelopmentAccessPanelProps) {
  const [token, setToken] = useState(getAccessToken() ?? '')

  function save() {
    setAccessToken(token)
    onSaved()
  }

  function clear() {
    clearAccessToken()
    setToken('')
    onSaved()
  }

  return (
    <Card className="max-w-3xl p-6">
      <div className="flex items-start gap-4">
        <div className="grid size-10 shrink-0 place-items-center rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-strong)] text-[var(--color-text-secondary)]">
          <KeyRound size={18} />
        </div>

        <div className="min-w-0 flex-1">
          <h2 className="text-sm font-semibold">Development API access</h2>
          <p className="mt-2 text-sm leading-6 text-[var(--color-text-secondary)]">
            Dashboard API requires a Viewer, Operator or Administrator JWT. Paste a development token here.
            This temporary mechanism stores the token in localStorage and will be replaced by the final authentication flow.
          </p>

          <textarea value={token} onChange={(event) => setToken(event.target.value)} spellCheck={false} placeholder="Paste JWT token" className="mt-5 min-h-28 w-full resize-y rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] px-3 py-3 font-mono text-xs leading-5 text-[var(--color-text-primary)] outline-none transition focus:border-[#444850]"/>

          <div className="mt-4 flex gap-2">
            <Button variant="primary" onClick={save}>Save token and retry</Button>
            <Button leftIcon={<Trash2 size={15} />} onClick={clear}>Clear</Button>
          </div>
        </div>
      </div>
    </Card>
  )
}
