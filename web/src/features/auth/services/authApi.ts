import { apiClient } from '@/shared/services/api/client'
import type { LoginResponse, RegisterResponse, UserProfile } from '../types'

export const authApi = {
  register: async (data: any): Promise<RegisterResponse> => {
    const response = await apiClient.post<RegisterResponse>('/v1/auth/register', data)
    return response.data
  },

  login: async (data: any): Promise<LoginResponse> => {
    const response = await apiClient.post<LoginResponse>('/v1/auth/login', data)
    return response.data
  },

  refresh: async (data: { refreshToken: string }): Promise<LoginResponse> => {
    const response = await apiClient.post<LoginResponse>('/v1/auth/refresh', data)
    return response.data
  },

  logout: async (data: { refreshToken: string }): Promise<void> => {
    await apiClient.post('/v1/auth/logout', data)
  },

  getCurrentUser: async (): Promise<UserProfile> => {
    const response = await apiClient.get<UserProfile>('/v1/auth/me')
    return response.data
  },
}
