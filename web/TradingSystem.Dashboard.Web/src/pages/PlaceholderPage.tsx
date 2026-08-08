import { ArrowUpRight } from 'lucide-react'

import { PageHeader } from '@/components/layout/PageHeader'

interface PlaceholderPageProps {
  title: string
  description: string
}

export function PlaceholderPage({ title, description }: PlaceholderPageProps) {
  return (
    <>
      <PageHeader title={title} description={description} />

      <main className="p-8">
        <section className="min-h-[260px] rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] p-6 shadow-[0_16px_55px_rgba(0,0,0,0.16)]">
          <div className="flex size-10 items-center justify-center rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-strong)] text-[var(--color-text-secondary)]">
            <ArrowUpRight size={18} />
          </div>

          <h2 className="mt-8 text-lg font-medium">{title} module</h2>
          <p className="mt-2 max-w-xl text-sm leading-6 text-[var(--color-text-secondary)]">
            The application shell and navigation are ready. This module will be implemented in the next feature packages.
          </p>
        </section>
      </main>
    </>
  )
}
