import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useDeleteProject } from '@/shared/services/api/projects'
import toast from 'react-hot-toast'

export const useDeleteProjectFlow = (projectId: string) => {
  const navigate = useNavigate()
  const deleteProjectMutation = useDeleteProject()
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false)

  const handleDeleteProject = async () => {
    toast.promise(deleteProjectMutation.mutateAsync(projectId), {
      loading: 'Deleting project...',
      success: () => {
        navigate('/projects')
        return 'Project deleted successfully'
      },
      error: 'Failed to delete project',
    })
    setDeleteConfirmOpen(false)
  }

  return {
    deleteConfirmOpen,
    setDeleteConfirmOpen,
    handleDeleteProject,
    isDeleting: deleteProjectMutation.isPending,
  }
}
