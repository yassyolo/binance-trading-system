
import {
  ChevronLeft,
  ChevronRight,
  LogOut,
  Orbit,
  ShieldCheck,
} from 'lucide-react'

import { SidebarItem } from '@/components/layout/SidebarItem'
import { navigationGroups } from '@/components/layout/navigation'
import { useAuthentication } from '@/features/auth/auth-context'

interface SidebarProps {
  collapsed: boolean
  onToggle: () => void
}

export function Sidebar({
  collapsed,
  onToggle,
}: SidebarProps) {
  const authentication =
    useAuthentication()

  return (
    <aside
      className={['fixed inset-y-0 left-0 z-30 flex flex-col border-r border-[var(--color-border)] bg-[var(--color-sidebar)] transition-[width] duration-200',
        collapsed
          ? 'w-[72px]'
          : 'w-[248px]',
      ].join(' ')}
    >
      <div className={
          collapsed
            ? 'flex h-[72px] items-center justify-center'
            : 'flex h-[72px] items-center gap-3 px-5'
        }
      >
        <div className="grid size-9 shrink-0 place-items-center rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-strong)]">
          <Orbit size={19} />
        </div>

        {!collapsed && (
          <div className="min-w-0">
            <p className="truncate text-sm font-semibold tracking-tight">
              Trading System
            </p>
            <p className="truncate text-xs text-[var(--color-text-muted)]">
              Operations Dashboard
            </p>
          </div>
        )}
      </div>

      <nav className="flex-1 overflow-y-auto px-3 pb-4">
        {navigationGroups.map(
          (group, index) => (
            <div key={group.label} className={index === 0 ? 'mt-1' : 'mt-6'}
            >
              {!collapsed && (
                <p className="mb-2 px-3 text-[10px] font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {group.label}
                </p>
              )}

              {collapsed && index > 0 && (
                  <div className="mx-auto mb-3 h-px w-7 bg-[var(--color-border)]" />
                )}

              <div className="space-y-1">
                {group.items.map(
                  (item) => (
                    <SidebarItem
                      key={item.path}
                      item={item}
                      collapsed={
                        collapsed
                      }
                    />
                  ),
                )}
              </div>
            </div>
          ),
        )}
      </nav>

      <div className="border-t border-[var(--color-border)] p-3">
        {!collapsed &&
          authentication.user && (
            <div className="mb-3 flex items-center gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2.5">
              <ShieldCheck
                size={16}
                className="shrink-0 text-[var(--color-success)]"
              />
              <div className="min-w-0">
                <p className="truncate text-xs font-medium">
                  {
                    authentication
                      .user
                      .displayName
                  }
                </p>
                <p className="truncate text-[10px] text-[var(--color-text-muted)]">
                  {
                    authentication
                      .user.roles[
                      authentication
                        .user.roles
                        .length - 1
                    ] ?? 'Viewer'
                  }
                </p>
              </div>
            </div>
          )}

        <button
          type="button"
          onClick={
            authentication.logout
          }
          className={[
            'mb-1 flex h-10 w-full items-center rounded-xl text-[var(--color-text-secondary)] transition-colors hover:bg-[var(--color-surface-hover)] hover:text-[var(--color-text-primary)]',
            collapsed
              ? 'justify-center'
              : 'gap-3 px-3',
          ].join(' ')}
          title="Sign out"
        >
          <LogOut size={18} />
          {!collapsed && (
            <span className="text-sm">
              Sign out
            </span>
          )}
        </button>

        <button
          type="button"
          onClick={onToggle}
          className={[
            'flex h-10 w-full items-center rounded-xl text-[var(--color-text-secondary)] transition-colors hover:bg-[var(--color-surface-hover)] hover:text-[var(--color-text-primary)]',
            collapsed
              ? 'justify-center'
              : 'justify-between px-3',
          ].join(' ')}
          title={
            collapsed
              ? 'Expand sidebar'
              : 'Collapse sidebar'
          }
        >
          {!collapsed && (
            <span className="text-sm">
              Collapse sidebar
            </span>
          )}
          {collapsed ? (
            <ChevronRight
              size={18}
            />
          ) : (
            <ChevronLeft
              size={18}
            />
          )}
        </button>
      </div>
    </aside>
  )
}
