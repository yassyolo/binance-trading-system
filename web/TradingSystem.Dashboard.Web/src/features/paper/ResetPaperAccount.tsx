import { useState } from 'react'
import { RotateCcw } from 'lucide-react'

import { ApiError } from '@/api/api-error'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { ConfirmActionDialog } from '@/components/ui/ConfirmActionDialog'
import { ErrorState } from '@/components/ui/ErrorState'
import { useResetPaperTrading } from '@/features/paper/paper.queries'

export function ResetPaperAccount() {
  const [open, setOpen] = useState(false)
  const mutation = useResetPaperTrading()
  const error = mutation.error instanceof ApiError ? mutation.error : null

  return (
    <>
      <Card className="border-[rgba(248,113,113,0.16)] p-6">
        <div className="flex items-start justify-between gap-6">
          <div>
            <h3 className="text-sm font-semibold">Reset paper account</h3>
            <p className="mt-2 max-w-2xl text-xs leading-5 text-[var(--color-text-muted)]">
              Archives the current simulated positions. The configured backend initial balance is used again after reset.
            </p>
          </div>
          <Button variant="danger" leftIcon={<RotateCcw size={15} />} onClick={() => setOpen(true)}>
            Reset account
          </Button>
        </div>

        {error && (
          <div className="mt-5">
            <ErrorState title="Paper account reset failed" description={error.message} />
          </div>
        )}
      </Card>

      <ConfirmActionDialog
        open={open}
        title="Reset paper trading account?"
        description="Current non-archived paper positions will be archived. Live Binance state is not touched."
        confirmLabel="Reset paper account"
        dangerous
        busy={mutation.isPending}
        onClose={() => {
          if (!mutation.isPending) setOpen(false)
        }}
        onConfirm={() =>
          mutation.mutate(undefined, {
            onSuccess: () => setOpen(false),
          })
        }
      />
    </>
  )
}
