import React from 'react'
import { LayoutDashboard, FolderClosed, Settings, ChevronLeft, ChevronRight, X } from 'lucide-react'
import { AppLogo } from './AppLogo'
import { SidebarItem } from './SidebarItem'
import { useUI } from '@/app/providers/UIProvider'
import clsx from 'clsx'

interface SidebarProps {
  isMobileDrawer?: boolean
}

export const Sidebar: React.FC<SidebarProps> = ({ isMobileDrawer = false }) => {
  const { sidebarCollapsed, toggleSidebarCollapse, setSidebarOpen } = useUI()

  const menuItems = [
    { to: '/', icon: LayoutDashboard, label: 'Dashboard' },
    { to: '/projects', icon: FolderClosed, label: 'Projects' },
    { to: '/settings', icon: Settings, label: 'Settings' },
  ]

  const handleMobileLinkClick = () => {
    if (isMobileDrawer) {
      setSidebarOpen(false)
    }
  }

  return (
    <aside
      className={clsx(
        'flex flex-col bg-primary h-full border-r border-slate-800 transition-all duration-300 relative',
        isMobileDrawer ? 'w-64' : sidebarCollapsed ? 'w-16' : 'w-64',
      )}
    >
      {/* Header / Logo */}
      <div className="flex h-16 items-center justify-between px-4 border-b border-slate-800">
        <AppLogo collapsed={!isMobileDrawer && sidebarCollapsed} />
        {isMobileDrawer && (
          <button
            onClick={() => setSidebarOpen(false)}
            className="text-slate-400 hover:text-white p-1 rounded-md focus:outline-none"
            aria-label="Close menu"
          >
            <X className="h-5 w-5" />
          </button>
        )}
      </div>

      {/* Nav Menu */}
      <nav className="flex-1 space-y-1.5 px-3 py-4 overflow-y-auto custom-scrollbar">
        {menuItems.map((item) => (
          <SidebarItem
            key={item.to}
            to={item.to}
            icon={item.icon}
            label={item.label}
            collapsed={!isMobileDrawer && sidebarCollapsed}
            onClick={handleMobileLinkClick}
          />
        ))}
      </nav>

      {/* Footer / Collapse Toggle */}
      {!isMobileDrawer && (
        <div className="p-3 border-t border-slate-800 flex justify-end">
          <button
            onClick={toggleSidebarCollapse}
            className="flex h-8 w-8 items-center justify-center rounded-lg border border-slate-800 text-slate-400 hover:bg-slate-800 hover:text-white transition-colors focus:outline-none"
            aria-label={sidebarCollapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          >
            {sidebarCollapsed ? (
              <ChevronRight className="h-4.5 w-4.5" />
            ) : (
              <ChevronLeft className="h-4.5 w-4.5" />
            )}
          </button>
        </div>
      )}
    </aside>
  )
}
