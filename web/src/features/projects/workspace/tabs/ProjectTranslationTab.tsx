import React, { useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import { Languages, Plus, Download, Globe } from 'lucide-react'
import { SectionCard } from '@/shared/components/ui/SectionCard'
import { EmptyState } from '@/shared/components/ui/EmptyState'
import type { Project } from '@/shared/services/api/projects'
import toast from 'react-hot-toast'

export const ProjectTranslationTab: React.FC = () => {
  const project = useOutletContext<Project>()
  const [selectedLanguage, setSelectedLanguage] = useState('es')
  const [translations, setTranslations] = useState<{ code: string; name: string }[]>([])

  const isCompleted = project.status.toLowerCase() === 'completed'

  const handleAddTranslation = () => {
    const languagesMap: Record<string, string> = {
      es: 'Spanish (Castilian)',
      fr: 'French (French)',
      de: 'German (Deutsch)',
      ja: 'Japanese (Nihongo)',
    }

    const name = languagesMap[selectedLanguage]
    if (translations.some((t) => t.code === selectedLanguage)) {
      toast.error('Translation language already configured!')
      return
    }

    setTranslations([...translations, { code: selectedLanguage, name }])
    toast.success(`Translation job spawned for ${name}!`)
  }

  return (
    <div className="space-y-6">
      {!isCompleted ? (
        <EmptyState
          title="No translations available"
          description="Subtitles translation features unlock once the original speech transcript is ready."
          icon={<Languages className="h-6 w-6 text-accent" />}
        />
      ) : (
        <div className="grid gap-6 md:grid-cols-3">
          {/* Main translations listing */}
          <div className="md:col-span-2 space-y-6">
            <SectionCard
              title="Workspace translations"
              subtitle="Translate speech layers dynamically."
            >
              {translations.length === 0 ? (
                <EmptyState
                  title="No subtitle translations generated"
                  description="Choose a language on the right sidebar to generate localized captions."
                  icon={<Globe className="h-5 w-5 text-accent" />}
                />
              ) : (
                <div className="space-y-3">
                  {translations.map((trans) => (
                    <div
                      key={trans.code}
                      className="flex items-center justify-between p-4 rounded-xl border border-border-custom bg-card shadow-custom-sm hover:shadow-custom-md transition-all duration-200"
                    >
                      <div className="flex items-center gap-3">
                        <Globe className="h-5 w-5 text-accent" />
                        <div>
                          <p className="text-sm font-semibold text-text-main m-0">{trans.name}</p>
                          <p className="text-[10px] text-text-muted mt-0.5 m-0">
                            Language code: {trans.code.toUpperCase()}
                          </p>
                        </div>
                      </div>

                      <div className="flex items-center gap-3">
                        <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[10px] font-semibold bg-green-50 border border-green-200 text-success">
                          Ready
                        </span>
                        <button
                          onClick={() =>
                            toast.success(`Downloading ${trans.code.toUpperCase()} subtitle...`)
                          }
                          className="p-1.5 text-text-muted hover:text-accent rounded-md hover:bg-slate-50 transition-colors"
                          title="Download translated SRT"
                        >
                          <Download className="h-4 w-4" />
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </SectionCard>
          </div>

          {/* Configuration sidebar */}
          <div className="space-y-6">
            <SectionCard
              title="Translate Subtitles"
              subtitle="Choose target localization language."
            >
              <div className="space-y-4">
                <div>
                  <label
                    htmlFor="targetLang"
                    className="block text-xs font-semibold text-text-muted mb-1.5 uppercase"
                  >
                    Target Language
                  </label>
                  <select
                    id="targetLang"
                    value={selectedLanguage}
                    onChange={(e) => setSelectedLanguage(e.target.value)}
                    className="w-full rounded-lg border border-border-custom bg-card px-3 py-2 text-sm text-text-main focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent"
                  >
                    <option value="es">Spanish (Español)</option>
                    <option value="fr">French (Français)</option>
                    <option value="de">German (Deutsch)</option>
                    <option value="ja">Japanese (日本語)</option>
                  </select>
                </div>

                <button
                  onClick={handleAddTranslation}
                  className="w-full inline-flex items-center justify-center gap-1.5 rounded-lg bg-accent py-2 text-sm font-semibold text-white hover:bg-accent-hover transition-colors"
                >
                  <Plus className="h-4 w-4" />
                  Spawn Translation
                </button>
              </div>
            </SectionCard>
          </div>
        </div>
      )}
    </div>
  )
}
export default ProjectTranslationTab
