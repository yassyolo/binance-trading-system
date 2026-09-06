import { useState } from 'react'
import {
  BellRing,
  CheckCircle2,
} from 'lucide-react'

import { PageHeader } from '@/components/layout/PageHeader'
import { Button } from '@/components/ui/Button'
import { PageSection } from '@/components/ui/PageSection'
import { AlertList } from '@/features/operations/AlertList'

export function AlertsPage() {
  const [acknowledged, setAcknowledged] =
    useState(false)

  return (
    <>
      <PageHeader title="Alerts" description="Operational alerts produced by the alert engine and trading safety rules."/>

      <main className="space-y-8 p-8">
        <PageSection
          title="Operational alerts"
          description="Acknowledgement records operator awareness; it does not imply that the underlying condition is resolved."
          actions={
            <div className="flex rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-1">
              <Button
                size="sm"
                variant={
                  !acknowledged
                    ? 'primary'
                    : 'ghost'
                }
                leftIcon={<BellRing size={14} />}
                onClick={() => setAcknowledged(false)}
              >
                Active
              </Button>

              <Button
                size="sm"
                variant={
                  acknowledged
                    ? 'primary'
                    : 'ghost'
                }
                leftIcon={<CheckCircle2 size={14} />}
                onClick={() => setAcknowledged(true)}
              >
                Acknowledged
              </Button>
            </div>
          }
        >
          <AlertList acknowledged={acknowledged} />
        </PageSection>
      </main>
    </>
  )
}
