import React from 'react'
import { useOutletContext } from 'react-router-dom'
import { Cpu, RefreshCw } from 'lucide-react'
import { SectionCard } from '@/shared/components/ui/SectionCard'
import { StatusBadge } from '@/shared/components/ui/StatusBadge'
import type { Project } from '@/shared/services/api/projects'
import toast from 'react-hot-toast'

export const ProjectJobsTab: React.FC = () => {
  const project = useOutletContext<Project>()

  // Mock list of project processing jobs
  const mockJobs = [
    {
      id: 'job-101',
      name: 'Speech-to-Text Transcription',
      type: 'transcription',
      status: project.status, // bind status dynamically to match project
      duration:
        project.status.toLowerCase() === 'completed'
          ? '1m 24s'
          : project.status.toLowerCase() === 'processing'
            ? 'calculating...'
            : '--',
      start: new Date(project.createdAt).toLocaleString(),
    },
    {
      id: 'job-102',
      name: 'Audio Track Extraction',
      type: 'extraction',
      status: project.status.toLowerCase() === 'created' ? 'pending' : 'completed',
      duration: '14 seconds',
      start: new Date(project.createdAt).toLocaleString(),
    },
  ]

  const handleRetry = (name: string) => {
    toast.success(`Retrying processing job: ${name}...`)
  }

  return (
    <div className="space-y-6">
      <SectionCard
        title="Background Processing Jobs"
        subtitle="Review active and historical AI worker tasks enqueued for this project."
        actions={
          <button
            onClick={() => toast.success('Refreshing background jobs queue...')}
            className="p-1.5 rounded-lg border border-border-custom bg-card text-text-muted hover:bg-slate-50 hover:text-text-main transition-colors focus:outline-none"
            title="Refresh logs"
          >
            <RefreshCw className="h-4 w-4" />
          </button>
        }
        padding={false}
      >
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm border-collapse">
            <thead>
              <tr className="border-b border-border-custom bg-slate-50/50 text-text-muted font-medium select-none">
                <th className="px-6 py-3.5">Job ID</th>
                <th className="px-6 py-3.5">Task Name</th>
                <th className="px-6 py-3.5">Status</th>
                <th className="px-6 py-3.5">Duration</th>
                <th className="px-6 py-3.5">Start Time</th>
                <th className="px-6 py-3.5 text-right">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border-custom/50">
              {mockJobs.map((job) => (
                <tr key={job.id} className="hover:bg-slate-50/30 transition-colors">
                  <td className="px-6 py-4 font-mono text-xs text-text-muted">{job.id}</td>
                  <td className="px-6 py-4 font-medium text-text-main flex items-center gap-2">
                    <Cpu className="h-4 w-4 text-accent shrink-0" />
                    {job.name}
                  </td>
                  <td className="px-6 py-4">
                    <StatusBadge status={job.status} />
                  </td>
                  <td className="px-6 py-4 text-text-muted">{job.duration}</td>
                  <td className="px-6 py-4 text-text-muted">{job.start}</td>
                  <td className="px-6 py-4 text-right">
                    <button
                      onClick={() => handleRetry(job.name)}
                      className="text-xs font-semibold text-accent hover:text-accent-hover transition-colors"
                    >
                      Retry
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </SectionCard>
    </div>
  )
}
export default ProjectJobsTab
