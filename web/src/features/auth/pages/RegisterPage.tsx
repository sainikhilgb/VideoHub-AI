import React, { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { authApi } from '../services/authApi'
import { Play, Eye, EyeOff, Loader2 } from 'lucide-react'
import toast from 'react-hot-toast'

export const RegisterPage: React.FC = () => {
  const navigate = useNavigate()
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errors, setErrors] = useState<{
    firstName?: string
    lastName?: string
    email?: string
    password?: string
    confirmPassword?: string
  }>({})

  const validate = () => {
    const newErrors: typeof errors = {}
    if (!firstName.trim()) newErrors.firstName = 'First name is required'
    if (!lastName.trim()) newErrors.lastName = 'Last name is required'
    if (!email) {
      newErrors.email = 'Email is required'
    } else if (!/\S+@\S+\.\S+/.test(email)) {
      newErrors.email = 'Invalid email address'
    }
    if (!password) {
      newErrors.password = 'Password is required'
    } else {
      if (password.length < 8) newErrors.password = 'Password must be at least 8 characters'
      if (!/[A-Z]/.test(password)) newErrors.password = 'Must contain at least one uppercase letter'
      if (!/[a-z]/.test(password)) newErrors.password = 'Must contain at least one lowercase letter'
      if (!/[0-9]/.test(password)) newErrors.password = 'Must contain at least one digit'
      if (!/[^a-zA-Z0-9]/.test(password)) newErrors.password = 'Must contain at least one special character'
    }
    if (confirmPassword !== password) {
      newErrors.confirmPassword = 'Passwords do not match'
    }
    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!validate()) return

    setIsSubmitting(true)
    try {
      await authApi.register({
        firstName,
        lastName,
        email,
        password,
      })
      toast.success('Registration successful! Please sign in.')
      navigate('/login')
    } catch (error: any) {
      console.error('Registration error', error)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-app-bg px-4 py-12 sm:px-6 lg:px-8 font-sans">
      <div className="w-full max-w-md space-y-8 bg-card p-8 rounded-xl border border-border-custom shadow-custom-lg">
        {/* Header */}
        <div className="flex flex-col items-center justify-center text-center">
          <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-accent text-white shadow-custom-md mb-4 transition-transform hover:scale-105">
            <Play className="h-6 w-6 fill-current ml-0.5" />
          </div>
          <h2 className="text-2xl font-bold tracking-tight text-text-main">
            Create your account
          </h2>
          <p className="mt-2 text-sm text-text-muted">
            Already have an account?{' '}
            <Link to="/login" className="font-semibold text-accent hover:text-accent-hover transition-colors">
              Sign in
            </Link>
          </p>
        </div>

        {/* Form */}
        <form className="mt-8 space-y-5" onSubmit={handleSubmit}>
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label htmlFor="firstName" className="block text-sm font-medium text-text-main mb-1.5">
                  First Name
                </label>
                <input
                  id="firstName"
                  type="text"
                  placeholder="John"
                  value={firstName}
                  onChange={(e) => {
                    setFirstName(e.target.value)
                    if (errors.firstName) setErrors((prev) => ({ ...prev, firstName: undefined }))
                  }}
                  className="w-full rounded-lg border border-border-custom bg-card px-3 py-2 text-sm text-text-main shadow-custom-sm placeholder:text-text-muted/60 focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent transition-colors"
                />
                {errors.firstName && (
                  <p className="mt-1.5 text-xs text-danger font-medium">{errors.firstName}</p>
                )}
              </div>

              <div>
                <label htmlFor="lastName" className="block text-sm font-medium text-text-main mb-1.5">
                  Last Name
                </label>
                <input
                  id="lastName"
                  type="text"
                  placeholder="Doe"
                  value={lastName}
                  onChange={(e) => {
                    setLastName(e.target.value)
                    if (errors.lastName) setErrors((prev) => ({ ...prev, lastName: undefined }))
                  }}
                  className="w-full rounded-lg border border-border-custom bg-card px-3 py-2 text-sm text-text-main shadow-custom-sm placeholder:text-text-muted/60 focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent transition-colors"
                />
                {errors.lastName && (
                  <p className="mt-1.5 text-xs text-danger font-medium">{errors.lastName}</p>
                )}
              </div>
            </div>

            <div>
              <label htmlFor="email" className="block text-sm font-medium text-text-main mb-1.5">
                Email address
              </label>
              <input
                id="email"
                type="email"
                placeholder="you@example.com"
                value={email}
                onChange={(e) => {
                  setEmail(e.target.value)
                  if (errors.email) setErrors((prev) => ({ ...prev, email: undefined }))
                }}
                className="w-full rounded-lg border border-border-custom bg-card px-3 py-2 text-sm text-text-main shadow-custom-sm placeholder:text-text-muted/60 focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent transition-colors"
              />
              {errors.email && (
                <p className="mt-1.5 text-xs text-danger font-medium">{errors.email}</p>
              )}
            </div>

            <div>
              <label htmlFor="password" className="block text-sm font-medium text-text-main mb-1.5">
                Password
              </label>
              <div className="relative">
                <input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  placeholder="••••••••"
                  value={password}
                  onChange={(e) => {
                    setPassword(e.target.value)
                    if (errors.password) setErrors((prev) => ({ ...prev, password: undefined }))
                  }}
                  className="w-full rounded-lg border border-border-custom bg-card pl-3 pr-10 py-2 text-sm text-text-main shadow-custom-sm placeholder:text-text-muted/60 focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent transition-colors"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute inset-y-0 right-0 flex items-center pr-3 text-text-muted hover:text-text-main focus:outline-none"
                >
                  {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              {errors.password && (
                <p className="mt-1.5 text-xs text-danger font-medium">{errors.password}</p>
              )}
            </div>

            <div>
              <label htmlFor="confirmPassword" className="block text-sm font-medium text-text-main mb-1.5">
                Confirm Password
              </label>
              <input
                id="confirmPassword"
                type={showPassword ? 'text' : 'password'}
                placeholder="••••••••"
                value={confirmPassword}
                onChange={(e) => {
                  setConfirmPassword(e.target.value)
                  if (errors.confirmPassword) setErrors((prev) => ({ ...prev, confirmPassword: undefined }))
                }}
                className="w-full rounded-lg border border-border-custom bg-card px-3 py-2 text-sm text-text-main shadow-custom-sm placeholder:text-text-muted/60 focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent transition-colors"
              />
              {errors.confirmPassword && (
                <p className="mt-1.5 text-xs text-danger font-medium">{errors.confirmPassword}</p>
              )}
            </div>
          </div>

          <div className="pt-2">
            <button
              type="submit"
              disabled={isSubmitting}
              className="flex w-full items-center justify-center gap-2 rounded-lg bg-accent px-4 py-2.5 text-sm font-semibold text-white shadow-custom-sm hover:bg-accent-hover focus:outline-none focus:ring-2 focus:ring-accent focus:ring-offset-2 disabled:bg-accent/70 disabled:cursor-not-allowed transition-colors duration-150"
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Creating account...
                </>
              ) : (
                'Create account'
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
