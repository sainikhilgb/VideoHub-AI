import React from 'react'
import { Link, useLocation } from 'react-router-dom'
import { ChevronRight, Home } from 'lucide-react'

export const Breadcrumb: React.FC = () => {
  const location = useLocation()
  const pathnames = location.pathname.split('/').filter((x) => x)

  return (
    <nav className="flex items-center space-x-1.5 text-xs text-text-muted" aria-label="Breadcrumb">
      <Link
        to="/"
        className="flex items-center hover:text-text-main transition-colors duration-150"
      >
        <Home className="h-3.5 w-3.5" />
      </Link>

      {pathnames.map((value, index) => {
        const to = `/${pathnames.slice(0, index + 1).join('/')}`
        const isLast = index === pathnames.length - 1
        const formattedName = value.charAt(0).toUpperCase() + value.slice(1).replace(/-/g, ' ')

        return (
          <React.Fragment key={to}>
            <ChevronRight className="h-3.5 w-3.5 text-text-muted/60" />
            {isLast ? (
              <span className="font-medium text-text-main select-none">{formattedName}</span>
            ) : (
              <Link to={to} className="hover:text-text-main transition-colors duration-150">
                {formattedName}
              </Link>
            )}
          </React.Fragment>
        )
      })}
    </nav>
  )
}
