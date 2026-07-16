import React from 'react'
import { useOutletContext } from 'react-router-dom'
import { Subtitles, Download, Loader2 } from 'lucide-react'
import { SectionCard } from '@/shared/components/ui/SectionCard'
import { EmptyState } from '@/shared/components/ui/EmptyState'
import { useProjectCaptions, type Project } from '@/shared/services/api/projects'

export const ProjectCaptionsTab: React.FC = () => {
  const project = useOutletContext<Project>()
  
  const { data: captions, isLoading } = useProjectCaptions(project.id)

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-12">
        <Loader2 className="h-8 w-8 text-accent animate-spin mb-2" />
        <p className="text-xs text-text-muted">Loading project captions...</p>
      </div>
    )
  }

  const completedCaptions = captions?.filter(c => c.status.toLowerCase() === 'completed') || []
  const hasCaptions = completedCaptions.length > 0

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

          <SectionCard title="Subtitle Preview Frame" subtitle="Review line layouts.">
            <div className="aspect-video w-full max-w-lg mx-auto rounded-lg bg-slate-900 flex items-end justify-center pb-8 text-white relative overflow-hidden shadow-custom-md">
              <span className="bg-black/75 px-4 py-1.5 rounded-md text-sm font-medium tracking-wide">
                Welcome to VideoHub AI. In this tutorial...
              </span>
            </div>
          </SectionCard>
        </div>
      )}
    </div>
  )
}
export default ProjectCaptionsTab
