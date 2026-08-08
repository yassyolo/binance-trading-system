import type { ButtonHTMLAttributes, ReactNode } from 'react'

import { cn } from '@/lib/cn'

interface IconButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  icon: ReactNode
  label: string
}

export function IconButton({ icon, label, className, type = 'button', ...props }: IconButtonProps) {
  return (
    <button
      type={type}
      title={label}
      aria-label={label}
      className={cn(
        'grid size-9 place-items-center rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-strong)] text-[var(--color-text-secondary)] transition hover:bg-[var(--color-surface-hover)] hover:text-[var(--color-text-primary)]',
        className,
      )}
      {...props}
    >
      {icon}
    </button>
  )
}
