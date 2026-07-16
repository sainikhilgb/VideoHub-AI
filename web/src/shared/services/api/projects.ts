import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import { apiClient } from './client'

// Interfaces matching .NET DTO models
export interface Project {
  id: string
  name: string
  originalLanguage: string
  userId: string
  status: string
  createdAt: string
  updatedAt: string
}

export interface ProjectRequest {
  name: string
  originalLanguage: string
}

export interface UploadMediaResponse {
  mediaId: string
  projectId: string
  uploadStatus: string
  storagePath: string
}

export interface GenerateCaptionsResponse {
  jobId: string
  hangfireJobId: string
}

export interface CaptionFileStatus {
  captionFileId: string
  format: string
  status: string
  blobUrl: string | null
  errorMessage: string | null
}

export interface LanguageStatus {
  languageCode: string
  captions: CaptionFileStatus[]
}

export interface JobStatusResponse {
  jobId: string
  status: string
  statusMessage: string | null
  startedAt: string | null
  completedAt: string | null
  languages: LanguageStatus[]
}

// Default dummy UserId since auth is deferred
export const DEFAULT_USER_ID = '00000000-0000-0000-0000-000000000000'

// 1. Fetch all projects
export const useProjects = () => {
  return useQuery<Project[]>({
    queryKey: ['projects'],
    queryFn: async () => {
      const response = await apiClient.get<Project[]>('/v1/projects')
      return response.data
    },
  })
}

// 2. Fetch project by ID
export const useProject = (id: string | undefined) => {
  return useQuery<Project>({
    queryKey: ['project', id],
    queryFn: async () => {
      if (!id) throw new Error('Project ID is required')
      const response = await apiClient.get<Project>(`/v1/projects/${id}`)
      return response.data
    },
    enabled: !!id,
  })
}

// 3. Create project
export const useCreateProject = () => {
  const queryClient = useQueryClient()
  return useMutation<Project, Error, ProjectRequest>({
    mutationFn: async (newProject) => {
      const response = await apiClient.post<Project>('/v1/projects', newProject)
      return response.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] })
    },
  })
}

