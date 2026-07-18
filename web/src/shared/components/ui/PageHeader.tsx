import React from 'react'

interface PageHeaderProps {
  title: string
  description?: string
  actions?: React.ReactNode
}

export const PageHeader: React.FC<PageHeaderProps> = ({ title, description, actions }) => {
  return (
    <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between border-b border-border-custom/50 pb-5 mb-6">
      <div className="space-y-1">
        <h1 className="text-3xl font-bold tracking-tight text-gradient font-sans m-0">{title}</h1>
        {description && <p className="text-sm text-text-muted m-0">{description}</p>}
      </div>
      {actions && <div className="flex items-center gap-3 mt-3 sm:mt-0">{actions}</div>}
    </div>
  )
}
