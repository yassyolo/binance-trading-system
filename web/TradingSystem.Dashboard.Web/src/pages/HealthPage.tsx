import {
  Activity,
  CircleCheck,
  CircleX,
  RefreshCcw,
  Server,
} from 'lucide-react'

import { PageHeader } from '@/components/layout/PageHeader'
import { Button } from '@/components/ui/Button'
import { DataTable, type DataTableColumn } from '@/components/ui/DataTable'
import { EmptyState } from '@/components/ui/EmptyState'
import { ErrorState } from '@/components/ui/ErrorState'
import { LoadingSkeleton } from '@/components/ui/LoadingSkeleton'
import { MetricCard } from '@/components/ui/MetricCard'
import { PageSection } from '@/components/ui/PageSection'
import { StatusBadge } from '@/components/ui/StatusBadge'
import { HealthCards } from '@/features/operations/HealthCards'
import {
  healthTone,
  relativeTime,
  utcDate,
} from '@/features/operations/operations-formatters'
import { useComponentHealth } from '@/features/operations/operations.queries'
import type { ComponentHealthDto } from '@/types/operations'

const columns: DataTableColumn<ComponentHealthDto>[] = [
  {
    key: 'component',
    header: 'Component',
    render: (component) => (
      <span className="font-medium text-[var(--color-text-primary)]">
        {component.component}
      </span>
    ),
  },
  {
    key: 'status',
    header: 'Status',
    render: (component) => (
      <StatusBadge
        label={component.status}
        tone={healthTone(component.status)}
      />
    ),
  },
  {
    key: 'lastSeen',
    header: 'Last seen',
    render: (component) => (
      <div>
        <p>{relativeTime(component.lastSeenUtc)}</p>
        <p className="mt-1 text-xs text-[var(--color-text-muted)]">
          {utcDate(component.lastSeenUtc)} UTC
        </p>
      </div>
    ),
  },
  {
    key: 'details',
    header: 'Details',
    render: (component) => (
      <span className="block max-w-[560px] truncate" title={component.details ?? undefined}>
        {component.details || '—'}
      </span>
    ),
  },
]

export function HealthPage() {
  const query = useComponentHealth()

  const healthy =
    query.data?.filter(
      (component) =>
        healthTone(component.status) === 'success',
    ).length ?? 0

  const unhealthy =
    query.data?.filter(
      (component) =>
        healthTone(component.status) === 'danger',
    ).length ?? 0

  return (
    <>
      <PageHeader
        title="Health"
        description="Service heartbeat freshness and operational component status."
      />

      <main className="space-y-8 p-8">
        <PageSection
          title="System health"
          description="Dashboard API derives component health from persisted heartbeat freshness."
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
            <div className="grid grid-cols-3 gap-4">
              {Array.from({ length: 6 }, (_, index) => (
                <LoadingSkeleton
                  key={index}
                  className="h-56"
                />
              ))}
            </div>
          )}

          {query.isError && (
            <ErrorState
              title="Health information unavailable"
              description={query.error.message}
              action={
                <Button onClick={() => void query.refetch()}>
                  Retry
                </Button>
              }
            />
          )}

          {query.data && query.data.length === 0 && (
            <EmptyState
              title="No heartbeat records"
              description="No component heartbeat data is currently available."
            />
          )}

          {query.data && query.data.length > 0 && (
            <div className="space-y-6">
              <div className="grid grid-cols-4 gap-4">
                <MetricCard
                  label="Components"
                  value={String(query.data.length)}
                  helper="Persisted heartbeat rows"
                  icon={<Server size={18} />}
                />

                <MetricCard
                  label="Healthy"
                  value={String(healthy)}
                  helper="Fresh heartbeat"
                  icon={<CircleCheck size={18} />}
                />

                <MetricCard
                  label="Unhealthy"
                  value={String(unhealthy)}
                  helper="Stale or unhealthy"
                  trend={unhealthy > 0 ? 'down' : 'neutral'}
                  icon={<CircleX size={18} />}
                />

                <MetricCard
                  label="Refresh"
                  value="10 sec"
                  helper="Automatic polling"
                  icon={<Activity size={18} />}
                />
              </div>

              <HealthCards components={query.data} />
            </div>
          )}
        </PageSection>

        {query.data && query.data.length > 0 && (
          <PageSection
            title="Component details"
            description="Compact view for comparing heartbeat freshness across all services."
          >
            <DataTable
              columns={columns}
              rows={query.data}
              rowKey={(component) => component.component}
            />
          </PageSection>
        )}
      </main>
    </>
  )
}
