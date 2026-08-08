import { useEffect, useState } from 'react'
import { Outlet } from 'react-router'

import { Sidebar } from '@/components/layout/Sidebar'

const sidebarStorageKey = 'trading-dashboard.sidebar-collapsed'

function readInitialCollapsedState() {
  return window.localStorage.getItem(sidebarStorageKey) === 'true'
}

export function AppShell() {
  const [collapsed, setCollapsed] = useState(readInitialCollapsedState)

  useEffect(() => {
    window.localStorage.setItem(sidebarStorageKey, String(collapsed))
  }, [collapsed])

  return (
    <div className="min-h-screen bg-[var(--color-app)] text-[var(--color-text-primary)]">
      <Sidebar collapsed={collapsed} onToggle={() => setCollapsed((current) => !current)} />

      <div
        className={[
          'min-h-screen transition-[padding-left] duration-200',
          collapsed ? 'pl-[72px]' : 'pl-[248px]',
        ].join(' ')}
      >
        <Outlet />
      </div>
    </div>
  )
}
