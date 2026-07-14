import React, { createContext, useContext, useState, useEffect } from 'react'
import { authApi } from '../services/authApi'
import type { UserProfile } from '../types'
import toast from 'react-hot-toast'

interface AuthContextType {
  user: UserProfile | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (accessToken: string, refreshToken: string, email: string) => Promise<void>
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
    } catch (error) {
      localStorage.removeItem('auth_token')
      localStorage.removeItem('refresh_token')
      localStorage.removeItem('user_email')
      localStorage.removeItem('user_id')
      setUser(null)
    }
  }

  useEffect(() => {
    const initializeAuth = async () => {
      const token = localStorage.getItem('auth_token')
      if (token) {
        await refreshProfile()
      }
      setIsLoading(false)
    }
    initializeAuth()
  }, [])

  const login = async (accessToken: string, refreshToken: string, email: string) => {
    localStorage.setItem('auth_token', accessToken)
    localStorage.setItem('refresh_token', refreshToken)
    localStorage.setItem('user_email', email)
    await refreshProfile()
  }

  const logout = async () => {
    const refreshToken = localStorage.getItem('refresh_token')
    if (refreshToken) {
      try {
        await authApi.logout({ refreshToken })
      } catch (err) {
        console.error('Logout request failed', err)
      }
    }
    localStorage.removeItem('auth_token')
    localStorage.removeItem('refresh_token')
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
