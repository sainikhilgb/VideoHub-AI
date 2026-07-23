import { useEffect, useRef } from 'react'
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { getAccessToken, apiClient } from '@/shared/services/api/client'

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
  const queryClient = useQueryClient()
  const connectionRef = useRef<HubConnection | null>(null)

  const onJobUpdateRef = useRef(onJobUpdate)
  const onCaptionFileUpdateRef = useRef(onCaptionFileUpdate)
  const onCombinedMediaUpdateRef = useRef(onCombinedMediaUpdate)

  // Keep callback refs up to date
  useEffect(() => {
    onJobUpdateRef.current = onJobUpdate
  }, [onJobUpdate])

  useEffect(() => {
    onCaptionFileUpdateRef.current = onCaptionFileUpdate
  }, [onCaptionFileUpdate])

  useEffect(() => {
    onCombinedMediaUpdateRef.current = onCombinedMediaUpdate
  }, [onCombinedMediaUpdate])

  useEffect(() => {
    if (!projectId) return

    const token = getAccessToken()
    if (!token) return

    const hubUrl = getHubUrl()

    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => getAccessToken() ?? token,
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

    connection.onreconnected(async (connectionId) => {
      console.log('SignalR reconnected:', connectionId)
      try {
        await connection.invoke('JoinProjectGroup', projectId)
        // Refresh all cached queries to fetch updates missed while offline
        queryClient.invalidateQueries({ queryKey: ['project', projectId] })
        queryClient.invalidateQueries({ queryKey: ['projectTranscript', projectId] })
        queryClient.invalidateQueries({ queryKey: ['projectCaptions', projectId] })
        queryClient.invalidateQueries({ queryKey: ['projectCombinedMedia', projectId] })
      } catch (err) {
        console.error('Error re-joining project group after reconnect:', err)
      }
    })

    connection.on('ReceiveJobUpdate', (event: JobUpdateEvent) => {
      onJobUpdateRef.current?.(event)
    })
    connection.on('ReceiveCaptionFileUpdate', (event: CaptionFileUpdateEvent) => {
      onCaptionFileUpdateRef.current?.(event)
    })
    connection.on('ReceiveCombinedMediaUpdate', (event: CombinedMediaUpdateEvent) => {
      onCombinedMediaUpdateRef.current?.(event)
    })

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
  }, [projectId, queryClient])
}
