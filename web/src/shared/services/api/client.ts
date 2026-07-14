import axios from 'axios'
import type { AxiosInstance, InternalAxiosRequestConfig, AxiosResponse } from 'axios'
import toast from 'react-hot-toast'

// Retrieve API Base URL from environment variables, restricting localhost to development
const getApiBaseUrl = (): string => {
  const envUrl = import.meta.env.VITE_API_BASE_URL
  if (envUrl) return envUrl

  const isDev = import.meta.env.DEV || import.meta.env.MODE === 'development'
  if (isDev) {
    return 'http://localhost:5000/api'
  }

  throw new Error('VITE_API_BASE_URL environment variable is missing in production environment.')
}

const API_BASE_URL = getApiBaseUrl()

let inMemoryToken: string | null = null

export const setAccessToken = (token: string | null) => {
  inMemoryToken = token
}

export const getAccessToken = () => {
  return inMemoryToken
}

export const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  timeout: 15000,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request Interceptor: Attach authentication token or correlation headers
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = getAccessToken()
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`
    }

    // Attach Correlation ID for logging matching backend architecture
    const correlationId = crypto.randomUUID()
    if (config.headers) {
      config.headers['X-Correlation-ID'] = correlationId
    }

    return config
  },
  (error) => {
    return Promise.reject(error)
  },
)

let isRefreshing = false
let failedQueue: any[] = []

const processQueue = (error: any, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error)
    } else {
      prom.resolve(token)
    }
  })
  failedQueue = []
}

// Response Interceptor & Global Error Handler
apiClient.interceptors.response.use(
  (response: AxiosResponse) => {
    return response
  },
  async (error: any) => {
    const originalRequest = error.config
    const status = error.response?.status
    const data = error.response?.data as { detail?: string; message?: string } | undefined

    // Extract readable error message matching Problem Details structure
    const errorMessage =
      data?.detail || data?.message || error.message || 'An unexpected error occurred'

    const isAuthRequest = originalRequest.url && (
      originalRequest.url.endsWith('/v1/auth/login') ||
      originalRequest.url.endsWith('/v1/auth/register') ||
      originalRequest.url.endsWith('/v1/auth/refresh') ||
      originalRequest.url.endsWith('/v1/auth/logout')
    )

    // Handle JWT Token Expired / Refresh
    if (status === 401 && !originalRequest._retry && !isAuthRequest) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject })
        })
          .then((token) => {
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${token}`
            }
            return apiClient(originalRequest)
          })
          .catch((err) => {
            return Promise.reject(err)
          })
      }

      const hasSession = !!localStorage.getItem('user_id')
      if (hasSession) {
        originalRequest._retry = true
        isRefreshing = true

        try {
          // Dynamic import to avoid early circular evaluation
          const { authApi } = await import('@/features/auth/services/authApi')
          const response = await authApi.refresh()
          const newAccessToken = response.accessToken

          setAccessToken(newAccessToken)

          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${newAccessToken}`
          }

          processQueue(null, newAccessToken)
          return apiClient(originalRequest)
        } catch (refreshError) {
          processQueue(refreshError, null)
          setAccessToken(null)
          localStorage.removeItem('user_email')
          localStorage.removeItem('user_id')
          window.location.href = '/login'
          return Promise.reject(refreshError)
        } finally {
          isRefreshing = false
        }
      }
    }

    // Handle other global HTTP status codes (suppressed for GET requests to let TanStack QueryCache handle final failures)
    const isGet = originalRequest?.method?.toLowerCase() === 'get'
    if (!isGet && !originalRequest.url?.includes('/v1/auth/login')) {
      switch (status) {
        case 400:
          toast.error(`Bad Request: ${errorMessage}`)
          break
        case 401:
          toast.error('Unauthorized. Please log in again.')
          break
        case 403:
          toast.error('Forbidden: You do not have permissions for this action.')
          break
        case 404:
          // Do not toast for 404s if handled inline by pages
          break
        case 500:
          toast.error(`Server Error: ${errorMessage}`)
          break
        case 503:
          toast.error('Service Unavailable. Please try again later.')
          break
        default:
          toast.error(errorMessage)
      }
    }

    return Promise.reject(error)
  },
)

export default apiClient
