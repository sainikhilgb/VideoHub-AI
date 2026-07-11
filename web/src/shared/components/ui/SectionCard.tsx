import React from 'react'
import clsx from 'clsx'

interface SectionCardProps {
  title?: string
  subtitle?: string
  actions?: React.ReactNode
  children: React.ReactNode
  className?: string
  padding?: boolean
}

export const SectionCard: React.FC<SectionCardProps> = ({
  title,
  subtitle,
  actions,
  children,
  className = '',
  padding = true,
}) => {
  return (
    <div
      className={clsx(
        'rounded-xl border border-border-custom bg-card shadow-custom-sm overflow-hidden',
        className,
      )}
    >
      {(title || subtitle || actions) && (
        <div className="flex items-center justify-between border-b border-border-custom/50 px-5 py-4 bg-slate-50/50">
          <div>
            {title && <h3 className="text-sm font-semibold text-text-main m-0">{title}</h3>}
            {subtitle && <p className="text-xs text-text-muted mt-0.5 mb-0">{subtitle}</p>}
          </div>
          {actions && <div className="flex items-center gap-2">{actions}</div>}
        </div>
      )}
      <div className={clsx(padding && 'p-5')}>{children}</div>
    </div>
  )
}
