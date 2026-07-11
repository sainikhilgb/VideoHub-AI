import React, { useState, useRef, useEffect } from 'react'
import { User, Settings, LogOut } from 'lucide-react'
import { Link } from 'react-router-dom'
import toast from 'react-hot-toast'

interface AvatarMenuProps {
  user?: {
    email: string
    displayName?: string
  }
}

export const AvatarMenu: React.FC<AvatarMenuProps> = ({
  user = { email: 'john.doe@videohub.ai', displayName: 'John Doe' },
}) => {
  const [isOpen, setIsOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const handleLogout = () => {
    localStorage.removeItem('auth_token')
    localStorage.removeItem('user_email')
    setIsOpen(false)
    toast.success('Logged out successfully')
  }

  const initials = user.displayName
    ? user.displayName
        .split(' ')
        .map((n) => n[0])
        .join('')
        .toUpperCase()
    : 'JD'

  return (
    <div className="relative" ref={menuRef}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        aria-label="User Menu"
        aria-haspopup="true"
        aria-expanded={isOpen}
        className="flex h-9 w-9 items-center justify-center rounded-full bg-accent text-white font-semibold text-sm shadow-custom-sm focus:outline-none focus-visible:ring-2 focus-visible:ring-accent transition-transform hover:scale-105"
      >
        {initials}
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-48 rounded-lg border border-border-custom bg-card py-1.5 shadow-custom-lg ring-1 ring-black/5 focus:outline-none z-50">
          <div className="px-4 py-2 border-b border-border-custom">
            <p className="text-xs text-text-muted">Signed in as</p>
            <p className="text-sm font-medium text-text-main truncate">{user.email}</p>
          </div>

          <Link
            to="/settings"
            onClick={() => setIsOpen(false)}
            className="flex items-center gap-2 px-4 py-2 text-sm text-text-muted hover:bg-app-bg hover:text-text-main transition-colors"
          >
            <User className="h-4 w-4" />
            <span>Profile</span>
          </Link>

          <Link
            to="/settings"
            onClick={() => setIsOpen(false)}
            className="flex items-center gap-2 px-4 py-2 text-sm text-text-muted hover:bg-app-bg hover:text-text-main transition-colors"
          >
            <Settings className="h-4 w-4" />
            <span>Settings</span>
          </Link>

          <div className="border-t border-border-custom my-1"></div>

          <button
            onClick={handleLogout}
            className="flex w-full items-center gap-2 px-4 py-2 text-sm text-danger hover:bg-red-50 transition-colors text-left"
          >
            <LogOut className="h-4 w-4" />
            <span>Logout</span>
          </button>
        </div>
      )}
    </div>
  )
}
export default AvatarMenu
