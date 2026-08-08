import { useEffect } from 'react'
import { TriangleAlert, X } from 'lucide-react'

import { Button } from '@/components/ui/Button'
import { IconButton } from '@/components/ui/IconButton'

interface ConfirmActionDialogProps {
  open: boolean
  title: string
  description: string
  confirmLabel: string
  dangerous?: boolean
  busy?: boolean
  onConfirm: () => void
  onClose: () => void
}

export function ConfirmActionDialog({
  open,
  title,
  description,
  confirmLabel,
  dangerous = false,
  busy = false,
  onConfirm,
  onClose,
}: ConfirmActionDialogProps) {
  useEffect(() => {
    if (!open) return

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !busy) onClose()
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [busy, onClose, open])

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-black/65 px-6">
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="confirmation-dialog-title"
        className="w-full max-w-[480px] rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] p-6 shadow-[0_30px_90px_rgba(0,0,0,0.55)]"
      >
        <div className="flex items-start justify-between gap-4">
          <div className="grid size-10 place-items-center rounded-xl bg-[rgba(251,191,36,0.08)] text-[var(--color-warning)]">
            <TriangleAlert size={19} />
          </div>
          <IconButton icon={<X size={17} />} label="Close dialog" onClick={onClose} disabled={busy} />
        </div>

        <h2 id="confirmation-dialog-title" className="mt-6 text-lg font-semibold">{title}</h2>
        <p className="mt-2 text-sm leading-6 text-[var(--color-text-secondary)]">{description}</p>

        <div className="mt-7 flex justify-end gap-2">
          <Button onClick={onClose} disabled={busy}>Cancel</Button>
          <Button variant={dangerous ? 'danger' : 'primary'} onClick={onConfirm} disabled={busy}>
            {busy ? 'Working…' : confirmLabel}
          </Button>
        </div>
      </div>
    </div>
  )
}
