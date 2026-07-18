import React from 'react'
import type { LucideIcon } from 'lucide-react'
import clsx from 'clsx'

interface DashboardCardProps {
  title: string
  value: string | number
  icon: LucideIcon
  description?: string
  trend?: {
    value: string
    positive: boolean
  }
  className?: string
}

export const DashboardCard: React.FC<DashboardCardProps> = ({
  title,
  value,
  icon: Icon,
  description,
  trend,
  className = '',
}) => {
  return (
    <div
      className={clsx(
        'rounded-xl border border-border-custom glassmorphism p-5 shadow-custom-sm transition-all duration-300 hover:shadow-custom-lg hover:-translate-y-1',
        className,
      )}
    >
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium text-text-muted select-none">{title}</span>
        <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-slate-50 text-text-muted border border-border-custom/40">
          <Icon className="h-5 w-5 text-accent" />
        </div>
      </div>

      <div className="mt-2.5">
        <span className="text-2xl font-bold tracking-tight text-text-main">{value}</span>
      </div>

      {(description || trend) && (
        <div className="mt-2 flex items-center gap-1.5 text-xs">
          {trend && (
            <span
              className={clsx('font-semibold', trend.positive ? 'text-success' : 'text-danger')}
            >
              {trend.value}
            </span>
          )}
          {description && <span className="text-text-muted select-none">{description}</span>}
        </div>
      )}
    </div>
  )
}
