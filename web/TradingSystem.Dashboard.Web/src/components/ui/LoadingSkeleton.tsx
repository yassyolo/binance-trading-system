import { cn } from '@/lib/cn'

interface LoadingSkeletonProps {
  className?: string
}

export function LoadingSkeleton({ className }: LoadingSkeletonProps) {
  return <div className={cn('animate-pulse rounded-lg bg-[var(--color-surface-strong)]', className)} />
}
