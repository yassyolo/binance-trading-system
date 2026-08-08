interface ProgressBarProps {
  value: number
  showLabel?: boolean
}

export function ProgressBar({ value, showLabel = false }: ProgressBarProps) {
  const normalized = Math.max(0, Math.min(100, value))

  return (
    <div>
      <div className="h-2 overflow-hidden rounded-full bg-[var(--color-surface-strong)]">
        <div
          className="h-full rounded-full bg-[var(--color-text-primary)] transition-[width] duration-300"
          style={{ width: `${normalized}%` }}
        />
      </div>
      {showLabel && <p className="mt-2 text-right text-xs text-[var(--color-text-muted)]">{normalized}%</p>}
    </div>
  )
}
