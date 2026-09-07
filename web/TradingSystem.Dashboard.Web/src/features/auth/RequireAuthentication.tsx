
import { LoaderCircle } from 'lucide-react'
import { Navigate, Outlet, useLocation,} from 'react-router'
import { useAuthentication } from '@/features/auth/auth-context'

export function RequireAuthentication() {
  const authentication = useAuthentication()
  const location = useLocation()

  if (authentication.status === 'loading') {
    return (
      <div className="grid min-h-screen place-items-center bg-[var(--color-app)] text-[var(--color-text-secondary)]">
        <div className="flex items-center gap-3 text-sm">
          <LoaderCircle className="animate-spin" size={18}/>
          Restoring dashboard session…
        </div>
      </div>
    )
  }

  if (authentication.status !== 'authenticated')
    return <Navigate to="/login" replace state={{from: location.pathname + location.search }}/>    

  return <Outlet />
}
