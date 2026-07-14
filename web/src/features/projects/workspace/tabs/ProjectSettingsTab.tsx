import React, { useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import { Save, Trash2 } from 'lucide-react'
import { SectionCard } from '@/shared/components/ui/SectionCard'
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog'
import { useUpdateProject, type Project } from '@/shared/services/api/projects'
import { useDeleteProjectFlow } from '../hooks/useDeleteProjectFlow'
import toast from 'react-hot-toast'

export const ProjectSettingsTab: React.FC = () => {
  const project = useOutletContext<Project>()
  const updateProjectMutation = useUpdateProject()

  const [name, setName] = useState(project.name)
  const [language, setLanguage] = useState(project.originalLanguage)

  const { deleteConfirmOpen, setDeleteConfirmOpen, handleDeleteProject } = useDeleteProjectFlow(
    project.id,
  )

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!name) return

    toast.promise(
      updateProjectMutation.mutateAsync({
        projectId: project.id,
        name,
        originalLanguage: language,
      }),
      {
        loading: 'Updating project settings...',
        success: 'Project settings updated!',
        error: 'Failed to update project settings',
      },
    )
  }

  return (
    <div className="max-w-xl space-y-6">
      <SectionCard title="Project Details" subtitle="Rename workspace context parameters.">
        <form onSubmit={handleUpdate} className="space-y-4">
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
              className="w-full rounded-lg border border-border-custom bg-card px-3 py-2 text-sm focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent"
            />
          </div>

          <div>
            <label
              htmlFor="language"
              className="block text-xs font-semibold text-text-muted mb-1.5 uppercase"
            >
              Original Spoken Language
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

          <div className="flex justify-end pt-3 border-t border-border-custom/50">
            <button
              type="submit"
              disabled={updateProjectMutation.isPending}
              className="inline-flex items-center gap-1.5 rounded-lg bg-accent px-4 py-2 text-xs font-semibold text-white shadow-custom-sm hover:bg-accent-hover transition-colors"
            >
              <Save className="h-4 w-4" />
              Save Changes
            </button>
          </div>
        </form>
      </SectionCard>

      <SectionCard title="Danger Zone" subtitle="Irreversible administrative operations.">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 p-4 rounded-xl border border-red-100 bg-red-50/10">
          <div>
            <h4 className="text-sm font-semibold text-text-main m-0">Delete Project</h4>
            <p className="text-xs text-text-muted mt-1 m-0">
              Permanently delete this project. All associated media assets will be wiped out.
            </p>
          </div>

          <button
            onClick={() => setDeleteConfirmOpen(true)}
            className="inline-flex items-center justify-center gap-1.5 rounded-lg bg-danger px-4 py-2 text-xs font-semibold text-white hover:bg-red-700 transition-colors shrink-0"
          >
            <Trash2 className="h-4 w-4" />
            Delete Project
          </button>
        </div>
      </SectionCard>

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
export default ProjectSettingsTab
