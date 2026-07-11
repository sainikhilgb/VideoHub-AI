import React from 'react'
import { Sun, Moon } from 'lucide-react'
import { useUI } from '@/app/providers/UIProvider'

export const ThemeToggle: React.FC = () => {
  const { theme, toggleTheme } = useUI()

  return (
    <button
      onClick={toggleTheme}
      className="flex h-9 w-9 items-center justify-center rounded-lg border border-border-custom bg-card text-text-muted shadow-custom-sm transition-all duration-200 hover:bg-app-bg hover:text-primary focus:outline-none"
      aria-label="Toggle theme"
    >
      {theme === 'light' ? (
        <Sun className="h-4.5 w-4.5 text-warning" />
      ) : (
        <Moon className="h-4.5 w-4.5 text-accent" />
      )}
    </button>
  )
}
