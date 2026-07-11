import React from 'react'
import { Search } from 'lucide-react'

export const SearchBar: React.FC = () => {
  return (
    <div className="relative w-full max-w-xs md:max-w-md">
      <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
        <Search className="h-4 w-4 text-text-muted" />
      </div>
      <input
        type="search"
        placeholder="Search projects, jobs, media..."
        className="w-full rounded-lg border border-border-custom bg-card py-1.5 pl-9 pr-3 text-sm text-text-main placeholder-text-muted shadow-custom-sm focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent transition-colors"
      />
    </div>
  )
}
