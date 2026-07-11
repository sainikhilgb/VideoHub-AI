import React from 'react'
import { AlertCircle } from 'lucide-react'

interface ErrorStateProps {
  title?: string
  description?: string
  onRetry?: () => void
}

export const ErrorState: React.FC<ErrorStateProps> = ({
  title = 'An error occurred',
  description = 'Something went wrong while loading this content. Please try again.',
  onRetry,
}) => {
  return (
    <div className="flex flex-col items-center justify-center text-center p-8 rounded-xl border border-red-100 bg-red-50/20 select-none">
      <div className="flex h-12 w-12 items-center justify-center rounded-full bg-red-50 border border-red-200 text-danger mb-4">
        <AlertCircle className="h-6 w-6" />
      </div>
      <h3 className="text-sm font-semibold text-danger m-0">{title}</h3>
      <p className="text-xs text-text-muted mt-1.5 mb-4 max-w-sm">{description}</p>
      {onRetry && (
        <button
          onClick={onRetry}
          className="rounded-lg bg-danger px-4 py-2 text-xs font-semibold text-white shadow-custom-sm hover:bg-red-700 transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-danger"
        >
          Retry
        </button>
      )}
    </div>
  )
}