// 4. Delete project
export const useDeleteProject = () => {
  const queryClient = useQueryClient()
  return useMutation<void, Error, string>({
    mutationFn: async (id) => {
      await apiClient.delete(`/v1/projects/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] })
    },
  })
}

// 5. Upload media file to project
export interface UploadMediaParams {
  projectId: string
  file: File
}

export const useUploadMedia = () => {
  return useMutation<UploadMediaResponse, Error, UploadMediaParams>({
    mutationFn: async ({ projectId, file }) => {
      const formData = new FormData()
      formData.append('File', file)

      const response = await apiClient.post<UploadMediaResponse>(
        `/v1/projects/${projectId}/media`,
        formData,
        {
          headers: {
            'Content-Type': 'multipart/form-data',
          },
        },
      )
      return response.data
    },
  })
}

// 6. Trigger caption generation
export interface GenerateCaptionsParams {
  projectId: string
  mediaId: string
  targetLanguages?: string[]
}

export const useGenerateCaptions = () => {
  return useMutation<GenerateCaptionsResponse, Error, GenerateCaptionsParams>({
    mutationFn: async ({ projectId, mediaId, targetLanguages = [] }) => {
      const response = await apiClient.post<GenerateCaptionsResponse>(
        `/v1/projects/${projectId}/media/${mediaId}/captions`,
        { targetLanguages },
      )
      return response.data
    },
  })
}

// 7. Poll job status
export const useJobStatus = (jobId: string | undefined, enabled = false) => {
  return useQuery<JobStatusResponse>({
    queryKey: ['jobStatus', jobId],
    queryFn: async () => {
      if (!jobId) throw new Error('Job ID is required')
      const response = await apiClient.get<JobStatusResponse>(`/v1/jobs/${jobId}`)
      return response.data
    },
    enabled: enabled && !!jobId,
    refetchInterval: (query) => {
      // Stop polling if job completes or fails
      const status = query.state.data?.status?.toLowerCase()
      if (status === 'completed' || status === 'failed') {
        return false
      }
      return 3000 // Poll every 3 seconds
    },
  })
}

export interface MediaFile {
  id: string
  projectId: string
  fileName: string
  contentType: string
  fileSize: number
  status: string
  createdAt: string
  url?: string | null
}

// 8. Fetch project media files (mocked fallback only if backend 404s)
export const useProjectMedia = (projectId: string | undefined) => {
  return useQuery<MediaFile[]>({
    queryKey: ['projectMedia', projectId],
    queryFn: async () => {
      if (!projectId) return []
      try {
        const response = await apiClient.get<MediaFile[]>(`/v1/projects/${projectId}/media`)
        return response.data
      } catch (err: unknown) {
        if (axios.isAxiosError(err) && err.response?.status === 404) {
          // Fallback mock since backend is infrastructure-first and lacks media list GET
          return [
            {
              id: 'media-1111',
              projectId: projectId || '',
              fileName: 'interview_raw.mp4',
              contentType: 'video/mp4',
              fileSize: 48200000,
              status: 'processed',
              createdAt: new Date().toISOString(),
            },
          ]
        }
        throw err
      }
    },
    enabled: !!projectId,
  })
}

// 9. Delete media file
export const useDeleteMedia = (projectId: string | undefined) => {
  const queryClient = useQueryClient()
  return useMutation<void, Error, string>({
    mutationFn: async (mediaId) => {
      try {
        await apiClient.delete(`/v1/media/${mediaId}`)
      } catch (err: unknown) {
        if (axios.isAxiosError(err) && err.response?.status === 404) {
          return
        }
        throw err
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projectMedia', projectId] })
    },
  })
}

// 10. Update project settings
export interface UpdateProjectParams {
  projectId: string
  name: string
  originalLanguage: string
}

export const useUpdateProject = () => {
  const queryClient = useQueryClient()
  return useMutation<Project, Error, UpdateProjectParams>({
    mutationFn: async ({ projectId, name, originalLanguage }) => {
      const response = await apiClient.put<Project>(`/v1/projects/${projectId}`, {
        name,
        originalLanguage,
      })
      return response.data
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['project', variables.projectId] })
      queryClient.invalidateQueries({ queryKey: ['projects'] })
    },
  })
}

// 11. Retrieve project caption files list
export interface ProjectCaptionResponse {
  id: string
  jobId: string | null
  format: string
  language: string
  status: string
  blobUrl: string | null
}

export const useProjectCaptions = (projectId: string | undefined) => {
  return useQuery<ProjectCaptionResponse[]>({
    queryKey: ['projectCaptions', projectId],
    queryFn: async () => {
      if (!projectId) return []
      const response = await apiClient.get<ProjectCaptionResponse[]>(`/v1/caption-files/project/${projectId}`)
      return response.data
    },
    enabled: !!projectId,
  })
}

// 12. Retrieve project transcript metadata
export interface ProjectTranscriptResponse {
  id: string
  language: string
  status: string
  blobUrl: string | null
  version: number
}

export const useProjectTranscript = (projectId: string | undefined, language?: string, version?: number) => {
  return useQuery<ProjectTranscriptResponse | null>({
    queryKey: ['projectTranscript', projectId, language, version],
    queryFn: async () => {
      if (!projectId) return null
      try {
        const params = new URLSearchParams()
        if (language) params.append('language', language)
        if (version) params.append('version', version.toString())
        const response = await apiClient.get<ProjectTranscriptResponse>(
          `/v1/projects/${projectId}/transcript?${params.toString()}`
        )
        return response.data
      } catch (err: unknown) {
        if (axios.isAxiosError(err) && err.response?.status === 404) {
          return null
        }
        throw err
      }
    },
    enabled: !!projectId,
  })
}
