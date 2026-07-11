import React from 'react'
import { Play } from 'lucide-react'

interface AppLogoProps {
  collapsed?: boolean
}

export const AppLogo: React.FC<AppLogoProps> = ({ collapsed = false }) => {
  return (
    <div className="flex items-center gap-2.5 px-2 py-1 select-none">
      <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-accent text-white shadow-custom-md transition-all duration-300 hover:scale-105 hover:shadow-lg">
        <Play className="h-4.5 w-4.5 fill-current ml-0.5" />
      </div>
      {!collapsed && (
        <span className="font-sans font-bold text-lg tracking-tight text-white animate-fade-in">
          VideoHub <span className="text-accent">AI</span>
        </span>
      )}
    </div>
  )
}
