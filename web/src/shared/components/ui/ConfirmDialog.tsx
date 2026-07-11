import React, { useEffect, useRef } from 'react'
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
  const dialogRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!isOpen) return

    const previousActiveElement = document.activeElement as HTMLElement
    const dialogElement = dialogRef.current

    // Set initial focus to cancel button to prevent accidental confirmation
    if (dialogElement) {
      const cancelButton = dialogElement.querySelector('#cancel-btn') as HTMLButtonElement
      if (cancelButton) {
        cancelButton.focus()
      }
    }

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onCancel()
      }

      if (e.key === 'Tab' && dialogElement) {
        const focusableSelectors =
          'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
        const focusableElements = Array.from(
          dialogElement.querySelectorAll<HTMLElement>(focusableSelectors),
        )
        if (focusableElements.length === 0) return

        const firstElement = focusableElements[0]
        const lastElement = focusableElements[focusableElements.length - 1]

        if (e.shiftKey) {
          if (document.activeElement === firstElement) {
            lastElement.focus()
            e.preventDefault()
          }
        } else {
          if (document.activeElement === lastElement) {
            firstElement.focus()
            e.preventDefault()
          }
        }
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => {
      window.removeEventListener('keydown', handleKeyDown)
      if (previousActiveElement) {
        previousActiveElement.focus()
      }
    }
  }, [isOpen, onCancel])

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs animate-fade-in">
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby="dialog-title"
        className="relative w-full max-w-md rounded-xl border border-border-custom bg-card shadow-custom-lg p-6 animate-scale-up"
      >
        {/* Close Button */}
        <button
          onClick={onCancel}
          aria-label="Close dialog"
          className="absolute top-4 right-4 text-text-muted hover:text-text-main p-1 rounded-md transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-accent"
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
            <h3 id="dialog-title" className="text-base font-semibold text-text-main m-0">
              {title}
            </h3>
            <p className="text-sm text-text-muted m-0">{message}</p>
          </div>
        </div>

        {/* Action Buttons */}
        <div className="mt-6 flex justify-end gap-3">
          <button
            id="cancel-btn"
            onClick={onCancel}
            className="rounded-lg border border-border-custom bg-card px-4 py-2 text-sm font-medium text-text-muted hover:bg-slate-50 transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          >
            {cancelLabel}
          </button>
          <button
            onClick={onConfirm}
            className={clsx(
              'rounded-lg px-4 py-2 text-sm font-semibold text-white shadow-custom-sm transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-accent',
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
export default ConfirmDialog
