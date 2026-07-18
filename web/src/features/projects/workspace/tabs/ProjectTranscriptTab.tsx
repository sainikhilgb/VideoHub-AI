import React, { useState, useEffect, useRef } from 'react'
import { useOutletContext, useNavigate } from 'react-router-dom'
import { FileText, Loader2, Sparkles, Edit3, Save, X } from 'lucide-react'
import { SectionCard } from '@/shared/components/ui/SectionCard'
import { EmptyState } from '@/shared/components/ui/EmptyState'
import { MediaPlayer } from '@/shared/components/ui/MediaPlayer'
import {
  useProjectTranscript,
  useProjectMedia,
  useGenerateCaptions,
  useJobStatus,
  useUpdateTranscript,
  useGenerateCaptionsFromTranscript,
  type Project,
} from '@/shared/services/api/projects'
import axios from 'axios'
import toast from 'react-hot-toast'

interface TranscriptWord {
  text: string
  start: number
  end: number
  confidence: number | null
}

interface TranscriptSegment {
  start: number
  end: number
  text: string
  confidence: number | null
  words?: TranscriptWord[]
}

interface TranscriptContent {
  detectedLanguage: string
  segments: TranscriptSegment[]
}

export const ProjectTranscriptTab: React.FC = () => {
  const project = useOutletContext<Project>()
  const navigate = useNavigate()

  const [selectedMediaId, setSelectedMediaId] = useState<string>('')
  const [activeJobId, setActiveJobId] = useState<string | null>(null)
  const [content, setContent] = useState<TranscriptContent | null>(null)
  const [isContentLoading, setIsContentLoading] = useState(false)
  const [currentTime, setCurrentTime] = useState(0)
  const playerRef = useRef<HTMLVideoElement | HTMLAudioElement>(null)

  const [isEditing, setIsEditing] = useState(false)
  const [editedContent, setEditedContent] = useState<TranscriptContent | null>(null)

  const updateTranscript = useUpdateTranscript()
  const generateCaptionsFromTranscript = useGenerateCaptionsFromTranscript()

  // API Queries & Mutations
  const {
    data: transcriptInfo,
    isLoading: isMetadataLoading,
    refetch: refetchTranscript,
  } = useProjectTranscript(project.id, project.originalLanguage, 1)
  const { data: mediaFiles, isLoading: isMediaLoading } = useProjectMedia(project.id)
  const { data: jobStatus } = useJobStatus(activeJobId ?? undefined, !!activeJobId)
  const generateCaptions = useGenerateCaptions()

  const activeMedia = mediaFiles?.find((m) => m.id === selectedMediaId)

  const activeSegmentIndex =
    content?.segments.findIndex((seg) => currentTime >= seg.start && currentTime <= seg.end) ?? -1

  useEffect(() => {
    if (activeSegmentIndex !== -1) {
      const activeEl = document.getElementById(`segment-${activeSegmentIndex}`)
      if (activeEl) {
        activeEl.scrollIntoView({ behavior: 'smooth', block: 'nearest' })
      }
    }
  }, [activeSegmentIndex])

  // Only run this effect when mediaFiles changes (avoid including selectedMediaId in deps)
  useEffect(() => {
    if (mediaFiles && mediaFiles.length > 0 && !selectedMediaId) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setSelectedMediaId(mediaFiles[0].id)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [mediaFiles])

  // Respond to jobStatus changes. Include activeJobId in deps and guard to avoid cascading renders.
  useEffect(() => {
    if (!jobStatus) return
    const status = jobStatus.status.toLowerCase()
    if ((status === 'completed' || status === 'failed') && activeJobId !== null) {
      refetchTranscript()
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setActiveJobId(null)
      if (status === 'failed') {
        toast.error('Speech transcription job failed.')
      } else {
        toast.success('Speech transcription completed successfully!')
      }
    }
  }, [jobStatus, refetchTranscript, activeJobId])

  useEffect(() => {
    if (transcriptInfo?.blobUrl) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setIsContentLoading(true)
      const controller = new AbortController()

      axios
        .get<TranscriptContent>(transcriptInfo.blobUrl, { signal: controller.signal })
        .then((res) => {
          setContent(res.data)
        })
        .catch((err) => {
          if (!axios.isCancel(err)) {
            console.error('Failed to load transcript JSON', err)
            setContent(null)
          }
        })
        .finally(() => {
          if (!controller.signal.aborted) {
            setIsContentLoading(false)
          }
        })

      return () => {
        controller.abort()
      }
    } else {
      setContent(null)
      setIsContentLoading(false)
    }
  }, [transcriptInfo?.blobUrl])

  const handleTriggerTranscription = async () => {
    if (!selectedMediaId) {
      toast.error('Please select a media asset to transcribe.')
      return
    }

    if (!activeMedia) {
      toast.error('Selected media asset not found.')
      return
    }

    const toastId = toast.loading('Queueing speech-to-text transcription job...')
    try {
      const result = await generateCaptions.mutateAsync({
        projectId: project.id,
        mediaId: activeMedia.id,
        targetLanguages: [project.originalLanguage],
      })
      toast.success('AI Transcription queued successfully!', { id: toastId })
      if (result?.jobId) {
        setActiveJobId(result.jobId)
      } else {
        refetchTranscript()
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to queue transcription'
      toast.error(msg, { id: toastId })
    }
  }

  const handleSaveTranscript = async () => {
    if (!editedContent || !transcriptInfo) return
    const toastId = toast.loading('Saving transcript draft...')
    try {
      await updateTranscript.mutateAsync({
        projectId: project.id,
        transcriptId: transcriptInfo.id,
        content: editedContent,
      })
      toast.success('Transcript draft saved successfully!', { id: toastId })
      setContent(editedContent)
      setIsEditing(false)
      refetchTranscript()
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to save transcript'
      toast.error(msg, { id: toastId })
    }
  }

  const handleGenerateCaptionsFromTranscript = async () => {
    if (!content || !transcriptInfo) return
    const toastId = toast.loading('Generating caption files...')
    try {
      await generateCaptionsFromTranscript.mutateAsync({
        projectId: project.id,
        transcriptId: transcriptInfo.id,
        content: content,
      })
      toast.success('Caption files (.srt / .vtt) successfully regenerated!', { id: toastId })
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to generate captions'
      toast.error(msg, { id: toastId })
    }
  }

  const handleSegmentTextChange = (idx: number, newText: string) => {
    if (!editedContent) return
    const newSegments = [...editedContent.segments]
    newSegments[idx] = { ...newSegments[idx], text: newText }
    setEditedContent({ ...editedContent, segments: newSegments })
  }

  const formatTime = (seconds: number) => {
    const mins = Math.floor(seconds / 60)
    const secs = Math.floor(seconds % 60)
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`
  }

  const isLoading = isMetadataLoading || isContentLoading || isMediaLoading
  const hasTranscript = content && content.segments.length > 0
  const hasMedia = mediaFiles && mediaFiles.length > 0

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-12">
        <Loader2 className="h-8 w-8 text-accent animate-spin mb-2" />
        <p className="text-xs text-text-muted">Loading speech transcript details...</p>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {hasMedia && (
        <div className="max-w-xs">
          <label className="block text-[10px] font-bold text-text-muted mb-1.5 uppercase tracking-wider">
            Active Media File
          </label>
          <select
            value={selectedMediaId}
            onChange={(e) => setSelectedMediaId(e.target.value)}
            className="w-full rounded-lg border border-border-custom bg-card px-3 py-1.5 text-xs font-semibold text-text-main focus:border-accent focus:outline-none"
          >
            <option value="">-- Choose a media file --</option>
            {mediaFiles?.map((m) => (
              <option key={m.id} value={m.id}>
                {m.fileName}
              </option>
            ))}
          </select>
        </div>
      )}

      {!hasTranscript ? (
        <EmptyState
          title="Transcript not generated yet"
          description="We are still transcribing speech for this workspace. Check back shortly."
          icon={<FileText className="h-6 w-6 text-accent" />}
          action={
            hasMedia ? (
              <button
                onClick={handleTriggerTranscription}
                disabled={generateCaptions.isPending}
                className="inline-flex items-center gap-1.5 rounded-lg bg-accent px-4 py-2 text-xs font-semibold text-white shadow-custom-sm hover:bg-accent-hover transition-colors disabled:opacity-50"
              >
                {generateCaptions.isPending ? (
                  <>
                    <Loader2 className="h-3.5 w-3.5 animate-spin" />
                    Queueing...
                  </>
                ) : (
                  <>
                    <Sparkles className="h-3.5 w-3.5" />
                    Generate Speech Transcript
                  </>
                )}
              </button>
            ) : (
              <button
                onClick={() => navigate(`/projects/${project.id}/media`)}
                className="inline-flex items-center gap-1.5 rounded-lg bg-accent px-4 py-2 text-xs font-semibold text-white shadow-custom-sm hover:bg-accent-hover transition-colors"
              >
                Go to Media Tab
              </button>
            )
          }
        />
      ) : (
        <div className="grid gap-6 md:grid-cols-3">
          {/* Main timeline */}
          <div className="md:col-span-2 space-y-6">
            <SectionCard
              title="Media Playback Preview"
              subtitle="Review output and synchronize video/audio media."
            >
              <MediaPlayer
                src={activeMedia?.url}
                contentType={activeMedia?.contentType}
                onTimeUpdate={setCurrentTime}
                playerRef={playerRef}
              />
            </SectionCard>

            <SectionCard
              title="Speech Transcript editor"
              subtitle="Review and correct generated speech segments."
              actions={
                !isEditing ? (
                  <button
                    onClick={() => {
                      setEditedContent(content)
                      setIsEditing(true)
                    }}
                    className="inline-flex items-center gap-1.5 rounded-lg border border-border-custom bg-card px-3 py-1.5 text-xs font-semibold text-text-main shadow-custom-sm hover:bg-slate-50 transition-colors"
                  >
                    <Edit3 className="h-3.5 w-3.5 text-accent" />
                    Edit Transcript
                  </button>
                ) : (
                  <div className="flex gap-2">
                    <button
                      onClick={() => setIsEditing(false)}
                      disabled={updateTranscript.isPending}
                      className="inline-flex items-center gap-1.5 rounded-lg border border-border-custom bg-card px-3 py-1.5 text-xs font-semibold text-text-main shadow-custom-sm hover:bg-slate-50 transition-colors disabled:opacity-50"
                    >
                      <X className="h-3.5 w-3.5" />
                      Cancel
                    </button>
                    <button
                      onClick={handleSaveTranscript}
                      disabled={updateTranscript.isPending}
                      className="inline-flex items-center gap-1.5 rounded-lg bg-accent px-3 py-1.5 text-xs font-semibold text-white shadow-custom-sm hover:bg-accent-hover transition-colors disabled:opacity-50"
                    >
                      {updateTranscript.isPending ? (
                        <Loader2 className="h-3.5 w-3.5 animate-spin" />
                      ) : (
                        <Save className="h-3.5 w-3.5" />
                      )}
                      Save Changes
                    </button>
                  </div>
                )
              }
            >
              <div className="space-y-4 max-h-[500px] overflow-y-auto pr-2 custom-scrollbar">
                {(isEditing ? editedContent?.segments : content.segments)?.map((seg, idx) => {
                  const isActive = !isEditing && idx === activeSegmentIndex
                  return (
                    <div
                      key={idx}
                      id={`segment-${idx}`}
                      onClick={() => {
                        if (!isEditing && playerRef.current) {
                          playerRef.current.currentTime = seg.start
                          playerRef.current.play().catch(() => {})
                        }
                      }}
                      className={`p-3.5 rounded-lg border transition-all flex gap-4 ${
                        !isEditing ? 'cursor-pointer select-none hover:bg-slate-100/50' : ''
                      } w-full text-left ${
                        isActive
                          ? 'bg-accent/5 border-accent shadow-sm ring-1 ring-accent/20'
                          : 'bg-slate-50 border-border-custom/50'
                      }`}
                    >
                      <span className="text-xs font-semibold text-accent shrink-0 mt-0.5">
                        {formatTime(seg.start)} - {formatTime(seg.end)}
                      </span>
                      <div className="space-y-1 flex-1">
                        {isEditing ? (
                          <textarea
                            value={seg.text}
                            onChange={(e) => handleSegmentTextChange(idx, e.target.value)}
                            className="w-full text-sm text-text-main leading-relaxed m-0 bg-white border border-border-custom rounded-md p-2 focus:outline-none focus:ring-1 focus:ring-accent resize-y min-h-[60px]"
                          />
                        ) : (
                          <p className="text-sm text-text-main leading-relaxed m-0">{seg.text}</p>
                        )}
                      </div>
                    </div>
                  )
                })}
              </div>
            </SectionCard>
          </div>

          {/* Quick info */}
          <div className="space-y-6">
            <SectionCard title="Speech Technicals">
              <dl className="space-y-3 text-xs select-none mb-4">
                <div className="flex justify-between py-1 border-b border-border-custom/40">
                  <dt className="text-text-muted">Detected Language</dt>
                  <dd className="font-semibold text-text-main">
                    {content.detectedLanguage.toUpperCase()}
                  </dd>
                </div>
                <div className="flex justify-between py-1 border-b border-border-custom/40">
                  <dt className="text-text-muted">Total Segments</dt>
                  <dd className="font-semibold text-text-main">{content.segments.length} rows</dd>
                </div>
                <div className="flex justify-between py-1 border-b border-border-custom/40">
                  <dt className="text-text-muted">Diarization Context</dt>
                  <dd className="font-semibold text-success">Optimized Alignment</dd>
                </div>
              </dl>

              <div className="border-t border-border-custom/40 my-4"></div>

              <button
                onClick={handleTriggerTranscription}
                disabled={generateCaptions.isPending}
                className="flex items-center justify-center gap-2 w-full rounded-lg border border-border-custom bg-card py-2 text-xs font-semibold text-text-main hover:bg-slate-50 transition-colors disabled:opacity-50 cursor-pointer"
              >
                {generateCaptions.isPending ? (
                  <>
                    <Loader2 className="h-3.5 w-3.5 animate-spin" />
                    Re-queueing...
                  </>
                ) : (
                  <>
                    <Sparkles className="h-3.5 w-3.5 text-accent" />
                    Re-trigger AI Transcription
                  </>
                )}
              </button>

              <button
                onClick={handleGenerateCaptionsFromTranscript}
                disabled={generateCaptionsFromTranscript.isPending || isEditing}
                className="flex items-center justify-center gap-2 w-full rounded-lg border border-transparent bg-accent py-2 text-xs font-semibold text-white hover:bg-accent-hover transition-colors disabled:opacity-50 cursor-pointer mt-3"
              >
                {generateCaptionsFromTranscript.isPending ? (
                  <>
                    <Loader2 className="h-3.5 w-3.5 animate-spin text-white" />
                    Generating...
                  </>
                ) : (
                  <>
                    <FileText className="h-3.5 w-3.5 text-white" />
                    Generate SRT / VTT
                  </>
                )}
              </button>
            </SectionCard>
          </div>
        </div>
      )}
    </div>
  )
}
export default ProjectTranscriptTab
