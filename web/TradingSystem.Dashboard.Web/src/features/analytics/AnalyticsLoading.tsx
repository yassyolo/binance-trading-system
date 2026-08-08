import { Card } from '@/components/ui/Card'
import { LoadingSkeleton } from '@/components/ui/LoadingSkeleton'

export function AnalyticsLoading() {
  return (
    <div className="space-y-6">
      <div className="grid grid-cols-4 gap-4">
        {Array.from({ length: 8 }, (_, index) => (
          <Card key={index} className="p-5">
            <LoadingSkeleton className="h-3 w-24" />
            <LoadingSkeleton className="mt-4 h-7 w-28" />
            <LoadingSkeleton className="mt-5 h-3 w-20" />
          </Card>
        ))}
      </div>

      <Card className="p-5">
        <LoadingSkeleton className="h-[340px] w-full" />
      </Card>
    </div>
  )
}
