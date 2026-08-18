import { useState } from 'react'
import type { ReactNode } from 'react'
import {
  CirclePlay,
  Octagon,
  Pause,
  Play,
  ShieldAlert,
  Square,
} from 'lucide-react'

import { ApiError } from '@/api/api-error'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { ConfirmActionDialog } from '@/components/ui/ConfirmActionDialog'
import { ErrorState } from '@/components/ui/ErrorState'
import { botCommandValues } from '@/features/bots/bot-enums'
import { useSendBotCommand } from '@/features/bots/bots.queries'

interface PendingCommand {
  command: number
  label: string
  description: string
  dangerous: boolean
}

interface BotCommandPanelProps {
  botName: string
}

const actions: Array<PendingCommand & { icon: ReactNode }> = [
  {
    command: botCommandValues.Start,
    label: 'Start',
    description: 'Start runtime execution.',
    dangerous: false,
    icon: <CirclePlay size={15} />,
  },
  {
    command: botCommandValues.Resume,
    label: 'Resume',
    description: 'Resume a paused bot.',
    dangerous: false,
    icon: <Play size={15} />,
  },
  {
    command: botCommandValues.Pause,
    label: 'Pause',
    description: 'Pause new execution while keeping runtime state.',
    dangerous: false,
    icon: <Pause size={15} />,
  },
  {
    command: botCommandValues.Stop,
    label: 'Stop',
    description: 'Stop runtime execution.',
    dangerous: false,
    icon: <Square size={15} />,
  },
  {
    command: botCommandValues.EmergencyStop,
    label: 'Emergency stop',
    description: 'Immediately transition the bot to EmergencyStopped.',
    dangerous: true,
    icon: <Octagon size={15} />,
  },
]

export function BotCommandPanel({ botName }: BotCommandPanelProps) {
  const [pending, setPending] = useState<PendingCommand | null>(null)
  const [reason, setReason] = useState('')
  const [cancelOpenOrders, setCancelOpenOrders] = useState(false)
  const [closeOpenPositions, setCloseOpenPositions] = useState(false)
  const mutation = useSendBotCommand(botName)
  const error = mutation.error instanceof ApiError ? mutation.error : null

  function resetDialog() {
    setPending(null)
    setReason('')
    setCancelOpenOrders(false)
    setCloseOpenPositions(false)
  }

  function execute() {
    if (!pending || !reason.trim()) return

    mutation.mutate(
      {
        command: pending.command,
        reason: reason.trim(),
        confirmed: pending.dangerous,
        cancelOpenOrders,
        closeOpenPositions,
        positionId: null,
      },
      {
        onSuccess: resetDialog,
      },
    )
  }

  return (
    <>
      <Card className="p-6">
        <div className="flex items-start justify-between gap-6">
          <div>
            <h3 className="text-sm font-semibold">Runtime commands</h3>
            <p className="mt-1 text-xs leading-5 text-[var(--color-text-muted)]">
              Commands are queued by Dashboard API and processed asynchronously by StrategyService.
            </p>
          </div>

          <ShieldAlert size={18} className="text-[var(--color-text-muted)]" />
        </div>

        {error && !pending && (
          <div className="mt-5">
            <ErrorState title="Command failed" description={error.message} />
          </div>
        )}

        <div className="mt-6 flex flex-wrap gap-2">
          {actions.map((action) => (
            <Button
              key={action.label}
              variant={action.dangerous ? 'danger' : 'secondary'}
              leftIcon={action.icon}
              onClick={() => {
                mutation.reset()
                setReason('')
                setCancelOpenOrders(false)
                setCloseOpenPositions(false)
                setPending(action)
              }}
            >
              {action.label}
            </Button>
          ))}
        </div>
      </Card>

      <ConfirmActionDialog
        open={pending !== null}
        title={`${pending?.label ?? 'Command'} ${botName}?`}
        description={pending?.description ?? ''}
        confirmLabel={pending?.label ?? 'Confirm'}
        dangerous={pending?.dangerous}
        busy={mutation.isPending}
        confirmDisabled={!reason.trim()}
        onClose={() => {
          if (!mutation.isPending) resetDialog()
        }}
        onConfirm={execute}
      >
        <div>
          <label className="block">
            <span className="mb-2 block text-xs font-medium text-[var(--color-text-secondary)]">
              Reason
            </span>

            <input
              autoFocus
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              placeholder="Required command reason"
              className="h-10 w-full rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] px-3 text-sm outline-none focus:border-[#454954]"
            />
          </label>

          {(pending?.command === botCommandValues.Stop ||
            pending?.command === botCommandValues.EmergencyStop) && (
            <div className="mt-4 flex flex-wrap gap-5 text-xs text-[var(--color-text-secondary)]">
              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={cancelOpenOrders}
                  onChange={(event) => setCancelOpenOrders(event.target.checked)}
                  className="accent-white"
                />
                Cancel open orders
              </label>

              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={closeOpenPositions}
                  onChange={(event) => setCloseOpenPositions(event.target.checked)}
                  className="accent-white"
                />
                Close open positions
              </label>
            </div>
          )}

          {error && (
            <p className="mt-4 text-xs leading-5 text-[var(--color-danger)]">
              {error.message}
            </p>
          )}
        </div>
      </ConfirmActionDialog>
    </>
  )
}
