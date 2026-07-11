import React from 'react'
import { Inbox } from 'lucide-react'

interface EmptyStateProps {
  title?: string
  description?: string
  icon?: React.ReactNode
  action?: React.ReactNode
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  title = 'No data found',
  description = 'There is currently nothing to show here.',
  icon,
  action,
}) => {
  return (
    <div className="flex flex-col items-center justify-center text-center p-8 rounded-xl border border-dashed border-border-custom bg-card select-none">
      <div className="flex h-12 w-12 items-center justify-center rounded-full bg-slate-50 border border-border-custom text-text-muted mb-4">
        {icon || <Inbox className="h-6 w-6" />}
      </div>
      <h3 className="text-sm font-semibold text-text-main m-0">{title}</h3>
      <p className="text-xs text-text-muted mt-1.5 mb-4 max-w-sm">{description}</p>
      {action && <div className="mt-2">{action}</div>}
    </div>
  )
}
