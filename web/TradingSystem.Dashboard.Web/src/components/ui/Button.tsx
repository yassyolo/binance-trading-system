import type { ButtonHTMLAttributes, ReactNode } from 'react'

import { cn } from '@/lib/cn'

type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'ghost'
type ButtonSize = 'sm' | 'md'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
  size?: ButtonSize
  leftIcon?: ReactNode
}

const variantClasses: Record<ButtonVariant, string> = {
  primary: 'border-transparent bg-[var(--color-text-primary)] text-[#111214] hover:bg-white disabled:bg-[#44474f] disabled:text-[#8a8d95]',
  secondary: 'border-[var(--color-border)] bg-[var(--color-surface-strong)] text-[var(--color-text-primary)] hover:bg-[var(--color-surface-hover)]',
  danger: 'border-[rgba(248,113,113,0.25)] bg-[rgba(248,113,113,0.10)] text-[var(--color-danger)] hover:bg-[rgba(248,113,113,0.16)]',
  ghost: 'border-transparent bg-transparent text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-hover)] hover:text-[var(--color-text-primary)]',
}

const sizeClasses: Record<ButtonSize, string> = {
  sm: 'h-8 px-3 text-xs',
  md: 'h-10 px-4 text-sm',
}

export function Button({
  className,
  variant = 'secondary',
  size = 'md',
  leftIcon,
  children,
  type = 'button',
  ...props
}: ButtonProps) {
  return (
    <button type={type}
      className={cn(
        'inline-flex items-center justify-center gap-2 rounded-xl border font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-60',
        variantClasses[variant],
        sizeClasses[size],
        className,
      )}
      {...props}
    >
      {leftIcon}
      {children}
    </button>
  )
}
