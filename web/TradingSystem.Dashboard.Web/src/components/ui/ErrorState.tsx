import type { ReactNode } from 'react'
import { CircleAlert } from 'lucide-react'
import { Card } from '@/components/ui/Card'

interface ErrorStateProps {
  title?: string
  description: string
  action?: ReactNode
}

export function ErrorState({ title = 'Something went wrong', description, action }: ErrorStateProps) {
  return (
    <Card className="border-[rgba(248,113,113,0.18)] p-6">
      <div className="flex items-start gap-4">
        <div className="grid size-10 shrink-0 place-items-center rounded-xl bg-[rgba(248,113,113,0.08)] text-[var(--color-danger)]">
          <CircleAlert size={19} />
        </div>
        <div className="min-w-0">
          <h3 className="text-sm font-semibold">{title}</h3>
          <p className="mt-1 text-sm leading-6 text-[var(--color-text-secondary)]">{description}</p>
          {action && <div className="mt-4">{action}</div>}
        </div>
      </div>
    </Card>
  )
}
