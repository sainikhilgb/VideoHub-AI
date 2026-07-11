import React from 'react'
import { PageHeader } from '@/shared/components/ui/PageHeader'
import { SectionCard } from '@/shared/components/ui/SectionCard'
import { StatusBadge } from '@/shared/components/ui/StatusBadge'
import { Cpu, RefreshCw } from 'lucide-react'
import toast from 'react-hot-toast'

export const JobsPage: React.FC = () => {
  const mockJobs = [
    {
      id: 'job-101',
      name: 'Speech-to-Text Transcription',
      type: 'transcription',
      status: 'processing',
      duration: '1m 24s',
      start: '2026-07-11 19:15',
    },
    {
      id: 'job-102',
      name: 'Audio Track Extraction',
      type: 'extraction',
      status: 'completed',
      duration: '22s',
      start: '2026-07-11 19:14',
    },
    {
      id: 'job-103',
      name: 'Spanish Subtitle Translation',
      type: 'translation',
      status: 'pending',
      duration: '--',
      start: '2026-07-11 19:15',
    },
  ]

  return (
    <div className="space-y-6">
      <PageHeader
        title="Jobs Queue"
        description="Monitor active and historical background AI processing tasks."
        actions={
          <button
            onClick={() => toast.success('Refreshing background jobs queue...')}
            className="inline-flex items-center gap-1.5 rounded-lg border border-border-custom bg-card px-4 py-2 text-sm font-semibold text-text-main shadow-custom-sm hover:bg-slate-50 transition-colors"
          >
            <RefreshCw className="h-4 w-4 text-text-muted" />
            Refresh Queue (Preview)
          </button>
        }
      />

      <SectionCard title="Active & Recent Jobs" padding={false}>
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm border-collapse">
            <thead>
              <tr className="border-b border-border-custom bg-slate-50/50 text-text-muted font-medium select-none">
                <th className="px-6 py-3">Job ID</th>
                <th className="px-6 py-3">Task Name</th>
                <th className="px-6 py-3">Status</th>
                <th className="px-6 py-3">Duration</th>
                <th className="px-6 py-3">Start Time</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border-custom/50">
              {mockJobs.map((job) => (
                <tr key={job.id} className="hover:bg-slate-50/40 transition-colors">
                  <td className="px-6 py-4 font-mono text-xs text-text-muted">{job.id}</td>
                  <td className="px-6 py-4 font-medium text-text-main flex items-center gap-2">
                    <Cpu className="h-4 w-4 text-accent" />
                    {job.name}
                  </td>
                  <td className="px-6 py-4">
                    <StatusBadge status={job.status} />
                  </td>
                  <td className="px-6 py-4 text-text-muted">{job.duration}</td>
                  <td className="px-6 py-4 text-text-muted">{job.start}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </SectionCard>
    </div>
  )
}
