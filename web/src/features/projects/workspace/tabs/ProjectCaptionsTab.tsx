import React from 'react'
import { useOutletContext } from 'react-router-dom'
import { Subtitles, Download } from 'lucide-react'
import { SectionCard } from '@/shared/components/ui/SectionCard'
import { EmptyState } from '@/shared/components/ui/EmptyState'
import type { Project } from '@/shared/services/api/projects'
import toast from 'react-hot-toast'

export const ProjectCaptionsTab: React.FC = () => {
  const project = useOutletContext<Project>()

  const isCompleted = project.status.toLowerCase() === 'completed'

  const handleDownload = (format: string) => {
    const mockSrt = `1\n00:00:00,000 --> 00:00:08,000\nWelcome to VideoHub AI. In this tutorial, we will learn how to initialize the platform.\n\n2\n00:00:08,000 --> 00:00:15,000\nOur core stack consists of React 19, TailwindCSS v4, and React Router v7.`
    const mockVtt = `WEBVTT\n\n1\n00:00:00.000 --> 00:00:08.000\nWelcome to VideoHub AI. In this tutorial, we will learn how to initialize the platform.\n\n2\n00:00:08.000 --> 00:00:15.000\nOur core stack consists of React 19, TailwindCSS v4, and React Router v7.`
    const mockAss = `[Script Info]\nTitle: VideoHub AI Captions\n\n[Events]\nDialogue: 0,0:00:00.00,0:00:08.00,Default,,0,0,0,,Welcome to VideoHub AI.\nDialogue: 0,0:00:08.00,0:00:15.00,Default,,0,0,0,,Our core stack consists of React 19...`

    let content = mockSrt
    if (format.toLowerCase() === 'vtt') content = mockVtt
    if (format.toLowerCase() === 'ass') content = mockAss

    const blob = new Blob([content], { type: 'text/plain;charset=utf-8' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `${project.name.replace(/\s+/g, '_')}_subtitles.${format.toLowerCase()}`
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)

    toast.success(`Subtitle captions downloaded in .${format.toLowerCase()} format!`)
  }

  return (
    <div className="space-y-6">
      {!isCompleted ? (
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
              {['SRT', 'VTT', 'ASS'].map((format) => (
                <div
                  key={format}
                  className="flex flex-col items-center justify-center p-6 border border-border-custom rounded-xl bg-slate-50/40 hover:bg-slate-50 transition-colors select-none text-center"
                >
                  <Subtitles className="h-8 w-8 text-accent mb-3" />
                  <span className="text-sm font-bold text-text-main">{format} Format</span>
                  <span className="text-[10px] text-text-muted mt-0.5 mb-4">SubRip Text Layer</span>

                  <button
                    onClick={() => handleDownload(format)}
                    className="inline-flex items-center gap-1 text-xs font-semibold text-accent hover:text-accent-hover transition-colors"
                  >
                    Download
                    <Download className="h-3 w-3" />
                  </button>
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
