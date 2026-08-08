import type { ReactNode } from 'react'

interface PageSectionProps {
  title: string
  description?: string
  actions?: ReactNode
  children: ReactNode
}

export function PageSection({ title, description, actions, children }: PageSectionProps) {
  return (
    <section>
      <div className="mb-4 flex items-end justify-between gap-6">
        <div>
          <h2 className="text-sm font-semibold tracking-tight">{title}</h2>
          {description && <p className="mt-1 text-xs leading-5 text-[var(--color-text-muted)]">{description}</p>}
        </div>
        {actions}
      </div>
      {children}
    </section>
  )
}
