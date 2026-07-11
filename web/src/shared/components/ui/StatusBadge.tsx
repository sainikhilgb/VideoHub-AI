import React from 'react'
import clsx from 'clsx'

export type StatusType =
  | 'idle'
  | 'pending'
  | 'processing'
  | 'completed'
  | 'failed'
  | 'success'
  | 'warning'
  | 'danger'
  | string

interface StatusBadgeProps {
  status: StatusType
  label?: string
}

export const StatusBadge: React.FC<StatusBadgeProps> = ({ status, label }) => {
  const normStatus = status.toLowerCase()

  const config: Record<string, { bg: string; text: string; dot: string }> = {
    completed: { bg: 'bg-green-50', text: 'text-success', dot: 'bg-success' },
    success: { bg: 'bg-green-50', text: 'text-success', dot: 'bg-success' },
    processing: { bg: 'bg-blue-50', text: 'text-accent', dot: 'bg-accent animate-pulse' },
    pending: { bg: 'bg-amber-50', text: 'text-warning', dot: 'bg-warning' },
    warning: { bg: 'bg-amber-50', text: 'text-warning', dot: 'bg-warning' },
    failed: { bg: 'bg-red-50', text: 'text-danger', dot: 'bg-danger' },
    danger: { bg: 'bg-red-50', text: 'text-danger', dot: 'bg-danger' },
    idle: { bg: 'bg-slate-100', text: 'text-text-muted', dot: 'bg-text-muted' },
  }

  const current = config[normStatus] || config.idle
  const displayText = label || status.charAt(0).toUpperCase() + status.slice(1)

  return (
    <span
      className={clsx(
        'inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-medium border border-black/[0.03] select-none',
        current.bg,
        current.text,
      )}
    >
      <span className={clsx('h-1.5 w-1.5 rounded-full', current.dot)} />
      {displayText}
    </span>
  )
}
