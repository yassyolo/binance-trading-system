import { NavLink } from 'react-router'

import type { NavigationItem } from '@/components/layout/navigation'

interface SidebarItemProps {
  item: NavigationItem
  collapsed: boolean
}

export function SidebarItem({ item, collapsed }: SidebarItemProps) {
  const Icon = item.icon

  return (
    <NavLink
      to={item.path}
      title={collapsed ? item.label : undefined}
      className={({ isActive }) =>
        [
          'group flex h-10 items-center rounded-xl text-sm transition-colors',
          collapsed ? 'justify-center px-0' : 'gap-3 px-3',
          isActive
            ? 'bg-[var(--color-surface-strong)] text-[var(--color-text-primary)]'
            : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-hover)] hover:text-[var(--color-text-primary)]',
        ].join(' ')
      }
    >
      <Icon className="shrink-0" size={18} strokeWidth={1.8} />
      {!collapsed && <span className="truncate">{item.label}</span>}
    </NavLink>
  )
}
