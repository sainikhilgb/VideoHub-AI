import React from 'react'
import { useOutletContext } from 'react-router-dom'
import { FileText, Play } from 'lucide-react'
import { SectionCard } from '@/shared/components/ui/SectionCard'
import { EmptyState } from '@/shared/components/ui/EmptyState'
import type { Project } from '@/shared/services/api/projects'

export const ProjectTranscriptTab: React.FC = () => {
  const project = useOutletContext<Project>()

  const isCompleted = project.status.toLowerCase() === 'completed'

  return (
    <div className="space-y-6">
      {!isCompleted ? (
        <EmptyState
          title="Transcript not generated yet"
          description="We are still transcribing speech for this workspace. Check back shortly."
          icon={<FileText className="h-6 w-6 text-accent" />}
        />
      ) : (
        <div className="grid gap-6 md:grid-cols-3">
          {/* Main timeline */}
          <div className="md:col-span-2 space-y-6">
            <SectionCard title="Video Preview" subtitle="Review output and video media file.">
              <div className="aspect-video w-full rounded-lg bg-slate-900 flex items-center justify-center text-white relative group overflow-hidden">
                <Play className="h-16 w-16 text-white/80 group-hover:scale-110 group-hover:text-white transition-all cursor-pointer drop-shadow-md" />
                <div className="absolute bottom-4 left-4 right-4 flex items-center justify-between text-xs text-white/60 bg-black/40 px-3 py-1.5 rounded-md">
                  <span>interview_raw.mp4</span>
                  <span>00:00 / 02:45</span>
                </div>
              </div>
            </SectionCard>

            <SectionCard
              title="Speech Transcript editor"
              subtitle="Review and correct generated speech segments."
            >
              <div className="space-y-4">
                <div className="p-3.5 rounded-lg bg-slate-50 border border-border-custom/50 flex gap-4">
                  <span className="text-xs font-semibold text-accent shrink-0 mt-0.5">
                    00:00 - 00:08
                  </span>
                  <div className="space-y-1 flex-1">
                    <p className="text-xs font-semibold text-text-main">Speaker 1</p>
                    <p className="text-sm text-text-main leading-relaxed m-0">
                      Welcome to VideoHub AI. In this tutorial, we will learn how to initialize the
                      platform.
                    </p>
                  </div>
                </div>
                <div className="p-3.5 rounded-lg bg-slate-50 border border-border-custom/50 flex gap-4">
                  <span className="text-xs font-semibold text-accent shrink-0 mt-0.5">
                    00:08 - 00:15
                  </span>
                  <div className="space-y-1 flex-1">
                    <p className="text-xs font-semibold text-text-main">Speaker 2</p>
                    <p className="text-sm text-text-main leading-relaxed m-0">
                      Our core stack consists of React 19, TailwindCSS v4, and React Router v7.
                    </p>
                  </div>
                </div>
              </div>
            </SectionCard>
          </div>

          {/* Quick info */}
          <div className="space-y-6">
            <SectionCard title="Speech Technicals">
              <dl className="space-y-3 text-xs select-none">
                <div className="flex justify-between py-1 border-b border-border-custom/40">
                  <dt className="text-text-muted">Detected Language</dt>
                  <dd className="font-semibold text-text-main">English (US)</dd>
                </div>
                <div className="flex justify-between py-1 border-b border-border-custom/40">
                  <dt className="text-text-muted">Speakers count</dt>
                  <dd className="font-semibold text-text-main">2 Speakers</dd>
                </div>
                <div className="flex justify-between py-1 border-b border-border-custom/40">
                  <dt className="text-text-muted">Diarization Accuracy</dt>
                  <dd className="font-semibold text-success">98.4% Confidence</dd>
                </div>
              </dl>
            </SectionCard>
          </div>
        </div>
      )}
    </div>
  )
}
export default ProjectTranscriptTab
