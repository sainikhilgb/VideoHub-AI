import React from 'react'
import { Music } from 'lucide-react'

export interface MediaPlayerProps {
  src?: string | null
  contentType?: string
  vttUrl?: string | null
  vttLanguage?: string
  onTimeUpdate?: (currentTime: number) => void
  playerRef?: React.RefObject<HTMLVideoElement | HTMLAudioElement | null>
}

export const MediaPlayer: React.FC<MediaPlayerProps> = ({
  src,
  contentType = 'video/mp4',
  vttUrl,
  vttLanguage = 'en',
  onTimeUpdate,
  playerRef,
}) => {
  const isAudio = contentType.startsWith('audio/')

  if (!src) {
    return (
      <div className="aspect-video w-full rounded-lg bg-slate-900 flex items-center justify-center text-white/50 text-xs">
        No media source loaded.
      </div>
    )
  }

  if (isAudio) {
    return (
      <div className="w-full p-8 rounded-xl bg-slate-900/40 border border-border-custom bg-card shadow-custom-sm flex flex-col items-center justify-center gap-4 select-none">
        <div className="p-4 rounded-full bg-accent/10 border border-accent/20">
          <Music className="h-10 w-10 text-accent" />
        </div>
        <audio
          ref={playerRef as React.RefObject<HTMLAudioElement>}
          src={src}
          controls
          className="w-full max-w-md focus:outline-none"
          onTimeUpdate={(e) => onTimeUpdate?.(e.currentTarget.currentTime)}
        />
      </div>
    )
  }

  return (
    <div className="aspect-video w-full rounded-lg bg-slate-950 overflow-hidden relative shadow-custom-md border border-border-custom/50 flex items-center justify-center">
      <video
        ref={playerRef as React.RefObject<HTMLVideoElement>}
        src={src}
        controls
        crossOrigin="anonymous"
        className="w-full h-full object-contain focus:outline-none"
        onTimeUpdate={(e) => onTimeUpdate?.(e.currentTarget.currentTime)}
      >
        {vttUrl && (
          <track
            kind="subtitles"
            src={vttUrl}
            srcLang={vttLanguage}
            label={vttLanguage.toUpperCase()}
            default
          />
        )}
      </video>
    </div>
  )
}
export default MediaPlayer
