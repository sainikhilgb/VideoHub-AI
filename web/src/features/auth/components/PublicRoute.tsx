import React from 'react'
import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import { LoadingSpinner } from '@/shared/components/ui/LoadingSpinner'

export const PublicRoute: React.FC = () => {
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return (
      <div className="flex h-screen w-screen items-center justify-center bg-app-bg">
        <LoadingSpinner />
      </div>
    )
  }

  return !isAuthenticated ? <Outlet /> : <Navigate to="/" replace />
}
