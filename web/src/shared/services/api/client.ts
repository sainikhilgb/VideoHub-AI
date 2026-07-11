import axios, { AxiosError } from 'axios'
import type { AxiosInstance, InternalAxiosRequestConfig, AxiosResponse } from 'axios'
import toast from 'react-hot-toast'

// Retrieve API Base URL from environment variables
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api'

export const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  timeout: 15000,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request Interceptor: Attach authentication token or correlation headers
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem('auth_token')
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

// Response Interceptor & Global Error Handler
apiClient.interceptors.response.use(
  (response: AxiosResponse) => {
    return response
  },
  (error: AxiosError) => {
    const status = error.response?.status
    const data = error.response?.data as { detail?: string; message?: string } | undefined

    // Extract readable error message matching Problem Details structure
    const errorMessage =
      data?.detail || data?.message || error.message || 'An unexpected error occurred'

    // Handle global HTTP status codes
    switch (status) {
      case 400:
        toast.error(`Bad Request: ${errorMessage}`)
        break
      case 401:
        toast.error('Unauthorized. Please log in again.')
        // Redirect to login or clear credentials in the future
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

    return Promise.reject(error)
  },
)
