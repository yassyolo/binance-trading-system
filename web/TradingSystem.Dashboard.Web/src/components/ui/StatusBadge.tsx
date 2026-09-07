import { Circle } from 'lucide-react'
import { cn } from '@/lib/cn'

export type StatusTone = 'success' | 'warning' | 'danger' | 'info' | 'neutral'

interface StatusBadgeProps {
  label: string
  tone?: StatusTone
  showDot?: boolean
}

const toneClasses: Record<StatusTone, string> = {
  success: 'border-[rgba(74,222,128,0.18)] bg-[rgba(74,222,128,0.08)] text-[var(--color-success)]',
  warning: 'border-[rgba(251,191,36,0.18)] bg-[rgba(251,191,36,0.08)] text-[var(--color-warning)]',
  danger: 'border-[rgba(248,113,113,0.18)] bg-[rgba(248,113,113,0.08)] text-[var(--color-danger)]',
  info: 'border-[rgba(96,165,250,0.18)] bg-[rgba(96,165,250,0.08)] text-[var(--color-info)]',
  neutral: 'border-[var(--color-border)] bg-[var(--color-surface-strong)] text-[var(--color-text-secondary)]',
}

export function StatusBadge({ label, tone = 'neutral', showDot = true }: StatusBadgeProps) {
  return (
    <span className={cn('inline-flex h-7 items-center gap-2 rounded-full border px-2.5 text-xs font-medium', toneClasses[tone],)} >
      {showDot && <Circle className="fill-current" size={7} />}
      {label}
    </span>
  )
}
