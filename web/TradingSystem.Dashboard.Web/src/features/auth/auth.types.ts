
export interface DashboardUser {
  userName: string
  displayName: string
  roles: string[]
}

export interface DashboardLoginResponse
  extends DashboardUser {
  accessToken: string
  tokenType: string
  expiresAtUtc: string
}

export interface DashboardLoginRequest {
  userName: string
  password: string
}
