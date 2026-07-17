import React, { createContext, useContext, useState, useEffect } from 'react'
import { authApi } from '../services/authApi'
import type { UserProfile } from '../types'
import { setAccessToken } from '@/shared/services/api/client'
import toast from 'react-hot-toast'

interface AuthContextType {
  user: UserProfile | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (accessToken: string, email: string) => Promise<void>
  logout: () => Promise<void>
  refreshProfile: () => Promise<void>
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<UserProfile | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  const refreshProfile = async () => {
    try {
      const profile = await authApi.getCurrentUser()
      setUser(profile)
      localStorage.setItem('user_id', profile.id)
    } catch {
      setAccessToken(null)
      localStorage.removeItem('user_email')
      localStorage.removeItem('user_id')
      setUser(null)
    }
  }

  useEffect(() => {
    const initializeAuth = async () => {
      const hasSession = !!localStorage.getItem('user_id')
      if (hasSession) {
        try {
          // Recover session silently
          const response = await authApi.refresh()
          setAccessToken(response.accessToken)
          await refreshProfile()
        } catch (error) {
          console.error('Failed to restore auth session', error)
          setAccessToken(null)
          localStorage.removeItem('user_email')
          localStorage.removeItem('user_id')
          setUser(null)
        }
      }
      setIsLoading(false)
    }
    initializeAuth()
  }, [])

  const login = async (accessToken: string, email: string) => {
    setAccessToken(accessToken)
    localStorage.setItem('user_email', email)
    await refreshProfile()
  }

  const logout = async () => {
    try {
      await authApi.logout()
    } catch (err) {
      console.error('Logout request failed', err)
    }
    setAccessToken(null)
    localStorage.removeItem('user_email')
    localStorage.removeItem('user_id')
    setUser(null)
    toast.success('Logged out successfully')
  }

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading,
        login,
        logout,
        refreshProfile,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export const useAuth = () => {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
