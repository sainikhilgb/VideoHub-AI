import React from 'react'
import { Menu, Bell } from 'lucide-react'
import { SearchBar } from './SearchBar'
import { ThemeToggle } from './ThemeToggle'
import { AvatarMenu } from './AvatarMenu'
import { Breadcrumb } from './Breadcrumb'
import { useUI } from '@/app/providers/UIProvider'
import toast from 'react-hot-toast'

export const Navbar: React.FC = () => {
  const { setSidebarOpen } = useUI()

  return (
    <header className="flex h-16 items-center justify-between border-b border-border-custom bg-card px-4 md:px-6 shadow-custom-sm sticky top-0 z-30 select-none">
      {/* Left side: Hamburger (mobile/tablet) + Breadcrumbs */}
      <div className="flex items-center gap-3">
        <button
          onClick={() => setSidebarOpen(true)}
          className="flex h-9 w-9 items-center justify-center rounded-lg border border-border-custom text-text-muted hover:bg-app-bg hover:text-primary focus:outline-none focus-visible:ring-2 focus-visible:ring-accent lg:hidden"
          aria-label="Open sidebar"
        >
          <Menu className="h-5 w-5" />
        </button>
        <div className="hidden sm:block">
          <Breadcrumb />
        </div>
      </div>

      {/* Right side: Search + Actions */}
      <div className="flex items-center gap-4 flex-1 justify-end max-w-xl">
        <div className="hidden md:block w-full max-w-xs lg:max-w-md">
          <SearchBar />
        </div>

        {/* Notifications Icon Button */}
        <button
          onClick={() => toast.success('You have no new notifications')}
          className="relative flex h-9 w-9 items-center justify-center rounded-lg border border-border-custom bg-card text-text-muted shadow-custom-sm transition-all duration-200 hover:bg-app-bg hover:text-primary focus:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          aria-label="Notifications"
        >
          <Bell className="h-4.5 w-4.5" />
          <span className="absolute top-1.5 right-1.5 h-2 w-2 rounded-full bg-danger ring-2 ring-card" />
        </button>

        {/* Theme Toggle */}
        <ThemeToggle />

        {/* User Profile Avatar Dropdown */}
        <AvatarMenu />
      </div>
    </header>
  )
}
export default Navbar
