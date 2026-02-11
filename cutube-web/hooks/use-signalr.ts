"use client";

import { useState, useEffect, useRef, useCallback } from "react";
import * as signalR from "@microsoft/signalr";

interface SignalRCallbacks {
  onDownloadStatusChanged?: (correlationId: string, newStatus: string) => void;
  onDownloadProgress?: (correlationId: string, progress: number, speed: number) => void;
  onDownloadCompleted?: (correlationId: string) => void;
  onDownloadFailed?: (correlationId: string, error: string) => void;
}

export function useSignalR(callbacks: SignalRCallbacks) {
  const [isConnected, setIsConnected] = useState(false);
  const [connectionError, setConnectionError] = useState<string | null>(null);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/downloads`)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    connectionRef.current = connection;

    connection.on("DownloadStatusChanged", (correlationId: string, newStatus: string) => {
      callbacks.onDownloadStatusChanged?.(correlationId, newStatus);
    });

    connection.on("DownloadProgress", (correlationId: string, data: { progress: number; speed: number }) => {
      callbacks.onDownloadProgress?.(correlationId, data.progress, data.speed);
    });

    connection.on("DownloadCompleted", (correlationId: string) => {
      callbacks.onDownloadCompleted?.(correlationId);
    });

    connection.on("DownloadFailed", (correlationId: string, error: string) => {
      callbacks.onDownloadFailed?.(correlationId, error);
    });

    connection
      .start()
      .then(() => {
        setIsConnected(true);
        setConnectionError(null);
      })
      .catch((err) => {
        setConnectionError(err.message);
      });

    connection.onreconnecting(() => {
      setIsConnected(false);
    });

    connection.onreconnected(() => {
      setIsConnected(true);
      setConnectionError(null);
    });

    connection.onclose(() => {
      setIsConnected(false);
    });

    return () => {
      connection.stop();
    };
  }, []);

  return { isConnected, connectionError };
}
