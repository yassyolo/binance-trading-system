
const tokenStorageKey =
  'trading-dashboard.access-token'

export const authenticationExpiredEvent =
  'trading-dashboard.authentication-expired'

export function getAccessToken() {
  return window.localStorage.getItem(
    tokenStorageKey,
  )
}

export function setAccessToken(token: string) {
  const normalized = token
    .trim()
    .replace(/^Bearer\s+/i, '')

  if (!normalized) {
    clearAccessToken()
    return
  }

  window.localStorage.setItem(
    tokenStorageKey,
    normalized,
  )
}

export function clearAccessToken() {
  window.localStorage.removeItem(
    tokenStorageKey,
  )
}

export function notifyAuthenticationExpired() {
  window.dispatchEvent(
    new Event(authenticationExpiredEvent),
  )
}
