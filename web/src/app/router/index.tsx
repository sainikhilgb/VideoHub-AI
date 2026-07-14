import { createBrowserRouter, Navigate } from 'react-router-dom'
import { AppLayout } from '@/app/layouts/AppLayout'
import { DashboardPage } from '@/features/dashboard/pages/DashboardPage'
import { ProjectsPage } from '@/features/projects/pages/ProjectsPage'
import { ProjectLayout } from '@/features/projects/workspace/ProjectLayout'
import { ProjectOverviewTab } from '@/features/projects/workspace/tabs/ProjectOverviewTab'
import { ProjectMediaTab } from '@/features/projects/workspace/tabs/ProjectMediaTab'
import { ProjectTranscriptTab } from '@/features/projects/workspace/tabs/ProjectTranscriptTab'
import { ProjectCaptionsTab } from '@/features/projects/workspace/tabs/ProjectCaptionsTab'
import { ProjectTranslationTab } from '@/features/projects/workspace/tabs/ProjectTranslationTab'
import { ProjectJobsTab } from '@/features/projects/workspace/tabs/ProjectJobsTab'
import { ProjectSettingsTab } from '@/features/projects/workspace/tabs/ProjectSettingsTab'
import { JobsPage } from '@/features/jobs/pages/JobsPage'
import { SettingsPage } from '@/features/settings/pages/SettingsPage'
import { NotFoundPage } from '@/shared/components/layout/NotFoundPage'
import { ProtectedRoute } from '@/features/auth/components/ProtectedRoute'
import { PublicRoute } from '@/features/auth/components/PublicRoute'
import { LoginPage } from '@/features/auth/pages/LoginPage'
import { RegisterPage } from '@/features/auth/pages/RegisterPage'

export const router = createBrowserRouter([
  // Public Routes (unauthenticated users)
  {
    element: <PublicRoute />,
    children: [
      {
        path: 'login',
        element: <LoginPage />,
      },
      {
        path: 'register',
        element: <RegisterPage />,
      },
    ],
  },
  // Protected Routes (authenticated users)
  {
    element: <ProtectedRoute />,
    children: [
      {
        path: '/',
        element: <AppLayout />,
        children: [
          {
            index: true,
            element: <DashboardPage />,
          },
          {
            path: 'projects',
            children: [
              {
                index: true,
                element: <ProjectsPage />,
              },
              {
                path: ':projectId',
                element: <ProjectLayout />,
                children: [
                  {
                    index: true,
                    element: <ProjectOverviewTab />,
                  },
                  {
                    path: 'media',
                    element: <ProjectMediaTab />,
                  },
                  {
                    path: 'transcript',
                    element: <ProjectTranscriptTab />,
                  },
                  {
                    path: 'captions',
                    element: <ProjectCaptionsTab />,
                  },
                  {
                    path: 'translations',
                    element: <ProjectTranslationTab />,
                  },
                  {
                    path: 'jobs',
                    element: <ProjectJobsTab />,
                  },
                  {
                    path: 'settings',
                    element: <ProjectSettingsTab />,
                  },
                ],
              },
            ],
          },
          {
            path: 'jobs',
            element: <JobsPage />,
          },
          {
            path: 'settings',
            element: <SettingsPage />,
          },
          {
            path: '404',
            element: <NotFoundPage />,
          },
          {
            path: '*',
            element: <Navigate to="/404" replace />,
          },
        ],
      },
    ],
  },
])
