import React, { useState, useEffect } from 'react'
import { useOutletContext } from 'react-router-dom'
import { Subtitles, Download, Loader2, Sparkles } from 'lucide-react'
import { SectionCard } from '@/shared/components/ui/SectionCard'
import { EmptyState } from '@/shared/components/ui/EmptyState'
import {
  useProjectCaptions,
  useProjectMedia,
  useProjectCombinedMedia,
  useCombineMedia,
  type Project,
} from '@/shared/services/api/projects'
import { MediaPlayer } from '@/shared/components/ui/MediaPlayer'
import toast from 'react-hot-toast'

export const ProjectCaptionsTab: React.FC = () => {
  const project = useOutletContext<Project>()

  const [selectedVttId, setSelectedVttId] = useState<string>('')

  const { data: captions, isLoading } = useProjectCaptions(project.id)
  const { data: mediaFiles } = useProjectMedia(project.id)
  const { data: combinedMediaList } = useProjectCombinedMedia(project.id)
  const combineMedia = useCombineMedia()

  const completedCaptions =
    captions?.filter((c) => c.status.toLowerCase() === 'completed' && !!c.blobUrl) || []
  const hasCaptions = completedCaptions.length > 0
  const activeMedia = mediaFiles && mediaFiles.length > 0 ? mediaFiles[0] : null

  const vttCaptions = completedCaptions.filter((c) => c.format.toLowerCase() === 'vtt')
  const activeVtt = vttCaptions.find((c) => c.id === selectedVttId) || vttCaptions[0] || null

  useEffect(() => {
    if (vttCaptions.length > 0 && !selectedVttId) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setSelectedVttId(vttCaptions[0].id)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [vttCaptions])

  const handleCombine = async (captionId: string) => {
    if (!activeMedia) {
      toast.error('No active media file is available for combining subtitles.')
      return
    }
    const toastId = toast.loading('Queueing subtitles video combining job...')
    try {
      await combineMedia.mutateAsync({
        projectId: project.id,
        mediaFileId: activeMedia.id,
        captionFileId: captionId,
        muxType: 'SoftMux',
      })
      toast.success('Subtitles combined video processing job queued!', { id: toastId })
    } catch (err: any) {
      toast.error(err.message || 'Failed to start combined video job.', { id: toastId })
    }
  }

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-12">
        <Loader2 className="h-8 w-8 text-accent animate-spin mb-2" />
        <p className="text-xs text-text-muted">Loading project captions...</p>
      </div>
    )
  }

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
                  <span className="text-sm font-bold text-text-main">
                    {caption.format.toUpperCase()} Format
                  </span>
                  <span className="text-sm text-text-muted mt-0.5 mb-4">
                    Language: {caption.language.toUpperCase()}
                  </span>

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

          <SectionCard
            title="Subtitle Playback Preview"
            subtitle="Review line layouts overlaying media."
          >
            <div className="space-y-4">
              {vttCaptions.length > 1 && (
                <div className="flex items-center gap-2 pb-2">
                  <label
                    htmlFor="preview-language-select"
                    className="text-xs font-medium text-text-muted"
                  >
                    Select Language Overlay:
                  </label>
                  <select
                    id="preview-language-select"
                    value={selectedVttId}
                    onChange={(e) => setSelectedVttId(e.target.value)}
                    className="rounded-lg border border-border-custom bg-card px-2.5 py-1 text-xs font-medium text-text-main focus:outline-none focus:ring-1 focus:ring-accent"
                  >
                    {vttCaptions.map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.language.toUpperCase()}
                      </option>
                    ))}
                  </select>
                </div>
              )}

              <MediaPlayer
                src={activeMedia?.url}
                contentType={activeMedia?.contentType}
                vttUrl={activeVtt?.blobUrl}
                vttLanguage={activeVtt?.language}
              />
            </div>
          </SectionCard>

          <SectionCard
            title="Combine Subtitles & Video"
            subtitle="Render captions directly into the video file via FFmpeg background tasks."
          >
            <div className="space-y-4">
              {/* Trigger controls */}
              <div className="flex flex-col sm:flex-row gap-4 p-4 border border-border-custom bg-slate-50/40 rounded-xl">
                <div className="flex-1 space-y-1">
                  <label
                    htmlFor="combine-language-select"
                    className="text-xs font-semibold text-text-main"
                  >
                    Choose Subtitle Language
                  </label>
                  <select
                    id="combine-language-select"
                    value={selectedVttId}
                    onChange={(e) => setSelectedVttId(e.target.value)}
                    className="w-full mt-1.5 rounded-lg border border-border-custom bg-card px-3 py-2 text-xs text-text-main focus:outline-none focus:ring-1 focus:ring-accent"
                  >
                    {vttCaptions.map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.language.toUpperCase()} (VTT format)
                      </option>
                    ))}
                  </select>
                </div>

                <div className="flex items-end">
                  <button
                    type="button"
                    disabled={combineMedia.isPending || !selectedVttId || !activeMedia}
                    onClick={() => handleCombine(selectedVttId)}
                    className="w-full sm:w-auto h-[38px] flex items-center justify-center gap-1.5 rounded-lg bg-accent px-4 text-xs font-semibold text-white hover:bg-accent-hover focus:outline-none focus:ring-2 focus:ring-accent focus:ring-offset-2 disabled:bg-accent/70 transition-colors cursor-pointer"
                  >
                    {combineMedia.isPending ? (
                      <Loader2 className="h-3.5 w-3.5 animate-spin" />
                    ) : (
                      <Sparkles className="h-3.5 w-3.5" />
                    )}
                    Mux Subtitles
                  </button>
                </div>
              </div>

              {/* Existing / processing combined exports */}
              {combinedMediaList && combinedMediaList.length > 0 && (
                <div className="space-y-3 pt-2">
                  <span className="text-xs font-bold text-text-main">
                    Completed & Processing Muxes
                  </span>
                  <div className="grid gap-3">
                    {combinedMediaList.map((cm) => (
                      <div
                        key={cm.id}
                        className="flex items-center justify-between p-3.5 border border-border-custom rounded-lg bg-slate-50/50"
                      >
                        <div className="flex flex-col gap-0.5">
                          <span className="text-xs font-semibold text-text-main">
                            Muxed Video ({cm.language.toUpperCase()})
                          </span>
                          <span className="text-[10px] text-text-muted">
                            Created: {new Date(cm.createdAt).toLocaleDateString()}
                          </span>
                          {cm.status === 'Failed' && cm.error && (
                            <p className="text-[10px] text-danger max-w-[280px] sm:max-w-md mt-1.5 break-words bg-danger/5 p-2 rounded border border-danger/10 font-medium">
                              Error: {cm.error}
                            </p>
                          )}
                        </div>

                        <div className="flex items-center gap-3">
                          {/* Status badge */}
                          <span
                            className={`px-2.5 py-0.5 rounded-full text-[10px] font-semibold tracking-wider uppercase select-none ${
                              cm.status === 'Completed'
                                ? 'bg-success/10 text-success'
                                : cm.status === 'Failed'
                                  ? 'bg-danger/10 text-danger'
                                  : 'bg-accent/10 text-accent animate-pulse'
                            }`}
                          >
                            {cm.status}
                          </span>

                          {/* Download button */}
                          {cm.status === 'Completed' && cm.url && (
                            <a
                              href={cm.url}
                              target="_blank"
                              rel="noopener noreferrer"
                              download
                              className="flex items-center gap-1 text-xs font-semibold text-accent hover:text-accent-hover transition-colors"
                            >
                              Download Video
                              <Download className="h-3.5 w-3.5" />
                            </a>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </SectionCard>
        </div>
      )}
    </div>
  )
}
export default ProjectCaptionsTab
