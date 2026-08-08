import { Card } from '@/components/ui/Card'
import { LoadingSkeleton } from '@/components/ui/LoadingSkeleton'

export function OverviewLoading() {
  return (
    <main className="space-y-10 p-8">
      <div className="grid grid-cols-4 gap-4">
        {Array.from({ length: 4 }, (_, index) => (
          <Card key={index} className="p-5">
            <LoadingSkeleton className="h-3 w-24" />
            <LoadingSkeleton className="mt-4 h-8 w-36" />
            <LoadingSkeleton className="mt-6 h-3 w-28" />
          </Card>
        ))}
      </div>

      <Card className="p-6">
        <LoadingSkeleton className="h-4 w-32" />
        <LoadingSkeleton className="mt-6 h-56 w-full" />
      </Card>
    </main>
  )
}
