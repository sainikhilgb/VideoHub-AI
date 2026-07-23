import React from 'react'
import { useParams, Link, useLocation, Outlet } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { StatusBadge } from '@/shared/components/ui/StatusBadge'
import { LoadingSpinner } from '@/shared/components/ui/LoadingSpinner'
import { ErrorState } from '@/shared/components/ui/ErrorState'
import { useProject } from '@/shared/services/api/projects'
import { useSignalR } from '@/shared/hooks/useSignalR'
import clsx from 'clsx'

export const ProjectLayout: React.FC = () => {
  const { projectId } = useParams<{ projectId: string }>()
  const location = useLocation()
  const queryClient = useQueryClient()
  const { data: project, isLoading, isError, refetch } = useProject(projectId)

  useSignalR({
    projectId,
    onJobUpdate: (event) => {
      console.log('SignalR: Received Job Update', event)
      queryClient.invalidateQueries({ queryKey: ['project', projectId] })
      queryClient.invalidateQueries({ queryKey: ['projectTranscript', projectId] })
      queryClient.invalidateQueries({ queryKey: ['projectCaptions', projectId] })
      queryClient.invalidateQueries({ queryKey: ['jobStatus', event.jobId] })
    },
    onCaptionFileUpdate: (event) => {
      console.log('SignalR: Received Caption File Update', event)
      queryClient.invalidateQueries({ queryKey: ['projectCaptions', projectId] })
    },
    onCombinedMediaUpdate: (event) => {
      console.log('SignalR: Received Combined Media Update', event)
      queryClient.invalidateQueries({ queryKey: ['projectCombinedMedia', projectId] })
    },
  })

  if (isLoading) {
    return <LoadingSpinner size="lg" className="min-h-[50vh]" />
  }

  if (isError || !project) {
    return (
      <div className="max-w-md mx-auto mt-12">
        <ErrorState onRetry={() => refetch()} />
      </div>
    )
  }

  // Active path checking
  const getTabClass = (path: string) => {
    const isTabActive =
      path === ''
        ? location.pathname === `/projects/${projectId}` ||
          location.pathname === `/projects/${projectId}/`
        : location.pathname.endsWith(path)

    return clsx(
      'px-4 py-2 text-sm font-medium border-b-2 transition-all duration-150 select-none whitespace-nowrap',
      isTabActive
        ? 'border-accent text-accent font-semibold'
        : 'border-transparent text-text-muted hover:text-text-main hover:border-border-custom',
    )
  }

  // Disable transcript/captions/translations tabs if no media has been uploaded
  const hasNoMedia =
    project.status.toLowerCase() === 'created' || project.status.toLowerCase() === 'idle'

  const tabs = [
    { name: 'Overview', path: '' },
    { name: 'Media', path: 'media' },
    { name: 'Transcript', path: 'transcript', disabled: hasNoMedia },
    { name: 'Captions', path: 'captions', disabled: hasNoMedia },
    { name: 'Translations', path: 'translations', disabled: hasNoMedia },
    { name: 'Jobs', path: 'jobs' },
    { name: 'Settings', path: 'settings' },
  ]

  return (
    <div className="space-y-6">
      {/* Workspace Header */}
      <div className="flex flex-col gap-2.5 sm:flex-row sm:items-center sm:justify-between border-b border-border-custom pb-4">
        <div>
          <span className="text-xs font-semibold text-accent uppercase tracking-wider">
            Project Workspace
          </span>
          <h1 className="text-2xl font-bold tracking-tight text-text-main font-sans mt-0.5 mb-0">
            {project.name}
          </h1>
        </div>
        <div className="flex items-center gap-3">
          <span className="text-xs text-text-muted">
            Source Language:{' '}
            <span className="font-semibold text-text-main">
              {project.originalLanguage.toUpperCase()}
            </span>
          </span>
          <StatusBadge status={project.status} />
        </div>
      </div>

      {/* Persistent Tabs Navigation */}
      <div className="border-b border-border-custom overflow-x-auto custom-scrollbar">
        <nav className="flex space-x-2 -mb-px">
          {tabs.map((tab) => {
            if (tab.disabled) {
              return (
                <span
                  key={tab.name}
                  className="px-4 py-2 text-sm font-medium text-slate-300 border-b-2 border-transparent cursor-not-allowed select-none"
                  title="Upload media first to unlock this tab"
                >
                  {tab.name}
                </span>
              )
            }
            return (
              <Link
                key={tab.name}
                to={
                  tab.path === '' ? `/projects/${projectId}` : `/projects/${projectId}/${tab.path}`
                }
                className={getTabClass(tab.path)}
              >
                {tab.name}
              </Link>
            )
          })}
        </nav>
      </div>

      {/* Render Active Tab Children Page, passing project data as React Router Outlet Context */}
      <div className="animate-fade-in py-2">
        <Outlet context={project} />
      </div>
    </div>
  )
}
export default ProjectLayout
