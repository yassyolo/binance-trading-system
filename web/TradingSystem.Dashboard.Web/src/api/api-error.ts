import type { ProblemDetails } from '@/api/problem-details'

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem: ProblemDetails | null,
    public readonly correlationId: string | null,
  ) {
    super(problem?.detail || problem?.title || `Request failed with status ${status}`)
    this.name = 'ApiError'
  }
}
