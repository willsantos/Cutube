"use client";

import { useState, useEffect, useRef } from "react";
import * as signalR from "@microsoft/signalr";
import { SIGNALR_HUB_URL } from "@/lib/constants";

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
  const callbacksRef = useRef(callbacks);

  useEffect(() => {
    callbacksRef.current = callbacks;
  }, [callbacks]);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(SIGNALR_HUB_URL)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    connectionRef.current = connection;

    connection.on("DownloadStatusChanged", (correlationId: string, newStatus: string) => {
      callbacksRef.current.onDownloadStatusChanged?.(correlationId, newStatus);
    });

    connection.on("DownloadProgress", (correlationId: string, data: { progress: number; speed: number }) => {
      callbacksRef.current.onDownloadProgress?.(correlationId, data.progress, data.speed);
    });

    connection.on("DownloadCompleted", (correlationId: string) => {
      callbacksRef.current.onDownloadCompleted?.(correlationId);
    });

    connection.on("DownloadFailed", (correlationId: string, error: string) => {
      callbacksRef.current.onDownloadFailed?.(correlationId, error);
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
