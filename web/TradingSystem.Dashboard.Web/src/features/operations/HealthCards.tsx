import { CircleCheck, CircleX, Clock3, Server, TriangleAlert,} from 'lucide-react'
import { Card } from '@/components/ui/Card'
import { StatusBadge } from '@/components/ui/StatusBadge'
import { healthTone, relativeTime, utcDate,} from '@/features/operations/operations-formatters'
import type { ComponentHealthDto } from '@/types/operations'

export function HealthCards({components}: { components: ComponentHealthDto[]}) {
  return (
    <div className="grid grid-cols-3 gap-4">
      {components.map((component) => {
        const tone = healthTone(component.status)

        return (
          <Card key={component.component} className="p-5">
            <div className="flex items-start justify-between gap-4">
              <div className="grid size-10 place-items-center rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-strong)] text-[var(--color-text-secondary)]">
                <HealthIcon tone={tone} />
              </div>

              <StatusBadge label={component.status} tone={tone}/>
            </div>

            <h3 className="mt-6 text-sm font-semibold">
              {component.component}
            </h3>

            <div className="mt-4 flex items-center gap-2 text-xs text-[var(--color-text-muted)]">
              <Clock3 size={13} />
              {relativeTime(component.lastSeenUtc)}
            </div>

            <p className="mt-1 text-[10px] text-[var(--color-text-muted)]">
              {utcDate(component.lastSeenUtc)} UTC
            </p>

            <div className="mt-5 min-h-12 rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] px-3 py-2.5 text-xs leading-5 text-[var(--color-text-secondary)]">
              {component.details || 'No additional details.'}
            </div>
          </Card>
        )
      })}
    </div>
  )
}

function HealthIcon({tone}: { tone: ReturnType<typeof healthTone>}) {
  if (tone === 'success')
    return <CircleCheck size={18} />

  if (tone === 'warning')
    return <TriangleAlert size={18} />

  if (tone === 'danger')
    return <CircleX size={18} />

  return <Server size={18} />
}
