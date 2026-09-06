import { Circle } from 'lucide-react'

interface PageHeaderProps {
  title: string
  description?: string
}

export function PageHeader({ title, description }: PageHeaderProps) {
  return (
    <header className="flex min-h-[88px] items-center justify-between border-b border-[var(--color-border)] px-8">
      <div>
        <h1 className="text-xl font-semibold tracking-tight">{title}</h1>
        {description && ( <p className="mt-1 text-sm text-[var(--color-text-secondary)]">{description}</p>)}
      </div>

      <div className="flex items-center gap-2 rounded-full border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-1.5 text-xs text-[var(--color-text-secondary)]">
        <Circle className="fill-[var(--color-success)] text-[var(--color-success)]" size={8} />
        Development
      </div>
    </header>
  )
}
