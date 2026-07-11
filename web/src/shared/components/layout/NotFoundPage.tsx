import React from 'react'
import { Link } from 'react-router-dom'
import { AlertCircle, ArrowLeft } from 'lucide-react'

export const NotFoundPage: React.FC = () => {
  return (
    <div className="min-h-[70vh] flex flex-col items-center justify-center text-center px-4 select-none">
      <div className="flex h-16 w-16 items-center justify-center rounded-full bg-slate-100 text-text-muted mb-6 border border-border-custom shadow-custom-sm">
        <AlertCircle className="h-8 w-8 text-accent" />
      </div>

      <h1 className="text-4xl font-extrabold tracking-tight text-text-main m-0 sm:text-5xl font-sans">
        404
      </h1>
      <h2 className="text-xl font-semibold text-text-main mt-3 mb-2">Page Not Found</h2>
      <p className="text-sm text-text-muted max-w-md mx-auto mb-8">
        Sorry, we couldn't find the page you're looking for. It might have been moved or deleted.
      </p>

      <Link
        to="/"
        className="inline-flex items-center gap-2 rounded-lg bg-accent px-4 py-2.5 text-sm font-semibold text-white shadow-custom-md hover:bg-accent-hover transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-accent"
      >
        <ArrowLeft className="h-4 w-4" />
        Back to Dashboard
      </Link>
    </div>
  )
}
