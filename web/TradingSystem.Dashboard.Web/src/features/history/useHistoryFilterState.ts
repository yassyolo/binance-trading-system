import { useState } from 'react'

import type { HistoryFilterValues } from '@/features/history/HistoryFilters'
import type { HistoryQuery } from '@/types/trading-history'

const emptyFilters: HistoryFilterValues = {
  botName: '',
  symbol: '',
  status: '',
  fromDate: '',
  toDate: '',
}

function toUtc(value: string) {
  if (!value) return undefined
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? undefined : date.toISOString()
}

export function useHistoryFilterState(
  take = 50,
  options?: { dates?: boolean; status?: boolean },
) {
  const [draft, setDraft] = useState<HistoryFilterValues>(emptyFilters)
  const [query, setQuery] = useState<HistoryQuery>({
    skip: 0,
    take,
  })

  function apply() {
    setQuery({
      skip: 0,
      take,
      botName: draft.botName.trim() || undefined,
      symbol: draft.symbol.trim().toUpperCase() || undefined,
      status: options?.status ? draft.status.trim() || undefined : undefined,
      fromUtc: options?.dates ? toUtc(draft.fromDate) : undefined,
      toUtc: options?.dates ? toUtc(draft.toDate) : undefined,
    })
  }

  function reset() {
    setDraft(emptyFilters)
    setQuery({ skip: 0, take })
  }

  function previous() {
    setQuery((current) => ({
      ...current,
      skip: Math.max(0, current.skip - current.take),
    }))
  }

  function next() {
    setQuery((current) => ({
      ...current,
      skip: current.skip + current.take,
    }))
  }

  return {
    draft,
    setDraft,
    query,
    apply,
    reset,
    previous,
    next,
  }
}
