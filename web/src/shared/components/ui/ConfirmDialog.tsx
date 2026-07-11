import React from 'react'
import { AlertTriangle, X } from 'lucide-react'
import clsx from 'clsx'

interface ConfirmDialogProps {
  isOpen: boolean
  title: string
  message: string
  confirmLabel?: string
  cancelLabel?: string
  onConfirm: () => void
  onCancel: () => void
  isDanger?: boolean
}

export const ConfirmDialog: React.FC<ConfirmDialogProps> = ({
  isOpen,
  title,
  message,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  onConfirm,
  onCancel,
  isDanger = false,
}) => {
  if (!isOpen) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs animate-fade-in">
      <div className="relative w-full max-w-md rounded-xl border border-border-custom bg-card shadow-custom-lg p-6 animate-scale-up">
        {/* Close Button */}
        <button
          onClick={onCancel}
          className="absolute top-4 right-4 text-text-muted hover:text-text-main p-1 rounded-md transition-colors focus:outline-none"
        >
          <X className="h-4 w-4" />
        </button>

        {/* Header Icon + Title */}
        <div className="flex items-start gap-4">
          <div
            className={clsx(
              'flex h-10 w-10 shrink-0 items-center justify-center rounded-full border',
              isDanger
                ? 'bg-red-50 border-red-200 text-danger'
                : 'bg-blue-50 border-blue-200 text-accent',
            )}
          >
            <AlertTriangle className="h-5 w-5" />
          </div>
          <div className="space-y-1.5">
            <h3 className="text-base font-semibold text-text-main m-0">{title}</h3>
            <p className="text-sm text-text-muted m-0">{message}</p>
          </div>
        </div>

        {/* Action Buttons */}
        <div className="mt-6 flex justify-end gap-3">
          <button
            onClick={onCancel}
            className="rounded-lg border border-border-custom bg-card px-4 py-2 text-sm font-medium text-text-muted hover:bg-slate-50 transition-colors focus:outline-none"
          >
            {cancelLabel}
          </button>
          <button
            onClick={onConfirm}
            className={clsx(
              'rounded-lg px-4 py-2 text-sm font-semibold text-white shadow-custom-sm transition-colors focus:outline-none',
              isDanger ? 'bg-danger hover:bg-red-700' : 'bg-accent hover:bg-accent-hover',
            )}
          >
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  )
}
