import { useEffect, useRef } from 'react'
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr'
import { getAccessToken } from '@/shared/services/api/client'

import { apiClient } from '@/shared/services/api/client'

const getHubUrl = (): string => {
  const baseURL = apiClient.defaults.baseURL || ''
  try {
    const url = new URL(baseURL)
    return `${url.origin}/hubs/projects`
  } catch {
    // Fallback for relative API base URLs (e.g. '/api' or '/api/v1')
    return baseURL.replace(/\/api(\/v1)?\/?$/, '/hubs/projects')
  }
}

export interface JobUpdateEvent {
  jobId: string
  projectId: string
  status: string
  statusMessage: string | null
}

export interface CaptionFileUpdateEvent {
  captionFileId: string
  projectId: string
  jobId: string
  format: string
  language: string
  status: string
  blobUrl: string | null
  errorMessage: string | null
}

export interface CombinedMediaUpdateEvent {
  id: string
  projectId: string
  mediaFileId: string
  language: string
  muxType: string
  status: string
  blobUrl: string | null
  error: string | null
  createdAt: string
}

interface SignalROptions {
  projectId: string | undefined
  onJobUpdate?: (event: JobUpdateEvent) => void
  onCaptionFileUpdate?: (event: CaptionFileUpdateEvent) => void
  onCombinedMediaUpdate?: (event: CombinedMediaUpdateEvent) => void
}

export const useSignalR = ({
  projectId,
  onJobUpdate,
  onCaptionFileUpdate,
  onCombinedMediaUpdate,
}: SignalROptions) => {
  const connectionRef = useRef<HubConnection | null>(null)

  useEffect(() => {
    if (!projectId) return

    const token = getAccessToken()
    if (!token) return

    const hubUrl = getHubUrl()

    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connectionRef.current = connection

    const startConnection = async () => {
      try {
        await connection.start()
        console.log('SignalR connected successfully.')

        // Join project-specific WebSocket group
        await connection.invoke('JoinProjectGroup', projectId)
      } catch (err) {
        console.error('Error starting SignalR connection:', err)
      }
    }

    if (onJobUpdate) {
      connection.on('ReceiveJobUpdate', onJobUpdate)
    }
    if (onCaptionFileUpdate) {
      connection.on('ReceiveCaptionFileUpdate', onCaptionFileUpdate)
    }
    if (onCombinedMediaUpdate) {
      connection.on('ReceiveCombinedMediaUpdate', onCombinedMediaUpdate)
    }

    startConnection()

    return () => {
      const stopConnection = async () => {
        if (connection.state === HubConnectionState.Connected) {
          try {
            await connection.invoke('LeaveProjectGroup', projectId)
          } catch (err) {
            console.error('Error leaving project group:', err)
          }
        }
        await connection.stop()
        console.log('SignalR connection stopped.')
      }
      stopConnection()
    }
  }, [projectId, onJobUpdate, onCaptionFileUpdate, onCombinedMediaUpdate])
}
