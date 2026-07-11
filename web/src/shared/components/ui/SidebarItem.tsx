import React from 'react'
import { Link, useLocation } from 'react-router-dom'
import type { LucideIcon } from 'lucide-react'
import clsx from 'clsx'

interface SidebarItemProps {
  to: string
  icon: LucideIcon
  label: string
  collapsed?: boolean
  onClick?: () => void
}

export const SidebarItem: React.FC<SidebarItemProps> = ({
  to,
  icon: Icon,
  label,
  collapsed = false,
  onClick,
}) => {
  const location = useLocation()

  // Handle active logic: exact match for root, exact-or-child match for others
  const isActive =
    to === '/'
      ? location.pathname === '/'
      : location.pathname === to || location.pathname.startsWith(`${to}/`)

  return (
    <Link
      to={to}
      onClick={onClick}
      aria-label={collapsed ? label : undefined}
      className={clsx(
        'group flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all duration-200 focus:outline-none focus-visible:ring-2 focus-visible:ring-accent select-none',
        isActive
          ? 'bg-accent text-white shadow-custom-sm font-semibold'
          : 'text-slate-400 hover:bg-slate-800 hover:text-white',
      )}
      title={collapsed ? label : undefined}
    >
      <Icon
        className={clsx(
          'h-5 w-5 shrink-0 transition-transform duration-200 group-hover:scale-105',
          isActive ? 'text-white' : 'text-slate-400 group-hover:text-white',
        )}
      />
      {!collapsed && <span className="truncate">{label}</span>}
    </Link>
  )
}
export default SidebarItem
