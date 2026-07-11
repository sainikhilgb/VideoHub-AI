import React, { useState, useRef } from 'react'
import { useOutletContext } from 'react-router-dom'
import { Upload, Trash2, Video, Loader2 } from 'lucide-react'
import { SectionCard } from '@/shared/components/ui/SectionCard'
import { StatusBadge } from '@/shared/components/ui/StatusBadge'
import { EmptyState } from '@/shared/components/ui/EmptyState'
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog'
import {
  useProjectMedia,
  useUploadMedia,
  useGenerateCaptions,
  useDeleteMedia,
  type Project,
} from '@/shared/services/api/projects'
import toast from 'react-hot-toast'

export const ProjectMediaTab: React.FC = () => {
  const project = useOutletContext<Project>()
  const fileInputRef = useRef<HTMLInputElement>(null)

  // API Hooks
  const { data: mediaFiles, refetch } = useProjectMedia(project.id)
  const uploadMediaMutation = useUploadMedia()
  const generateCaptionsMutation = useGenerateCaptions()
  const deleteMediaMutation = useDeleteMedia(project.id)

  // Component states
  const [file, setFile] = useState<File | null>(null)
  const [isProcessing, setIsProcessing] = useState(false)
  const [uploadProgress, setUploadProgress] = useState(0)
  const [statusMessage, setStatusMessage] = useState('')
  const [mediaToDelete, setMediaToDelete] = useState<string | null>(null)

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      setFile(e.target.files[0])
    }
  }

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault()
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      setFile(e.dataTransfer.files[0])
    }
  }

  const handleUploadClick = () => {
    fileInputRef.current?.click()
  }

  const handleStartProcessing = async () => {
    if (!file || !project.id) return

    try {
      setIsProcessing(true)
      setStatusMessage('Uploading media file to server...')
      setUploadProgress(30)

      // Step 1: Upload media file
      const uploadRes = await uploadMediaMutation.mutateAsync({
        projectId: project.id,
        file,
      })

      // Step 2: Trigger ASR queue
      setStatusMessage('Queueing AI transcription...')
      setUploadProgress(75)
      await generateCaptionsMutation.mutateAsync({
        projectId: project.id,
        mediaId: uploadRes.mediaId,
        targetLanguages: [project.originalLanguage],
      })

      setUploadProgress(100)
      toast.success('Media file uploaded successfully! Processing started.')
      refetch()
      setFile(null)
      setIsProcessing(false)
    } catch (err: any) {
      toast.error(err?.message || 'Failed to process media file')
      setIsProcessing(false)
    }
  }

  const handleDeleteConfirm = async () => {
    if (!mediaToDelete) return
    toast.promise(deleteMediaMutation.mutateAsync(mediaToDelete), {
      loading: 'Deleting file...',
      success: 'Media file removed',
      error: 'Failed to delete file',
    })
    setMediaToDelete(null)
  }

  const hasMedia = mediaFiles && mediaFiles.length > 0

  return (
    <div className="space-y-6">
      {isProcessing ? (
        <SectionCard className="text-center py-12">
          <div className="flex flex-col items-center justify-center">
            <Loader2 className="h-8 w-8 text-accent animate-spin mb-4" />
            <h3 className="text-base font-semibold text-text-main mb-1">Processing Upload</h3>
            <p className="text-xs text-text-muted mb-6">{statusMessage}</p>
            <div className="w-full max-w-sm bg-slate-100 rounded-full h-1.5 overflow-hidden mb-2">
              <div
                className="bg-accent h-full transition-all duration-300"
                style={{ width: `${uploadProgress}%` }}
              />
            </div>
            <span className="text-[10px] font-semibold text-text-muted">
              {uploadProgress}% Complete
            </span>
          </div>
        </SectionCard>
      ) : (
        <div className="grid gap-6 md:grid-cols-3">
          {/* Upload panel */}
          <div className="md:col-span-1">
            <SectionCard title="Upload Media" subtitle="Support video/audio files.">
              <div className="space-y-4">
                <input
                  type="file"
                  ref={fileInputRef}
                  onChange={handleFileChange}
                  accept="video/*,audio/*"
                  className="hidden"
                />
                <div
                  onDragOver={handleDragOver}
                  onDrop={handleDrop}
                  onClick={handleUploadClick}
                  className="flex flex-col items-center justify-center border-2 border-dashed border-border-custom rounded-xl p-8 bg-slate-50/50 hover:bg-slate-50 transition-colors cursor-pointer select-none text-center"
                >
                  <Upload className="h-5 w-5 text-accent mb-3" />
                  {file ? (
                    <div>
                      <p className="text-xs font-semibold text-text-main truncate max-w-xs">
                        {file.name}
                      </p>
                      <p className="text-[10px] text-text-muted">
                        {(file.size / (1024 * 1024)).toFixed(2)} MB
                      </p>
                    </div>
                  ) : (
                    <>
                      <p className="text-xs font-semibold text-text-main mb-0.5">
                        Drag and drop file here
                      </p>
                      <p className="text-[10px] text-text-muted">MP4, MOV, MP3, WAV up to 500MB</p>
                    </>
                  )}
                </div>

                {file && (
                  <button
                    onClick={handleStartProcessing}
                    className="w-full rounded-lg bg-accent py-2 text-sm font-semibold text-white hover:bg-accent-hover transition-colors"
                  >
                    Upload & Process
                  </button>
                )}
              </div>
            </SectionCard>
          </div>

          {/* Files List panel */}
          <div className="md:col-span-2">
            <SectionCard
              title="Assets catalog"
              subtitle="List of project media files."
              padding={false}
            >
              {!hasMedia ? (
                <div className="p-8">
                  <EmptyState
                    title="No media assets yet"
                    description="Upload an audio or video file to start transcriptions."
                    icon={<Video className="h-5 w-5 text-accent" />}
                  />
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-left text-sm border-collapse">
                    <thead>
                      <tr className="border-b border-border-custom bg-slate-50/50 text-text-muted font-medium select-none">
                        <th className="px-6 py-3">File Name</th>
                        <th className="px-6 py-3">Size</th>
                        <th className="px-6 py-3">Status</th>
                        <th className="px-6 py-3 text-right">Action</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border-custom/50">
                      {mediaFiles.map((file) => (
                        <tr key={file.id} className="hover:bg-slate-50/30 transition-colors">
                          <td className="px-6 py-4 font-medium text-text-main flex items-center gap-2">
                            <Video className="h-4 w-4 text-accent shrink-0" />
                            <span className="truncate max-w-xs">{file.fileName}</span>
                          </td>
                          <td className="px-6 py-4 text-text-muted">
                            {(file.fileSize / (1024 * 1024)).toFixed(1)} MB
                          </td>
                          <td className="px-6 py-4">
                            <StatusBadge status={file.status} />
                          </td>
                          <td className="px-6 py-4 text-right">
                            <button
                              onClick={() => setMediaToDelete(file.id)}
                              className="text-text-muted hover:text-danger p-1 rounded-md transition-colors"
                              title="Delete asset"
                            >
                              <Trash2 className="h-4 w-4" />
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
        </div>
      )}

      <ConfirmDialog
        isOpen={!!mediaToDelete}
        title="Delete Media Asset"
        message="Are you sure you want to remove this media file? All aligned transcript segments will be lost."
        confirmLabel="Delete Asset"
        isDanger={true}
        onConfirm={handleDeleteConfirm}
        onCancel={() => setMediaToDelete(null)}
      />
    </div>
  )
}
export default ProjectMediaTab
