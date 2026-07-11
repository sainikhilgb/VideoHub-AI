import React from 'react'
import { useOutletContext, Link, useNavigate } from 'react-router-dom'
import {
  Upload,
  FileText,
  Subtitles,
  Languages,
  Trash2,
  Calendar,
  Layers,
  Sparkles,
} from 'lucide-react'
import { SectionCard } from '@/shared/components/ui/SectionCard'
import { StatusBadge } from '@/shared/components/ui/StatusBadge'
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog'
import { useDeleteProject, type Project } from '@/shared/services/api/projects'
import toast from 'react-hot-toast'

export const ProjectOverviewTab: React.FC = () => {
  const project = useOutletContext<Project>()
  const navigate = useNavigate()
  const deleteProjectMutation = useDeleteProject()
  const [deleteConfirmOpen, setDeleteConfirmOpen] = React.useState(false)

  const handleDeleteProject = async () => {
    toast.promise(deleteProjectMutation.mutateAsync(project.id), {
      loading: 'Deleting project...',
      success: () => {
        navigate('/projects')
        return 'Project deleted successfully'
      },
      error: 'Failed to delete project',
    })
    setDeleteConfirmOpen(false)
  }

  const isCreated = project.status.toLowerCase() === 'created'
  const isProcessing =
    project.status.toLowerCase() === 'processing' || project.status.toLowerCase() === 'pending'
  const isCompleted = project.status.toLowerCase() === 'completed'

  return (
    <div className="grid gap-6 md:grid-cols-3">
      {/* Main summary details */}
      <div className="md:col-span-2 space-y-6">
        <SectionCard title="Project Summary" subtitle="General context parameters.">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="flex items-center gap-3.5 p-4 rounded-xl border border-border-custom bg-slate-50/40">
              <Calendar className="h-5 w-5 text-accent shrink-0" />
              <div>
                <p className="text-[10px] uppercase font-bold text-text-muted m-0">
                  Initialized On
                </p>
                <p className="text-sm font-semibold text-text-main m-0">
                  {new Date(project.createdAt).toLocaleString()}
                </p>
              </div>
            </div>

            <div className="flex items-center gap-3.5 p-4 rounded-xl border border-border-custom bg-slate-50/40">
              <Layers className="h-5 w-5 text-accent shrink-0" />
              <div>
                <p className="text-[10px] uppercase font-bold text-text-muted m-0">Current Stage</p>
                <div className="mt-0.5">
                  <StatusBadge status={project.status} />
                </div>
              </div>
            </div>
          </div>
        </SectionCard>

        {isCreated && (
          <div className="flex flex-col items-center justify-center text-center p-8 rounded-xl border border-dashed border-border-custom bg-card select-none">
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-slate-50 border border-border-custom text-text-muted mb-4">
              <Upload className="h-5 w-5 text-accent" />
            </div>
            <h3 className="text-sm font-semibold text-text-main m-0">No media uploaded yet</h3>
            <p className="text-xs text-text-muted mt-1.5 mb-4 max-w-sm">
              You must upload an audio or video file before you can start transcription and
              generating captions.
            </p>
            <Link
              to={`/projects/${project.id}/media`}
              className="inline-flex items-center gap-1.5 rounded-lg bg-accent px-4 py-2 text-xs font-semibold text-white shadow-custom-sm hover:bg-accent-hover transition-colors"
            >
              <Upload className="h-4 w-4" />
              Go to Media Tab
            </Link>
          </div>
        )}

        {isProcessing && (
          <div className="flex flex-col items-center justify-center text-center p-8 rounded-xl border border-border-custom bg-card select-none">
            <Loader className="h-10 w-10 text-accent animate-spin mb-4" />
            <h3 className="text-sm font-semibold text-text-main m-0">
              AI Speech-to-Text Processing
            </h3>
            <p className="text-xs text-text-muted mt-1.5 max-w-sm">
              We are transcribing speech and aligning word timestamps. Check the Jobs tab for
              real-time logs.
            </p>
          </div>
        )}

        {isCompleted && (
          <SectionCard title="Live Output Details" subtitle="Ready preview status.">
            <div className="flex items-center gap-3.5 p-4 rounded-xl border border-emerald-100 bg-emerald-50/20 text-success">
              <Sparkles className="h-5 w-5 shrink-0" />
              <div>
                <p className="text-xs font-semibold m-0">
                  Transcripts & captions successfully generated!
                </p>
                <p className="text-[11px] text-text-muted mt-0.5 m-0">
                  Switch to the Transcript or Captions tabs above to view, edit, or download files.
                </p>
              </div>
            </div>
          </SectionCard>
        )}
      </div>

      {/* Sidebar Quick Actions */}
      <div className="space-y-6">
        <SectionCard title="Quick Actions">
          <div className="flex flex-col gap-2.5">
            <Link
              to={`/projects/${project.id}/media`}
              className="flex items-center justify-center gap-2 w-full rounded-lg border border-border-custom bg-card py-2 text-sm font-semibold text-text-main hover:bg-slate-50 transition-colors"
            >
              <Upload className="h-4 w-4 text-text-muted" />
              Upload Media
            </Link>

            <button
              disabled={isCreated}
              onClick={() => navigate(`/projects/${project.id}/transcript`)}
              className="flex items-center justify-center gap-2 w-full rounded-lg border border-border-custom bg-card py-2 text-sm font-semibold text-text-main hover:bg-slate-50 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
            >
              <FileText className="h-4 w-4 text-text-muted" />
              Generate Transcript
            </button>

            <button
              disabled={isCreated}
              onClick={() => navigate(`/projects/${project.id}/captions`)}
              className="flex items-center justify-center gap-2 w-full rounded-lg border border-border-custom bg-card py-2 text-sm font-semibold text-text-main hover:bg-slate-50 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
            >
              <Subtitles className="h-4 w-4 text-text-muted" />
              Generate Captions
            </button>

            <button
              disabled={isCreated}
              onClick={() => navigate(`/projects/${project.id}/translations`)}
              className="flex items-center justify-center gap-2 w-full rounded-lg border border-border-custom bg-card py-2 text-sm font-semibold text-text-main hover:bg-slate-50 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
            >
              <Languages className="h-4 w-4 text-text-muted" />
              Translate Subtitles
            </button>

            <div className="border-t border-border-custom/50 my-1"></div>

            <button
              onClick={() => setDeleteConfirmOpen(true)}
              className="flex items-center justify-center gap-2 w-full rounded-lg bg-red-50 py-2 text-sm font-semibold text-danger hover:bg-red-100 transition-colors"
            >
              <Trash2 className="h-4 w-4" />
              Delete Project
            </button>
          </div>
        </SectionCard>
      </div>

      <ConfirmDialog
        isOpen={deleteConfirmOpen}
        title="Delete Project Workspace"
        message="Are you sure you want to delete this project? This will permanently remove all uploads, transcripts, captions, and translations. This action is irreversible."
        confirmLabel="Delete Project"
        isDanger={true}
        onConfirm={handleDeleteProject}
        onCancel={() => setDeleteConfirmOpen(false)}
      />
    </div>
  )
}

// Simple loader helper inside file
const Loader: React.FC<{ className?: string }> = ({ className }) => (
  <svg className={`animate-spin ${className}`} fill="none" viewBox="0 0 24 24">
    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
    <path
      className="opacity-75"
      fill="currentColor"
      d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
    />
  </svg>
)
