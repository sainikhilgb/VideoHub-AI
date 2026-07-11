import React from 'react'
import { PageHeader } from '@/shared/components/ui/PageHeader'
import { SectionCard } from '@/shared/components/ui/SectionCard'
import { Save } from 'lucide-react'

export const SettingsPage: React.FC = () => {
  return (
    <div className="space-y-6">
      <PageHeader
        title="Settings"
        description="Configure account, billing, and global AI processing parameters."
      />

      <div className="max-w-3xl space-y-6">
        <SectionCard title="General Profile" subtitle="Manage your identity.">
          <form className="space-y-4" onSubmit={(e) => e.preventDefault()}>
            <div className="grid gap-4 sm:grid-cols-2">
              <div>
                <label className="block text-sm font-medium text-text-main mb-1.5">
                  First Name
                </label>
                <input
                  type="text"
                  defaultValue="John"
                  className="w-full rounded-lg border border-border-custom bg-card px-3 py-2 text-sm focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-text-main mb-1.5">Last Name</label>
                <input
                  type="text"
                  defaultValue="Doe"
                  className="w-full rounded-lg border border-border-custom bg-card px-3 py-2 text-sm focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent"
                />
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-text-main mb-1.5">
                Email Address
              </label>
              <input
                type="email"
                defaultValue="john.doe@videohub.ai"
                className="w-full rounded-lg border border-border-custom bg-card px-3 py-2 text-sm focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent"
              />
            </div>
          </form>
        </SectionCard>

        <SectionCard title="AI Processing Engine" subtitle="Adjust default AI configurations.">
          <form className="space-y-4" onSubmit={(e) => e.preventDefault()}>
            <div>
              <label className="block text-sm font-medium text-text-main mb-1.5">
                Default ASR Engine
              </label>
              <select className="w-full rounded-lg border border-border-custom bg-card px-3 py-2 text-sm focus:border-accent focus:ring-1 focus:ring-accent">
                <option value="whisper-v3">OpenAI Whisper Large v3 (High Quality)</option>
                <option value="whisper-distil">Distil-Whisper (Faster)</option>
                <option value="assembly">AssemblyAI (Enterprise)</option>
              </select>
            </div>
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="diarization"
                defaultChecked
                className="rounded text-accent focus:ring-accent"
              />
              <label
                htmlFor="diarization"
                className="text-sm text-text-main font-medium select-none"
              >
                Enable Speaker Diarization (Differentiate between speakers automatically)
              </label>
            </div>
          </form>
        </SectionCard>

        <div className="flex justify-end gap-3">
          <button className="inline-flex items-center gap-1.5 rounded-lg bg-accent px-4 py-2 text-sm font-semibold text-white shadow-custom-sm hover:bg-accent-hover transition-colors">
            <Save className="h-4 w-4" />
            Save Changes
          </button>
        </div>
      </div>
    </div>
  )
}
