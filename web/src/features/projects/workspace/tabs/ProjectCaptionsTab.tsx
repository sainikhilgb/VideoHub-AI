import React from 'react'
import { useOutletContext } from 'react-router-dom'
import { Subtitles, Download, Loader2 } from 'lucide-react'
import { SectionCard } from '@/shared/components/ui/SectionCard'
import { EmptyState } from '@/shared/components/ui/EmptyState'
import { useProjectCaptions, useProjectMedia, type Project } from '@/shared/services/api/projects'
import { MediaPlayer } from '@/shared/components/ui/MediaPlayer'

export const ProjectCaptionsTab: React.FC = () => {
  const project = useOutletContext<Project>()
  
  const { data: captions, isLoading } = useProjectCaptions(project.id)
  const { data: mediaFiles } = useProjectMedia(project.id)

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-12">
        <Loader2 className="h-8 w-8 text-accent animate-spin mb-2" />
        <p className="text-xs text-text-muted">Loading project captions...</p>
      </div>
    )
  }

  const completedCaptions = captions?.filter(c => c.status.toLowerCase() === 'completed' && !!c.blobUrl) || []
  const hasCaptions = completedCaptions.length > 0
  const activeMedia = mediaFiles && mediaFiles.length > 0 ? mediaFiles[0] : null
  const vttCaption = completedCaptions.find(c => c.format.toLowerCase() === 'vtt')

  return (
    <div className="space-y-6">
      {!hasCaptions ? (
        <EmptyState
          title="No captions generated"
          description="Captions will be compiled once the speech-to-text alignment completes."
          icon={<Subtitles className="h-6 w-6 text-accent" />}
        />
      ) : (
        <div className="max-w-3xl space-y-6">
          <SectionCard
            title="Generated Subtitle Formats"
            subtitle="Download local formats for video embedding."
          >
            <div className="grid gap-4 sm:grid-cols-3">
              {completedCaptions.map((caption) => (
                <div
                  key={caption.id}
                  className="flex flex-col items-center justify-center p-6 border border-border-custom rounded-xl bg-slate-50/40 hover:bg-slate-50 transition-colors select-none text-center"
                >
                  <Subtitles className="h-8 w-8 text-accent mb-3" />
                  <span className="text-sm font-bold text-text-main">{caption.format.toUpperCase()} Format</span>
                  <span className="text-[10px] text-text-muted mt-0.5 mb-4">Language: {caption.language.toUpperCase()}</span>

                  <a
                    href={caption.blobUrl || undefined}
                    target="_blank"
                    rel="noopener noreferrer"
                    download
                    className="inline-flex items-center gap-1 text-xs font-semibold text-accent hover:text-accent-hover transition-colors"
                  >
                    Download
                    <Download className="h-3 w-3" />
                  </a>
                </div>
              ))}
            </div>
          </SectionCard>

          <SectionCard title="Subtitle Playback Preview" subtitle="Review line layouts overlaying media.">
            <MediaPlayer
              src={activeMedia?.url}
              contentType={activeMedia?.contentType}
              vttUrl={vttCaption?.blobUrl}
              vttLanguage={vttCaption?.language}
            />
          </SectionCard>
        </div>
      )}
    </div>
  )
}
export default ProjectCaptionsTab
