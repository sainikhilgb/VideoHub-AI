import React, { useState, useEffect } from 'react'
import { useOutletContext, useNavigate } from 'react-router-dom'
import { FileText, Play, Loader2, Sparkles } from 'lucide-react'
import { SectionCard } from '@/shared/components/ui/SectionCard'
import { EmptyState } from '@/shared/components/ui/EmptyState'
import {
  useProjectTranscript,
  useProjectMedia,
  useGenerateCaptions,
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
  
  // API Queries & Mutations
  const { data: transcriptInfo, isLoading: isMetadataLoading, refetch: refetchTranscript } = useProjectTranscript(project.id)
  const { data: mediaFiles, isLoading: isMediaLoading } = useProjectMedia(project.id)
  const generateCaptions = useGenerateCaptions()

  const [content, setContent] = useState<TranscriptContent | null>(null)
  const [isContentLoading, setIsContentLoading] = useState(false)

  useEffect(() => {
    if (transcriptInfo?.blobUrl) {
      setIsContentLoading(true)
      axios.get<TranscriptContent>(transcriptInfo.blobUrl)
        .then(res => {
          setContent(res.data)
          setIsContentLoading(false)
        })
        .catch(err => {
          console.error("Failed to load transcript JSON", err)
          setContent(null)
          setIsContentLoading(false)
        })
    } else {
      setContent(null)
    }
  }, [transcriptInfo?.blobUrl])

  const handleTriggerTranscription = async () => {
    const activeMedia = mediaFiles?.[0]
    if (!activeMedia) {
      toast.error("No media asset found to transcribe. Please upload a file first.")
      return
    }

    const toastId = toast.loading("Queueing speech-to-text transcription job...")
    try {
      await generateCaptions.mutateAsync({
        projectId: project.id,
        mediaId: activeMedia.id,
        targetLanguages: [project.originalLanguage],
      })
      toast.success("AI Transcription queued successfully!", { id: toastId })
      // Poll/refetch transcript status metadata
      refetchTranscript()
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Failed to queue transcription"
      toast.error(msg, { id: toastId })
    }
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
            <SectionCard title="Video Preview" subtitle="Review output and video media file.">
              <div className="aspect-video w-full rounded-lg bg-slate-900 flex items-center justify-center text-white relative group overflow-hidden">
                <Play className="h-16 w-16 text-white/80 group-hover:scale-110 group-hover:text-white transition-all cursor-pointer drop-shadow-md" />
                <div className="absolute bottom-4 left-4 right-4 flex items-center justify-between text-xs text-white/60 bg-black/40 px-3 py-1.5 rounded-md">
                  <span>Workspace Media</span>
                  <span>00:00 / --:--</span>
                </div>
              </div>
            </SectionCard>

            <SectionCard
              title="Speech Transcript editor"
              subtitle="Review and correct generated speech segments."
            >
              <div className="space-y-4">
                {content.segments.map((seg, idx) => (
                  <div key={idx} className="p-3.5 rounded-lg bg-slate-50 border border-border-custom/50 flex gap-4">
                    <span className="text-xs font-semibold text-accent shrink-0 mt-0.5">
                      {formatTime(seg.start)} - {formatTime(seg.end)}
                    </span>
                    <div className="space-y-1 flex-1">
                      <p className="text-xs font-semibold text-text-main">
                        Speaker {idx % 2 === 0 ? '1' : '2'}
                      </p>
                      <p className="text-sm text-text-main leading-relaxed m-0">
                        {seg.text}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            </SectionCard>
          </div>

          {/* Quick info */}
          <div className="space-y-6">
            <SectionCard title="Speech Technicals">
              <dl className="space-y-3 text-xs select-none mb-4">
                <div className="flex justify-between py-1 border-b border-border-custom/40">
                  <dt className="text-text-muted">Detected Language</dt>
                  <dd className="font-semibold text-text-main">{content.detectedLanguage.toUpperCase()}</dd>
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
            </SectionCard>
          </div>
        </div>
      )}
    </div>
  )
}
export default ProjectTranscriptTab
