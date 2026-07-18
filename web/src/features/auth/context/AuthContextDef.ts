import { createContext } from 'react'
import type { UserProfile } from '../types'

export interface AuthContextType {
  user: UserProfile | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (accessToken: string, email: string) => Promise<void>
  logout: () => Promise<void>
  refreshProfile: () => Promise<void>
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined)
