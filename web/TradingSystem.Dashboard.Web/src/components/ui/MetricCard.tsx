import type { ReactNode } from 'react'
import { ArrowDownRight, ArrowUpRight } from 'lucide-react'

import { Card } from '@/components/ui/Card'
import { cn } from '@/lib/cn'

interface MetricCardProps {
  label: string
  value: string
  helper?: string
  icon?: ReactNode
  trend?: 'up' | 'down' | 'neutral'
}

export function MetricCard({ label, value, helper, icon, trend = 'neutral' }: MetricCardProps) {
  const positive = trend === 'up'
  const negative = trend === 'down'

  return (
    <Card className="p-5">
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="text-xs font-medium uppercase tracking-[0.12em] text-[var(--color-text-muted)]">{label}</p>
          <p className="mt-3 text-2xl font-semibold tracking-tight">{value}</p>
        </div>

        {icon && (
          <div className="grid size-9 place-items-center rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-strong)] text-[var(--color-text-secondary)]">
            {icon}
          </div>
        )}
      </div>

      {helper && (
        <div
          className={cn(
            'mt-5 flex items-center gap-1 text-xs',
            positive && 'text-[var(--color-success)]',
            negative && 'text-[var(--color-danger)]',
            trend === 'neutral' && 'text-[var(--color-text-muted)]',
          )}
        >
          {positive && <ArrowUpRight size={14} />}
          {negative && <ArrowDownRight size={14} />}
          <span>{helper}</span>
        </div>
      )}
    </Card>
  )
}
