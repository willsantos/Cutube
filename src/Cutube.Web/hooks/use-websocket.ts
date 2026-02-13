"use client";

import { HubConnectionState } from "@microsoft/signalr";
import { useEffect, useState } from "react";
import { ws } from "@/lib/websocket";

export function useWebSocket() {
  const [state, setState] = useState<HubConnectionState>(ws.state);

  useEffect(() => {
    const unsubscribe = ws.onConnectionStateChange(setState);
    ws.connect().catch(() => undefined);

    return () => {
      unsubscribe();
    };
  }, []);

  return {
    state,
    isConnected: state === HubConnectionState.Connected,
    isReconnecting: state === HubConnectionState.Reconnecting,
  };
}
