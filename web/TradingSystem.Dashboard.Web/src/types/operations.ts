export interface ComponentHealthDto {
  component: string
  status: string
  lastSeenUtc: string | null
  details: string | null
}

export interface AlertDto {
  alertId: number
  severity: string
  type: string
  message: string
  botName: string | null
  positionId: string | null
  createdAtUtc: string
  acknowledged: boolean
  acknowledgedAtUtc: string | null
  acknowledgedBy: string | null
}

export interface AuditEventDto {
  auditId: string
  occurredAtUtc: string
  actor: string
  action: string
  entityType: string
  entityId: string | null
  reason: string | null
  correlationId: string | null
  ipAddress: string | null
  oldValueJson: string | null
  newValueJson: string | null
}

export interface AuditQuery {
  actor?: string
  action?: string
  fromUtc?: string
  toUtc?: string
  skip: number
  take: number
}
