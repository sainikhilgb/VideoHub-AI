import React, { useState, useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, FolderClosed, Trash2, ExternalLink, X } from 'lucide-react'
import { PageHeader } from '@/shared/components/ui/PageHeader'
import { EmptyState } from '@/shared/components/ui/EmptyState'
import { ErrorState } from '@/shared/components/ui/ErrorState'
import { LoadingSpinner } from '@/shared/components/ui/LoadingSpinner'
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog'
import { StatusBadge } from '@/shared/components/ui/StatusBadge'
import { useProjects, useDeleteProject, useCreateProject } from '@/shared/services/api/projects'
import toast from 'react-hot-toast'

export const ProjectsPage: React.FC = () => {
  const navigate = useNavigate()

  // API Queries & Mutations
  const { data: projects, isLoading, isError, refetch } = useProjects()
  const deleteProjectMutation = useDeleteProject()
  const createProjectMutation = useCreateProject()

  // Local States
  const [projectToDelete, setProjectToDelete] = useState<string | null>(null)
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [name, setName] = useState('')
  const [language, setLanguage] = useState('en')
  const modalRef = useRef<HTMLDivElement>(null)

  // Focus trapping and Escape key listener
  useEffect(() => {
    if (!isModalOpen) return

    const previousActiveElement = document.activeElement as HTMLElement
    const modalElement = modalRef.current
    if (modalElement) {
      const inputElement = modalElement.querySelector('#projectName') as HTMLInputElement
      if (inputElement) {
        inputElement.focus()
      }
    }

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setIsModalOpen(false)
      }

      if (e.key === 'Tab' && modalElement) {
        const focusableSelectors =
          'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
        const focusableElements = Array.from(
          modalElement.querySelectorAll<HTMLElement>(focusableSelectors),
        )
        if (focusableElements.length === 0) return

        const firstElement = focusableElements[0]
        const lastElement = focusableElements[focusableElements.length - 1]

        if (e.shiftKey) {
          if (document.activeElement === firstElement) {
            lastElement.focus()
            e.preventDefault()
          }
        } else {
          if (document.activeElement === lastElement) {
            firstElement.focus()
            e.preventDefault()
          }
        }
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => {
      window.removeEventListener('keydown', handleKeyDown)
      if (previousActiveElement) {
        previousActiveElement.focus()
      }
    }
  }, [isModalOpen])

  const handleDeleteConfirm = async () => {
    if (!projectToDelete) return

    toast.promise(deleteProjectMutation.mutateAsync(projectToDelete), {
      loading: 'Deleting project...',
      success: 'Project deleted successfully',
      error: 'Failed to delete project',
    })

    setProjectToDelete(null)
  }

  const handleCreateSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!name.trim()) {
      toast.error('Project Name is required')
      return
    }

    if (name.length > 200) {
      toast.error('Project Name must be under 200 characters')
      return
    }

    try {
      const newProject = await createProjectMutation.mutateAsync({
        name: name.trim(),
        originalLanguage: language,
      })

      toast.success('Project initialized successfully!')
      setIsModalOpen(false)
      setName('')

      // Auto redirect to Project details / workspace Overview
      navigate(`/projects/${newProject.id}`)
    } catch {
      toast.error('Failed to initialize project')
    }
  }

  if (isLoading) {
    return <LoadingSpinner size="lg" className="min-h-[50vh]" />
  }

  if (isError) {
    return (
      <div className="max-w-md mx-auto mt-12">
        <ErrorState onRetry={() => refetch()} />
      </div>
    )
  }

  const hasProjects = projects && projects.length > 0

  return (
    <div className="space-y-6">
      <PageHeader
        title="Projects"
        description="Manage your video transcripts, captions, and translations."
        actions={
          <button
            onClick={() => setIsModalOpen(true)}
            className="inline-flex items-center gap-1.5 rounded-lg bg-accent px-4 py-2 text-sm font-semibold text-white shadow-custom-sm hover:bg-accent-hover transition-colors"
          >
            <Plus className="h-4 w-4" />
            New Project
          </button>
        }
      />

      {!hasProjects ? (
        <EmptyState
          title="No projects yet"
          description="Create your first project to transcribe, generate captions, or translate subtitles."
          icon={<FolderClosed className="h-6 w-6 text-accent" />}
          action={
            <button
              onClick={() => setIsModalOpen(true)}
              className="inline-flex items-center gap-1.5 rounded-lg bg-accent px-4 py-2 text-xs font-semibold text-white shadow-custom-sm hover:bg-accent-hover transition-colors"
            >
              <Plus className="h-4 w-4" />
              Create Project
            </button>
          }
        />
      ) : (
        <div className="rounded-xl border border-border-custom bg-card shadow-custom-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm border-collapse">
              <thead>
                <tr className="border-b border-border-custom bg-slate-50/50 text-text-muted font-medium select-none">
                  <th className="px-6 py-3.5">Project Name</th>
                  <th className="px-6 py-3.5">Original Language</th>
                  <th className="px-6 py-3.5">Status</th>
                  <th className="px-6 py-3.5">Created</th>
                  <th className="px-6 py-3.5 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border-custom/50">
                {projects.map((project) => (
                  <tr key={project.id} className="hover:bg-slate-50/30 transition-colors">
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
                    <td className="px-6 py-4 text-right flex items-center justify-end gap-3.5">
                      <button
                        onClick={() => navigate(`/projects/${project.id}`)}
                        className="inline-flex items-center gap-1 text-xs font-semibold text-accent hover:text-accent-hover transition-colors"
                      >
                        Details
                        <ExternalLink className="h-3 w-3" />
                      </button>
                      <button
                        onClick={() => setProjectToDelete(project.id)}
                        className="text-text-muted hover:text-danger p-1 rounded-md transition-colors"
                        title="Delete project"
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Delete Confirmation Modal */}
      <ConfirmDialog
        isOpen={!!projectToDelete}
        title="Delete Project"
        message="Are you sure you want to delete this project? This will permanently delete the project metadata, transcripts, and related media files. This action cannot be undone."
        confirmLabel="Delete"
        isDanger={true}
        onConfirm={handleDeleteConfirm}
        onCancel={() => setProjectToDelete(null)}
      />

      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs animate-fade-in">
          <div
            ref={modalRef}
            role="dialog"
            aria-modal="true"
            aria-labelledby="modal-title"
            className="relative w-full max-w-md rounded-xl border border-border-custom bg-card shadow-custom-lg p-6 animate-scale-up"
          >
            <button
              onClick={() => setIsModalOpen(false)}
              className="absolute top-4 right-4 text-text-muted hover:text-text-main p-1 rounded-md transition-colors focus:outline-none"
            >
              <X className="h-4 w-4" />
            </button>

            <h3 id="modal-title" className="text-base font-semibold text-text-main mb-1">
              Create New Project
            </h3>
            <p className="text-xs text-text-muted mb-5">
              Provide a name and source spoken language.
            </p>

            <form onSubmit={handleCreateSubmit} className="space-y-4">
              <div>
                <label
                  htmlFor="projectName"
                  className="block text-xs font-semibold text-text-muted mb-1.5 uppercase"
                >
                  Project Name
                </label>
                <input
                  type="text"
                  id="projectName"
                  required
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="My Awesome Video"
                  className="w-full rounded-lg border border-border-custom bg-card px-3 py-2 text-sm focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent"
                />
              </div>

              <div>
                <label
                  htmlFor="language"
                  className="block text-xs font-semibold text-text-muted mb-1.5 uppercase"
                >
                  Spoken Language
                </label>
                <select
                  id="language"
                  value={language}
                  onChange={(e) => setLanguage(e.target.value)}
                  className="w-full rounded-lg border border-border-custom bg-card px-3 py-2 text-sm focus:border-accent focus:outline-none"
                >
                  <option value="en">English (EN)</option>
                  <option value="es">Spanish (ES)</option>
                  <option value="fr">French (FR)</option>
                  <option value="de">German (DE)</option>
                </select>
              </div>

              <div className="flex justify-end gap-3 pt-4 border-t border-border-custom/50">
                <button
                  type="button"
                  onClick={() => setIsModalOpen(false)}
                  className="rounded-lg border border-border-custom bg-card px-4 py-2 text-xs font-semibold text-text-muted hover:bg-slate-50 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={createProjectMutation.isPending}
                  className="rounded-lg bg-accent px-4 py-2 text-xs font-semibold text-white shadow-custom-sm hover:bg-accent-hover transition-colors disabled:opacity-50"
                >
                  {createProjectMutation.isPending ? 'Initializing...' : 'Create'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
export default ProjectsPage
