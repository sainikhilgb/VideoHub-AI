import React from 'react'
import { RouterProvider } from 'react-router-dom'
import { QueryClient, QueryClientProvider, QueryCache } from '@tanstack/react-query'
import { Toaster } from 'react-hot-toast'
import { UIProvider } from '@/app/providers/UIProvider'
import { AuthProvider } from '@/features/auth/context/AuthContext'
import { router } from '@/app/router'
import toast from 'react-hot-toast'

// Create a client for TanStack Query
const queryClient = new QueryClient({
  queryCache: new QueryCache({
    onError: (error: any) => {
      if (error?.response?.status !== 404 && error?.response?.status !== 401) {
        const errorMessage =
          error?.response?.data?.detail ||
          error?.response?.data?.message ||
          error.message ||
          'Failed to load query data'
        toast.error(errorMessage)
      }
    },
  }),
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
      staleTime: 5 * 60 * 1000, // 5 minutes
    },
  },
})

export const App: React.FC = () => {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <UIProvider>
          <RouterProvider router={router} />
          <Toaster
            position="top-right"
            toastOptions={{
              duration: 4000,
              style: {
                background: '#ffffff',
                color: '#0f172a',
                border: '1px solid #e2e8f0',
                borderRadius: '8px',
                fontSize: '14px',
              },
            }}
          />
        </UIProvider>
      </AuthProvider>
    </QueryClientProvider>
  )
}

export default App
