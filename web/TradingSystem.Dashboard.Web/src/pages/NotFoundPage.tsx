import { ArrowLeft } from 'lucide-react'
import { Link } from 'react-router'

import { PageHeader } from '@/components/layout/PageHeader'

export function NotFoundPage() {
  return (
    <>
      <PageHeader title="Page not found" description="The requested dashboard route does not exist." />

      <main className="p-8">
        <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] p-8">
          <p className="text-sm text-[var(--color-text-secondary)]">Error 404</p>
          <Link
            to="/overview"
            className="mt-5 inline-flex items-center gap-2 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-strong)] px-4 py-2 text-sm transition hover:bg-[var(--color-surface-hover)]"
          >
            <ArrowLeft size={16} />
            Back to overview
          </Link>
        </div>
      </main>
    </>
  )
}
