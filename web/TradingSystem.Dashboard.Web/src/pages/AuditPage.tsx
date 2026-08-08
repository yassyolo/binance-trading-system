import { useState } from 'react'
import { RefreshCcw } from 'lucide-react'

import { ApiError } from '@/api/api-error'
import { PageHeader } from '@/components/layout/PageHeader'
import { Button } from '@/components/ui/Button'
import { EmptyState } from '@/components/ui/EmptyState'
import { ErrorState } from '@/components/ui/ErrorState'
import { LoadingSkeleton } from '@/components/ui/LoadingSkeleton'
import { PageSection } from '@/components/ui/PageSection'
import { AuditEventCard } from '@/features/operations/AuditEventCard'
import { AuditFilters } from '@/features/operations/AuditFilters'
import { useAuditEvents } from '@/features/operations/operations.queries'
import type { AuditQuery } from '@/types/operations'

const take = 50

export function AuditPage() {
  const [queryState, setQueryState] =
    useState<AuditQuery>({
      skip: 0,
      take,
    })

  const query = useAuditEvents(queryState)

  const error =
    query.error instanceof ApiError
      ? query.error
      : null

  return (
    <>
      <PageHeader
        title="Audit"
        description="Who changed or controlled the system, when it happened and why."
      />

      <main className="space-y-6 p-8">
        <AuditFilters
          take={take}
          onApply={setQueryState}
        />

        <PageSection
          title="Audit events"
          description="Operator and Administrator access only. Sensitive values must already be redacted by the backend."
          actions={
            <Button
              size="sm"
              leftIcon={
                <RefreshCcw
                  size={14}
                  className={
                    query.isFetching
                      ? 'animate-spin'
                      : ''
                  }
                />
              }
              onClick={() => void query.refetch()}
            >
              Refresh
            </Button>
          }
        >
          {query.isPending && (
            <div className="space-y-3">
              {Array.from({ length: 6 }, (_, index) => (
                <LoadingSkeleton
                  key={index}
                  className="h-44"
                />
              ))}
            </div>
          )}

          {query.isError && (
            <ErrorState
              title={
                error?.status === 403
                  ? 'Operator access required'
                  : 'Audit events unavailable'
              }
              description={
                error?.message ?? query.error.message
              }
              action={
                <Button onClick={() => void query.refetch()}>
                  Retry
                </Button>
              }
            />
          )}

          {query.data && query.data.length === 0 && (
            <EmptyState
              title="No audit events found"
              description="No audit records match the selected actor, action and time range."
            />
          )}

          {query.data && query.data.length > 0 && (
            <>
              <div className="space-y-3">
                {query.data.map((event) => (
                  <AuditEventCard
                    key={event.auditId}
                    event={event}
                  />
                ))}
              </div>

              <div className="mt-5 flex items-center justify-between">
                <p className="text-xs text-[var(--color-text-muted)]">
                  Page{' '}
                  {Math.floor(
                    queryState.skip /
                      queryState.take,
                  ) + 1}
                  {' · '}
                  {query.data.length} event
                  {query.data.length === 1 ? '' : 's'}
                </p>

                <div className="flex gap-2">
                  <Button
                    size="sm"
                    disabled={
                      queryState.skip === 0 ||
                      query.isFetching
                    }
                    onClick={() =>
                      setQueryState((current) => ({
                        ...current,
                        skip: Math.max(
                          0,
                          current.skip - current.take,
                        ),
                      }))
                    }
                  >
                    Previous
                  </Button>

                  <Button
                    size="sm"
                    disabled={
                      query.isFetching ||
                      query.data.length <
                        queryState.take
                    }
                    onClick={() =>
                      setQueryState((current) => ({
                        ...current,
                        skip:
                          current.skip +
                          current.take,
                      }))
                    }
                  >
                    Next
                  </Button>
                </div>
              </div>
            </>
          )}
        </PageSection>
      </main>
    </>
  )
}
