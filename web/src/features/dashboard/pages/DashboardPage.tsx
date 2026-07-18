import React from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Plus, FolderClosed, FileText, Cpu, ExternalLink, ChevronRight } from 'lucide-react'
import { PageHeader } from '@/shared/components/ui/PageHeader'
import { DashboardCard } from '@/shared/components/ui/DashboardCard'
import { SectionCard } from '@/shared/components/ui/SectionCard'
import { StatusBadge } from '@/shared/components/ui/StatusBadge'
import { LoadingSpinner } from '@/shared/components/ui/LoadingSpinner'
import { useProjects } from '@/shared/services/api/projects'
import { ErrorState } from '@/shared/components/ui/ErrorState'
import { useAuth } from '@/features/auth/context/AuthContext'

export const DashboardPage: React.FC = () => {
  const navigate = useNavigate()
  const { user } = useAuth()
  const { data: projects, isLoading, isError, refetch } = useProjects()

  if (isLoading) {
    return <LoadingSpinner size="lg" className="min-h-[50vh]" />
  }

  // Get recent 4 projects to display
  const recentProjects = projects ? projects.slice(0, 4) : []
  const processingProjects = projects
    ? projects.filter((p) => p.status === 'processing' || p.status === 'pending')
    : []

  // Stats Card data
  const stats = [
    {
      title: 'Projects',
      value: projects ? projects.length : 0,
      icon: FolderClosed,
      trend: { value: 'Live count', positive: true },
    },
    {
      title: 'Transcriptions',
      value: projects ? projects.filter((p) => p.status === 'completed').length : 0,
      icon: FileText,
      trend: { value: 'ASR Whispers', positive: true },
    },
    {
      title: 'Processing Jobs',
      value: processingProjects.length,
      icon: Cpu,
      description: 'Running on Hangfire',
    },
  ]

  // Recent Activities
  const recentActivities = [
    { id: 'act-1', text: 'Project speech transcription completed', time: '10 mins ago' },
    { id: 'act-2', text: 'Spanish subtitles generated', time: '2 hours ago' },
    { id: 'act-3', text: 'New media file uploaded', time: '5 hours ago' },
  ]

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Welcome Section */}
      <PageHeader
        title={`Welcome back, ${user?.firstName || 'User'}!`}
        description="Here is the overview of your VideoHub AI workspace processing activities."
        actions={
          <Link
            to="/projects"
            className="inline-flex items-center gap-1.5 rounded-lg bg-accent px-4 py-2.5 text-sm font-semibold text-white shadow-custom-sm hover:bg-accent-hover transition-colors duration-150"
          >
            <Plus className="h-4 w-4" />
            Create Project
          </Link>
        }
      />

      {/* Statistics Cards Grid */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 animate-slide-up" style={{ animationDelay: '0.1s' }}>
        {stats.map((stat, i) => (
          <DashboardCard
            key={i}
            title={stat.title}
            value={stat.value}
            icon={stat.icon}
            description={stat.description}
            trend={stat.trend}
          />
        ))}
      </div>

      {/* Main Grid content: Recent Projects & Sidebar Activities */}
      <div className="grid gap-6 lg:grid-cols-3 animate-slide-up" style={{ animationDelay: '0.2s' }}>
        {/* Left Column: Recent Projects Table */}
        <div className="lg:col-span-2 space-y-6">
          <SectionCard
            title="Recent Projects"
            subtitle="Your workspace's recently modified videos."
            actions={
              <button
                onClick={() => navigate('/projects')}
                className="inline-flex items-center gap-1 text-xs font-semibold text-accent hover:text-accent-hover transition-colors"
              >
                View All
                <ChevronRight className="h-3 w-3" />
              </button>
            }
            padding={false}
          >
            {isError ? (
              <div className="p-8">
                <ErrorState onRetry={() => refetch()} />
              </div>
            ) : recentProjects.length === 0 ? (
              <div className="p-8 text-center text-text-muted text-xs">
                No projects found. Create a project to see details here.
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm border-collapse">
                  <thead>
                    <tr className="border-b border-border-custom bg-slate-50/50 text-text-muted font-medium select-none">
                      <th className="px-6 py-3">Project Name</th>
                      <th className="px-6 py-3">Language</th>
                      <th className="px-6 py-3">Status</th>
                      <th className="px-6 py-3">Created</th>
                      <th className="px-6 py-3 text-right">Action</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border-custom/50">
                    {recentProjects.map((project) => (
                      <tr key={project.id} className="hover:bg-slate-50/40 transition-colors">
                        <td className="px-6 py-4 font-medium text-text-main">{project.name}</td>
                        <td className="px-6 py-4 text-text-muted">
                          {project.originalLanguage.toUpperCase()}
                        </td>
                        <td className="px-6 py-4">
                          <StatusBadge status={project.status} />
                        </td>
                        <td className="px-6 py-4 text-text-muted">
                          {new Date(project.createdAt).toLocaleDateString()}
                        </td>
                        <td className="px-6 py-4 text-right">
                          <button
                            onClick={() => navigate(`/projects/${project.id}`)}
                            className="inline-flex items-center gap-1 text-xs font-semibold text-accent hover:text-accent-hover"
                          >
                            Details
                            <ExternalLink className="h-3 w-3" />
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </SectionCard>
        </div>

        {/* Right Column: Processing Queue & Activity logs */}
        <div className="space-y-6">
          {/* Active Processing Queue */}
          <SectionCard title="Processing Queue" subtitle="Background jobs executing now.">
            <div className="space-y-3.5">
              {processingProjects.length > 0 ? (
                processingProjects.map((p) => (
                  <div
                    key={p.id}
                    className="flex items-center justify-between p-3 rounded-lg border border-border-custom/60 bg-slate-50/50"
                  >
                    <div className="min-w-0 flex-1">
                      <p className="text-xs font-semibold text-text-main truncate m-0">
                        Transcribing: {p.name}
                      </p>
                      <p className="text-[10px] text-text-muted mt-0.5 m-0">
                        Project ID: {p.id.slice(0, 8)}
                      </p>
                    </div>
                    <StatusBadge status={p.status} />
                  </div>
                ))
              ) : (
                <div className="text-center text-xs text-text-muted py-4">
                  No active processing jobs.
                </div>
              )}
            </div>
          </SectionCard>

          {/* Recent Activity Log panel */}
          <SectionCard title="Recent Activity (Preview)">
            <div className="relative border-l border-border-custom ml-1.5 pl-4 space-y-4">
              {recentActivities.map((act) => (
                <div key={act.id} className="relative">
                  <span className="absolute -left-[21px] top-1 flex h-2 w-2 rounded-full bg-accent ring-4 ring-card" />
                  <p className="text-xs text-text-main font-medium leading-normal m-0">
                    {act.text}
                  </p>
                  <p className="text-[10px] text-text-muted mt-0.5 m-0">{act.time}</p>
                </div>
              ))}
            </div>
          </SectionCard>
        </div>
      </div>
    </div>
  )
}
