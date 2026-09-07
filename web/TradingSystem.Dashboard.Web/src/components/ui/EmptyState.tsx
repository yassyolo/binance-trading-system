import type { ReactNode } from 'react'
import { Inbox } from 'lucide-react'
import { Card } from '@/components/ui/Card'

interface EmptyStateProps {
  title: string
  description: string
  action?: ReactNode
}

export function EmptyState({ title, description, action }: EmptyStateProps) {
  return (
    <Card className="grid min-h-[230px] place-items-center p-8 text-center">
      <div>
        <div className="mx-auto grid size-11 place-items-center rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-strong)] text-[var(--color-text-secondary)]">
          <Inbox size={19} />
        </div>
        <h3 className="mt-5 text-sm font-semibold">{title}</h3>
        <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-[var(--color-text-secondary)]">{description}</p>
        {action && <div className="mt-5">{action}</div>}
      </div>
    </Card>
  )
}
