export interface UserProfile {
  id: string
  email: string
  firstName: string
  lastName: string
  displayName: string
  role: string
  isActive: boolean
  createdAt: string
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  expiresIn: number
}

export interface RegisterResponse {
  id: string
  email: string
  firstName: string
  lastName: string
}

export interface LoginRequest {
  email: string
  password?: string
}

export interface RegisterRequest {
  email: string
  password?: string
  confirmPassword?: string
  firstName: string
  lastName: string
}
