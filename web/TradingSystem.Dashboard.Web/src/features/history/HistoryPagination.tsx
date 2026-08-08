import { ChevronLeft, ChevronRight } from 'lucide-react'

import { Button } from '@/components/ui/Button'

interface HistoryPaginationProps {
  skip: number
  take: number
  returned: number
  disabled?: boolean
  onPrevious: () => void
  onNext: () => void
}

export function HistoryPagination({
  skip,
  take,
  returned,
  disabled = false,
  onPrevious,
  onNext,
}: HistoryPaginationProps) {
  const page = Math.floor(skip / take) + 1

  return (
    <div className="mt-4 flex items-center justify-between">
      <p className="text-xs text-[var(--color-text-muted)]">
        Page {page} · showing {returned} row{returned === 1 ? '' : 's'}
      </p>

      <div className="flex gap-2">
        <Button
          size="sm"
          leftIcon={<ChevronLeft size={14} />}
          onClick={onPrevious}
          disabled={disabled || skip === 0}
        >
          Previous
        </Button>

        <Button
          size="sm"
          onClick={onNext}
          disabled={disabled || returned < take}
        >
          Next
          <ChevronRight size={14} />
        </Button>
      </div>
    </div>
  )
}
