import type { HTMLAttributes, ReactNode } from 'react'

import { cn } from '@/lib/cn'

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode
}

export function Card({ className, children, ...props }: CardProps) {
  return (
    <div className={cn('rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] shadow-[0_16px_55px_rgba(0,0,0,0.16)]', className,)}
      {...props}
    >
      {children}
    </div>
  )
}
