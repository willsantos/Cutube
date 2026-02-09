# Fase 2.4: Frontend Next.js + TypeScript

**Status:** 🎯 Planejamento
**Épico:** Cutube-2i6 (Épico 2: Arquitetura Híbrida CLI + API)
**Duração:** 7-10 dias
**Responsável:** Frontend Developer (full-stack)
**Prioridade:** 🔥 Alta
**Dependência:** ✅ Fase 2.3 completa (WebSocket + REST API)

---

## Objetivo

Criar interface web moderna usando Next.js 15, React 19, TypeScript e TailwindCSS que se comunica com a API REST e SignalR para gerenciar downloads de vídeos em tempo real.

**Benefícios:**
- Interface visual intuitiva para downloads
- Visualização de progresso em tempo real via WebSocket
- Design responsivo (mobile, tablet, desktop)
- Dark mode suporte
- UI moderna com shadcn/ui components
- Type safety com TypeScript strict mode

---

## Visão Arquitetural

```
┌─────────────────────────────────────────────────────────────┐
│                      Frontend Layer                         │
│                    Next.js 15 App Router                    │
│                                                             │
│  /app/page.tsx                 - Dashboard principal        │
│  /app/downloads/page.tsx       - Lista de downloads        │
│  /app/downloads/[id]/page.tsx  - Detalhes de download      │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                    Components Layer                         │
│                  React + TypeScript                        │
│                                                             │
│  UI Components (shadcn/ui):                                │
│  ├── DownloadForm              - Form para criar download  │
│  ├── DownloadsList             - Lista de downloads        │
│  ├── DownloadCard              - Card de download          │
│  ├── ProgressBar               - Barra de progresso        │
│  ├── StatusBadge               - Badge colorido            │
│  ├── VideoPreview              - Preview + metadados       │
│  ├── TimeRangePicker           - Input timerange           │
│  └── DownloadActions           - Ações (cancel, retry)     │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                     Services Layer                          │
│                                                             │
│  lib/api.ts                    - REST API client            │
│  lib/websocket.ts              - SignalR client            │
│  lib/query.ts                  - React Query (cache)       │
│  hooks/                        - Custom React hooks         │
│    ├── useDownloads.ts                                    │
│    ├── useDownloadProgress.ts                             │
│    └── useVideoMetadata.ts                                │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                      Backend Layer                          │
│              ASP.NET Core API (Fase 2.2/2.3)                │
│                                                             │
│  /api/downloads         - REST endpoints                   │
│  /api/videos/info       - Video metadata                   │
│  /hubs/downloads        - SignalR Hub (WebSocket)          │
└─────────────────────────────────────────────────────────────┘
```

**Data Flow:**

```
User Action (Submit Form)
    │
    ├─> POST /api/downloads (REST)
    │       │
    │       └─> Response: 202 Accepted + downloadId
    │
    └─> WebSocket: Join "download:{id}" group
            │
            ├─> Receive: DownloadStarted event
            │       └─> Update UI (show card)
            │
            ├─> Receive: DownloadProgress events (1-2s)
            │       └─> Update UI (progress bar, speed, eta)
            │
            └─> Receive: DownloadCompleted/Failed events
                    └─> Update UI (final status, download link)
```

---

## Tarefas

### 2.4.1 Criar projeto Next.js 15

**Estimativa:** 2 horas
**Comandos:**

```bash
# Criar projeto Next.js 15
npx create-next-app@latest cutube-web \
  --typescript \
  --tailwind \
  --app \
  --no-src-dir \
  --import-alias "@/*" \
  --eslint

cd cutube-web

# Adicionar dependências adicionais
npm install @microsoft/signalr
npm install date-fns      # Formatação de datas
npm install lucide-react  # Icons
npm install class-variance-authority  # Variantes de componentes
npm install clsx tailwind-merge       # Merge de classes

# Adicionar shadcn/ui
npx shadcn@latest init
```

**Configuração TypeScript (tsconfig.json):**

```json
{
  "compilerOptions": {
    "target": "ES2020",
    "lib": ["dom", "dom.iterable", "esnext"],
    "allowJs": true,
    "skipLibCheck": true,
    "strict": true,
    "noEmit": true,
    "esModuleInterop": true,
    "module": "esnext",
    "moduleResolution": "bundler",
    "resolveJsonModule": true,
    "isolatedModules": true,
    "jsx": "preserve",
    "incremental": true,
    "plugins": [
      {
        "name": "next"
      }
    ],
    "paths": {
      "@/*": ["./*"]
    }
  },
  "include": ["next-env.d.ts", "**/*.ts", "**/*.tsx", ".next/types/**/*.ts"],
  "exclude": ["node_modules"]
}
```

**Configuração Tailwind (tailwind.config.ts):**

```typescript
import type { Config } from "tailwindcss";

const config: Config = {
  darkMode: ["class"],
  content: [
    "./pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./components/**/*.{js,ts,jsx,tsx,mdx}",
    "./app/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  theme: {
    extend: {
      colors: {
        border: "hsl(var(--border))",
        input: "hsl(var(--input))",
        ring: "hsl(var(--ring))",
        background: "hsl(var(--background))",
        foreground: "hsl(var(--foreground))",
        primary: {
          DEFAULT: "hsl(var(--primary))",
          foreground: "hsl(var(--primary-foreground))",
        },
        secondary: {
          DEFAULT: "hsl(var(--secondary))",
          foreground: "hsl(var(--secondary-foreground))",
        },
        destructive: {
          DEFAULT: "hsl(var(--destructive))",
          foreground: "hsl(var(--destructive-foreground))",
        },
        muted: {
          DEFAULT: "hsl(var(--muted))",
          foreground: "hsl(var(--muted-foreground))",
        },
        accent: {
          DEFAULT: "hsl(var(--accent))",
          foreground: "hsl(var(--accent-foreground))",
        },
        card: {
          DEFAULT: "hsl(var(--card))",
          foreground: "hsl(var(--card-foreground))",
        },
      },
      borderRadius: {
        lg: "var(--radius)",
        md: "calc(var(--radius) - 2px)",
        sm: "calc(var(--radius) - 4px)",
      },
    },
  },
  plugins: [require("tailwindcss-animate")],
};

export default config;
```

**Checklist:**
- [ ] Criar projeto Next.js com App Router
- [ ] Configurar TypeScript strict mode
- [ ] Configurar TailwindCSS com dark mode
- [ ] Configurar path aliases (@/*)
- [ ] Adicionar ESLint + Prettier
- [ ] Adicionar dependências (@microsoft/signalr, lucide-react, etc)
- [ ] Build sem erros: `npm run build`
- [ ] Dev server rodando: `npm run dev` (http://localhost:3000)

**Critérios de aceito:**
- ✅ Projeto criado com Next.js 15
- ✅ TypeScript sem erros
- ✅ TailwindCSS funcionando (classes aplicadas)
- ✅ App Router configurado
- ✅ Build OK

---

### 2.4.2 Setup shadcn/ui components

**Estimativa:** 3 horas
**Comandos:**

```bash
# Inicializar shadcn/ui
npx shadcn@latest init

# Adicionar componentes base
npx shadcn@latest add button
npx shadcn@latest add card
npx shadcn@latest add input
npx shadcn@latest add label
npx shadcn@latest add form
npx shadcn@latest add select
npx shadcn@latest add badge
npx shadcn@latest add progress
npx shadcn@latest add toast
npx shadcn@latest add switch
npx shadcn@latest add dialog
npx shadcn@latest add separator
```

**Configuração components.json:**

```json
{
  "$schema": "https://ui.shadcn.com/schema.json",
  "style": "default",
  "rsc": true,
  "tsx": true,
  "tailwind": {
    "config": "tailwind.config.ts",
    "css": "app/globals.css",
    "baseColor": "slate",
    "cssVariables": true,
    "prefix": ""
  },
  "aliases": {
    "components": "@/components",
    "utils": "@/lib/utils",
    "ui": "@/components/ui",
    "lib": "@/lib",
    "hooks": "@/hooks"
  }
}
```

**Utils file (lib/utils.ts):**

```typescript
import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatBytes(bytes: number): string {
  if (bytes === 0) return "0 Bytes";
  const k = 1024;
  const sizes = ["Bytes", "KB", "MB", "GB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return Math.round(bytes / Math.pow(k, i) * 100) / 100 + " " + sizes[i];
}

export function formatSpeed(bytesPerSecond: number): string {
  return formatBytes(bytesPerSecond) + "/s";
}

export function formatDuration(seconds: number): string {
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const s = Math.floor(seconds % 60);
  return `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
}

export function formatTimeRange(timeString: string): string {
  // "00:01:30" -> "1m 30s"
  const parts = timeString.split(':');
  if (parts.length !== 3) return timeString;

  const hours = parseInt(parts[0], 10);
  const minutes = parseInt(parts[1], 10);
  const seconds = parseInt(parts[2], 10);

  const result: string[] = [];
  if (hours > 0) result.push(`${hours}h`);
  if (minutes > 0) result.push(`${minutes}m`);
  if (seconds > 0) result.push(`${seconds}s`);

  return result.join(' ') || '0s';
}
```

**Checklist:**
- [ ] Inicializar shadcn/ui com components.json
- [ ] Adicionar componentes base (button, card, input, etc)
- [ ] Configurar CSS variables (globals.css)
- [ ] Criar lib/utils.ts com utilitários
- [ ] Testar componentes (criar página de teste)
- [ ] Verificar dark mode funcionando

**Critérios de aceito:**
- ✅ shadcn/ui configurado
- ✅ Componentes base instalados
- ✅ Dark mode funcionando
- ✅ Componentes renderizam corretamente

---

### 2.4.3 Criar tipos TypeScript

**Estimativa:** 2 horas
**Arquivos:**
```
types/
  ├── download.ts                  - Tipos de download
  ├── video.ts                     - Tipos de vídeo
  └── api.ts                       - Tipos gerais de API
```

**types/download.ts:**

```typescript
// types/download.ts
export type DownloadStatus =
  | "queued"
  | "downloading"
  | "processing"
  | "completed"
  | "failed"
  | "cancelled";

export interface DownloadRequest {
  url: string;
  outputPath?: string;
  startTime?: string;      // "HH:MM:SS"
  endTime?: string;        // "HH:MM:SS"
  audioOnly?: boolean;
  customFilename?: string;
}

export interface DownloadProgress {
  downloadId: string;
  progress: number;         // 0-100
  speed: number;            // bytes/s
  eta?: string;             // "HH:MM:SS"
  downloadedBytes: number;
  totalBytes: number;
  status: DownloadStatus;
}

export interface DownloadSummary {
  downloadId: string;
  url: string;
  status: DownloadStatus;
  progress: number;
  filePath?: string;
  createdAt: string;        // ISO date
  completedAt?: string;     // ISO date
  errorMessage?: string;
}

export interface DownloadDetails extends DownloadSummary {
  speed: number;
  eta?: string;
  downloadedBytes?: number;
  totalBytes?: number;
}

// SignalR Events
export interface DownloadStartedEvent {
  downloadId: string;
  url: string;
  startedAt: string;        // ISO date
}

export interface DownloadProgressEvent {
  downloadId: string;
  progress: number;
  speed: number;
  eta?: string;
  downloadedBytes: number;
  totalBytes: number;
  status: string;
}

export interface DownloadCompletedEvent {
  downloadId: string;
  filePath: string;
  size: number;
  duration: number;         // seconds
  completedAt: string;      // ISO date
}

export interface DownloadFailedEvent {
  downloadId: string;
  error: string;
  failedAt: string;         // ISO date
}
```

**types/video.ts:**

```typescript
// types/video.ts
export interface VideoMetadata {
  id: string;
  title: string;
  uploader: string;
  duration: string;         // "HH:MM:SS"
  thumbnailUrl: string;
  viewCount?: number;
  uploadDate?: string;      // ISO date
  formats: VideoFormat[];
}

export interface VideoFormat {
  formatId: string;
  extension: string;
  resolution?: string;      // "1920x1080"
  fileSize?: number;        // bytes
}

export interface VideoFormatOption {
  label: string;            // "1080p (MP4) - 150MB"
  value: string;            // formatId
  extension: string;
  resolution?: string;
  fileSize?: number;
}
```

**types/api.ts:**

```typescript
// types/api.ts
export interface ApiResponse<T> {
  data: T;
  error?: string;
}

export interface ApiError {
  type: string;
  title: string;
  status: number;
  detail: string;
  errors?: Record<string, string[]>;
}

export interface CreateDownloadResponse {
  downloadId: string;
  status: string;
  message?: string;
}

export interface GetDownloadsResponse {
  downloads: DownloadSummary[];
  totalCount: number;
}
```

**Checklist:**
- [ ] Criar types/download.ts com todos tipos de download
- [ ] Criar types/video.ts com tipos de metadados
- [ ] Criar types/api.ts com tipos de resposta HTTP
- [ ] Exportar tipos como barrel export (types/index.ts)
- [ ] Verificar TypeScript sem erros

**Critérios de aceito:**
- ✅ Todos tipos definidos
- ✅ TypeScript compila sem erros
- ✅ Tipos compatíveis com API backend

---

### 2.4.4 Criar API client (REST)

**Estimativa:** 4 horas
**Arquivos:**
```
lib/
  ├── api.ts                      - REST API client
  └── api-helpers.ts              - Helpers para fetch
```

**lib/api-helpers.ts:**

```typescript
// lib/api-helpers.ts
import { ApiError } from "@/types/api";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

export async function fetchApi<T>(
  endpoint: string,
  options?: RequestInit
): Promise<T> {
  const url = `${API_BASE_URL}${endpoint}`;

  const response = await fetch(url, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...options?.headers,
    },
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({
      title: "Unknown error",
      detail: response.statusText,
    })) as ApiError;

    throw new Error(error.detail || error.title || "API request failed");
  }

  return response.json();
}

export function buildQueryString(params: Record<string, string | number | undefined>): string {
  const searchParams = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined) {
      searchParams.append(key, String(value));
    }
  });
  const queryString = searchParams.toString();
  return queryString ? `?${queryString}` : "";
}
```

**lib/api.ts:**

```typescript
// lib/api.ts
import { fetchApi, buildQueryString } from "./api-helpers";
import {
  DownloadRequest,
  DownloadSummary,
  DownloadDetails,
  VideoMetadata,
  CreateDownloadResponse,
  GetDownloadsResponse,
} from "@/types";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

export const api = {
  downloads: {
    /**
     * Criar novo download
     * POST /api/downloads
     */
    create: async (request: DownloadRequest): Promise<string> => {
      const response = await fetchApi<CreateDownloadResponse>(
        "/api/downloads",
        {
          method: "POST",
          body: JSON.stringify(request),
        }
      );
      return response.downloadId;
    },

    /**
     * Listar todos downloads
     * GET /api/downloads?status=downloading&limit=10&offset=0
     */
    list: async (params?: {
      status?: string;
      limit?: number;
      offset?: number;
    }): Promise<GetDownloadsResponse> => {
      const queryString = buildQueryString(params || {});
      return fetchApi<GetDownloadsResponse>(`/api/downloads${queryString}`);
    },

    /**
     * Obter detalhes de download específico
     * GET /api/downloads/{id}
     */
    get: async (id: string): Promise<DownloadDetails> => {
      return fetchApi<DownloadDetails>(`/api/downloads/${id}`);
    },

    /**
     * Cancelar/Deletar download
     * DELETE /api/downloads/{id}
     */
    cancel: async (id: string): Promise<void> => {
      await fetchApi<void>(`/api/downloads/${id}`, {
        method: "DELETE",
      });
    },
  },

  videos: {
    /**
     * Obter metadados de vídeo
     * GET /api/videos/info?url={url}
     */
    getInfo: async (url: string): Promise<VideoMetadata> => {
      const encodedUrl = encodeURIComponent(url);
      return fetchApi<VideoMetadata>(`/api/videos/info?url=${encodedUrl}`);
    },
  },

  health: {
    /**
     * Health check simples
     * GET /health
     */
    check: async (): Promise<{ status: string }> => {
      return fetchApi<{ status: string }>("/health");
    },
  },
};
```

**Checklist:**
- [ ] Criar lib/api-helpers.ts com fetchApi e buildQueryString
- [ ] Criar lib/api.ts com todos endpoints REST
- [ ] Configurar NEXT_PUBLIC_API_URL no .env.local
- [ ] Error handling adequado
- [ ] TypeScript types corretos
- [ ] Testar com API real

**Critérios de aceito:**
- ✅ API client funcional
- ✅ Todos endpoints implementados
- ✅ Error handling OK
- ✅ TypeScript sem erros

---

### 2.4.5 Criar SignalR client (WebSocket)

**Estimativa:** 4 horas
**Arquivos:**
```
lib/
  └── websocket.ts                - SignalR WebSocket client

hooks/
  ├── useWebSocket.ts             - Hook para conexão WebSocket
  └── useDownloadProgress.ts      - Hook para progresso de download
```

**lib/websocket.ts:**

```typescript
// lib/websocket.ts
import * as signalR from "@microsoft/signalr";
import type {
  DownloadProgressEvent,
  DownloadCompletedEvent,
  DownloadFailedEvent,
} from "@/types";

type ProgressListener = (data: DownloadProgressEvent) => void;
type CompletedListener = (data: DownloadCompletedEvent) => void;
type FailedListener = (data: DownloadFailedEvent) => void;

class WebSocketService {
  private connection: signalR.HubConnection | null = null;
  private isConnecting = false;
  private reconnectAttempts = 0;
  private readonly maxReconnectAttempts = 5;

  // Listeners por downloadId
  private progressListeners: Map<string, Set<ProgressListener>> = new Map();
  private completedListeners: Map<string, Set<CompletedListener>> = new Map();
  private failedListeners: Map<string, Set<FailedListener>> = new Map();

  /**
   * Conectar ao SignalR Hub
   */
  async connect(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      console.log("[WebSocket] Already connected");
      return;
    }

    if (this.isConnecting) {
      console.log("[WebSocket] Connection already in progress");
      return;
    }

    this.isConnecting = true;
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
    const hubUrl = apiUrl.replace("http", "ws") + "/hubs/downloads";

    console.log("[WebSocket] Connecting to:", hubUrl);

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents,
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000]) // Delays: 0s, 2s, 10s, 30s
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Event handlers
    this.connection.on("DownloadProgress", (downloadId: string, data: DownloadProgressEvent) => {
      console.log("[WebSocket] DownloadProgress:", downloadId, data.progress);
      this.emitProgress(downloadId, data);
    });

    this.connection.on("DownloadCompleted", (downloadId: string, data: DownloadCompletedEvent) => {
      console.log("[WebSocket] DownloadCompleted:", downloadId);
      this.emitCompleted(downloadId, data);
    });

    this.connection.on("DownloadFailed", (downloadId: string, data: DownloadFailedEvent) => {
      console.log("[WebSocket] DownloadFailed:", downloadId, data.error);
      this.emitFailed(downloadId, data);
    });

    this.connection.onreconnecting((error) => {
      console.log("[WebSocket] Reconnecting...", error);
      this.reconnectAttempts++;
    });

    this.connection.onreconnected((connectionId) => {
      console.log("[WebSocket] Reconnected:", connectionId);
      this.reconnectAttempts = 0;
    });

    this.connection.onclose((error) => {
      console.log("[WebSocket] Connection closed", error);
      this.isConnecting = false;
    });

    try {
      await this.connection.start();
      console.log("[WebSocket] Connected successfully");
      this.isConnecting = false;
      this.reconnectAttempts = 0;
    } catch (error) {
      console.error("[WebSocket] Connection failed:", error);
      this.isConnecting = false;
      throw error;
    }
  }

  /**
   * Entrar no grupo de um download (para receber updates)
   */
  async joinDownload(downloadId: string): Promise<void> {
    if (!this.connection) {
      await this.connect();
    }

    try {
      await this.connection!.invoke("JoinDownloadGroup", downloadId);
      console.log("[WebSocket] Joined download group:", downloadId);
    } catch (error) {
      console.error("[WebSocket] Failed to join download group:", error);
      throw error;
    }
  }

  /**
   * Sair do grupo de um download
   */
  async leaveDownload(downloadId: string): Promise<void> {
    if (!this.connection) return;

    try {
      await this.connection.invoke("LeaveDownloadGroup", downloadId);
      console.log("[WebSocket] Left download group:", downloadId);

      // Remover listeners
      this.progressListeners.delete(downloadId);
      this.completedListeners.delete(downloadId);
      this.failedListeners.delete(downloadId);
    } catch (error) {
      console.error("[WebSocket] Failed to leave download group:", error);
    }
  }

  /**
   * Registrar listener para progress updates
   */
  onProgress(downloadId: string, listener: ProgressListener): () => void {
    if (!this.progressListeners.has(downloadId)) {
      this.progressListeners.set(downloadId, new Set());
    }
    this.progressListeners.get(downloadId)!.add(listener);

    // Retornar função para remover listener
    return () => {
      this.offProgress(downloadId, listener);
    };
  }

  /**
   * Remover listener de progress
   */
  offProgress(downloadId: string, listener: ProgressListener): void {
    const listeners = this.progressListeners.get(downloadId);
    if (listeners) {
      listeners.delete(listener);
      if (listeners.size === 0) {
        this.progressListeners.delete(downloadId);
      }
    }
  }

  /**
   * Registrar listener para completion
   */
  onCompleted(downloadId: string, listener: CompletedListener): () => void {
    if (!this.completedListeners.has(downloadId)) {
      this.completedListeners.set(downloadId, new Set());
    }
    this.completedListeners.get(downloadId)!.add(listener);

    return () => {
      this.offCompleted(downloadId, listener);
    };
  }

  offCompleted(downloadId: string, listener: CompletedListener): void {
    const listeners = this.completedListeners.get(downloadId);
    if (listeners) {
      listeners.delete(listener);
      if (listeners.size === 0) {
        this.completedListeners.delete(downloadId);
      }
    }
  }

  /**
   * Registrar listener para failure
   */
  onFailed(downloadId: string, listener: FailedListener): () => void {
    if (!this.failedListeners.has(downloadId)) {
      this.failedListeners.set(downloadId, new Set());
    }
    this.failedListeners.get(downloadId)!.add(listener);

    return () => {
      this.offFailed(downloadId, listener);
    };
  }

  offFailed(downloadId: string, listener: FailedListener): void {
    const listeners = this.failedListeners.get(downloadId);
    if (listeners) {
      listeners.delete(listener);
      if (listeners.size === 0) {
        this.failedListeners.delete(downloadId);
      }
    }
  }

  /**
   * Emitir progress events para listeners registrados
   */
  private emitProgress(downloadId: string, data: DownloadProgressEvent): void {
    const listeners = this.progressListeners.get(downloadId);
    if (listeners) {
      listeners.forEach((listener) => listener(data));
    }
  }

  private emitCompleted(downloadId: string, data: DownloadCompletedEvent): void {
    const listeners = this.completedListeners.get(downloadId);
    if (listeners) {
      listeners.forEach((listener) => listener(data));
    }
  }

  private emitFailed(downloadId: string, data: DownloadFailedEvent): void {
    const listeners = this.failedListeners.get(downloadId);
    if (listeners) {
      listeners.forEach((listener) => listener(data));
    }
  }

  /**
   * Desconectar do hub
   */
  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      console.log("[WebSocket] Disconnected");
    }
  }

  /**
   * Verificar estado da conexão
   */
  isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }
}

// Singleton instance
export const ws = new WebSocketService();
```

**hooks/useDownloadProgress.ts:**

```typescript
// hooks/useDownloadProgress.ts
import { useState, useEffect } from "react";
import { ws } from "@/lib/websocket";
import type { DownloadProgressEvent, DownloadCompletedEvent, DownloadFailedEvent } from "@/types";

interface UseDownloadProgressResult {
  progress: DownloadProgressEvent | null;
  isCompleted: boolean;
  isFailed: boolean;
  error: string | null;
  completedData: DownloadCompletedEvent | null;
}

export function useDownloadProgress(downloadId: string): UseDownloadProgressResult {
  const [progress, setProgress] = useState<DownloadProgressEvent | null>(null);
  const [isCompleted, setIsCompleted] = useState(false);
  const [isFailed, setIsFailed] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [completedData, setCompletedData] = useState<DownloadCompletedEvent | null>(null);

  useEffect(() => {
    let mounted = true;

    // Conectar WebSocket
    ws.connect().catch((err) => {
      console.error("Failed to connect WebSocket:", err);
    });

    // Entrar no grupo do download
    ws.joinDownload(downloadId).catch((err) => {
      console.error("Failed to join download group:", err);
    });

    // Registrar listeners
    const unsubscribeProgress = ws.onProgress(downloadId, (data) => {
      if (mounted) {
        setProgress(data);
      }
    });

    const unsubscribeCompleted = ws.onCompleted(downloadId, (data) => {
      if (mounted) {
        setIsCompleted(true);
        setCompletedData(data);
        setProgress((prev) => ({
          ...prev!,
          progress: 100,
          status: "completed",
        }));
      }
    });

    const unsubscribeFailed = ws.onFailed(downloadId, (data) => {
      if (mounted) {
        setIsFailed(true);
        setError(data.error);
      }
    });

    // Cleanup
    return () => {
      mounted = false;
      unsubscribeProgress();
      unsubscribeCompleted();
      unsubscribeFailed();
      ws.leaveDownload(downloadId);
    };
  }, [downloadId]);

  return {
    progress,
    isCompleted,
    isFailed,
    error,
    completedData,
  };
}
```

**Checklist:**
- [ ] Instalar @microsoft/signalr
- [ ] Criar WebSocketService em lib/websocket.ts
- [ ] Implementar connect/disconnect
- [ ] Implementar joinDownload/leaveDownload
- [ ] Implementar listeners (onProgress, onCompleted, onFailed)
- [ ] Criar hook useDownloadProgress
- [ ] Auto-reconnect configurado
- [ ] Testar com API real

**Critérios de aceito:**
- ✅ WebSocket conecta com sucesso
- ✅ Events recebidos em tempo real
- ✅ Auto-reconnect funcionando
- ✅ Hook useDownloadProgress funciona

---

### 2.4.6 Criar página: Dashboard

**Estimativa:** 6 horas
**Arquivos:**
```
app/
  ├── page.tsx                      - Dashboard principal
  ├── layout.tsx                    - Root layout
  └── globals.css                   - Estilos globais

components/
  ├── DownloadForm.tsx              - Form para criar download
  ├── DownloadsList.tsx             - Lista de downloads
  ├── DownloadCard.tsx              - Card de download individual
  ├── ProgressBar.tsx               - Barra de progresso
  └── StatusBadge.tsx               - Badge de status
```

**app/page.tsx (Dashboard):**

```typescript
// app/page.tsx
"use client";

import { useState, useEffect } from "react";
import { api } from "@/lib/api";
import type { DownloadSummary } from "@/types";
import { DownloadForm } from "@/components/DownloadForm";
import { DownloadsList } from "@/components/DownloadsList";
import { Button } from "@/components/ui/button";
import { RefreshCw } from "lucide-react";

export default function DashboardPage() {
  const [downloads, setDownloads] = useState<DownloadSummary[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchDownloads = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await api.downloads.list();
      setDownloads(response.downloads);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to fetch downloads");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchDownloads();

    // Poll a cada 5 segundos (até implementarmos SignalR para lista)
    const interval = setInterval(fetchDownloads, 5000);
    return () => clearInterval(interval);
  }, []);

  const handleDownloadCreated = (downloadId: string) => {
    // Refetch para incluir novo download
    fetchDownloads();
  };

  const handleDownloadDeleted = (downloadId: string) => {
    setDownloads((prev) => prev.filter((d) => d.downloadId !== downloadId));
  };

  return (
    <div className="min-h-screen bg-background">
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="flex items-center justify-between mb-8">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Cutube</h1>
            <p className="text-muted-foreground">
              Gerenciador de downloads de vídeos
            </p>
          </div>
          <Button
            onClick={fetchDownloads}
            disabled={isLoading}
            variant="outline"
            size="icon"
          >
            <RefreshCw className={`h-4 w-4 ${isLoading ? "animate-spin" : ""}`} />
          </Button>
        </div>

        {/* Download Form */}
        <div className="mb-8">
          <DownloadForm onDownloadCreated={handleDownloadCreated} />
        </div>

        {/* Downloads List */}
        <div>
          <h2 className="text-xl font-semibold mb-4">Downloads</h2>
          {error && (
            <div className="p-4 mb-4 text-sm text-destructive bg-destructive/10 rounded-md">
              {error}
            </div>
          )}
          <DownloadsList
            downloads={downloads}
            isLoading={isLoading}
            onDownloadDeleted={handleDownloadDeleted}
          />
        </div>
      </div>
    </div>
  );
}
```

**components/DownloadForm.tsx:**

```typescript
// components/DownloadForm.tsx
"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { api } from "@/lib/api";
import { Loader2, Download } from "lucide-react";
import type { DownloadRequest } from "@/types";

const downloadSchema = z.object({
  url: z.string().url("URL inválida"),
  outputPath: z.string().optional(),
  startTime: z.string().optional(),
  endTime: z.string().optional(),
  audioOnly: z.boolean().default(false),
  customFilename: z.string().optional(),
});

type DownloadFormData = z.infer<typeof downloadSchema>;

interface DownloadFormProps {
  onDownloadCreated: (downloadId: string) => void;
}

export function DownloadForm({ onDownloadCreated }: DownloadFormProps) {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<DownloadFormData>({
    resolver: zodResolver(downloadSchema),
    defaultValues: {
      audioOnly: false,
    },
  });

  const onSubmit = async (data: DownloadFormData) => {
    setIsLoading(true);
    setError(null);

    try {
      const request: DownloadRequest = {
        url: data.url,
        outputPath: data.outputPath || undefined,
        startTime: data.startTime || undefined,
        endTime: data.endTime || undefined,
        audioOnly: data.audioOnly,
        customFilename: data.customFilename || undefined,
      };

      const downloadId = await api.downloads.create(request);
      onDownloadCreated(downloadId);
      reset();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create download");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="bg-card rounded-lg border p-6">
      <h3 className="text-lg font-semibold mb-4">Novo Download</h3>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        {/* URL */}
        <div className="space-y-2">
          <Label htmlFor="url">URL do vídeo *</Label>
          <Input
            id="url"
            placeholder="https://youtube.com/watch?v=..."
            {...register("url")}
            disabled={isLoading}
          />
          {errors.url && (
            <p className="text-sm text-destructive">{errors.url.message}</p>
          )}
        </div>

        {/* Audio Only */}
        <div className="flex items-center space-x-2">
          <Switch
            id="audioOnly"
            {...register("audioOnly")}
            disabled={isLoading}
          />
          <Label htmlFor="audioOnly">Apenas áudio (MP3)</Label>
        </div>

        {/* Time Range */}
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="startTime">Início (HH:MM:SS)</Label>
            <Input
              id="startTime"
              placeholder="00:00:00"
              {...register("startTime")}
              disabled={isLoading}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="endTime">Fim (HH:MM:SS)</Label>
            <Input
              id="endTime"
              placeholder="00:00:00"
              {...register("endTime")}
              disabled={isLoading}
            />
          </div>
        </div>

        {/* Output Path */}
        <div className="space-y-2">
          <Label htmlFor="outputPath">Caminho de saída</Label>
          <Input
            id="outputPath"
            placeholder="/home/user/Downloads"
            {...register("outputPath")}
            disabled={isLoading}
          />
        </div>

        {/* Custom Filename */}
        <div className="space-y-2">
          <Label htmlFor="customFilename">Nome do arquivo</Label>
          <Input
            id="customFilename"
            placeholder="meu-video.mp4"
            {...register("customFilename")}
            disabled={isLoading}
          />
        </div>

        {/* Error */}
        {error && (
          <p className="text-sm text-destructive">{error}</p>
        )}

        {/* Submit */}
        <Button type="submit" disabled={isLoading} className="w-full">
          {isLoading ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Criando download...
            </>
          ) : (
            <>
              <Download className="mr-2 h-4 w-4" />
              Iniciar Download
            </>
          )}
        </Button>
      </form>
    </div>
  );
}
```

**components/DownloadCard.tsx:**

```typescript
// components/DownloadCard.tsx
"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ProgressBar } from "@/components/ProgressBar";
import { StatusBadge } from "@/components/StatusBadge";
import { useDownloadProgress } from "@/hooks/useDownloadProgress";
import { api } from "@/lib/api";
import type { DownloadSummary } from "@/types";
import { Trash2, Download } from "lucide-react";
import { formatBytes, formatSpeed, formatTimeRange } from "@/lib/utils";

interface DownloadCardProps {
  download: DownloadSummary;
  onDelete: (downloadId: string) => void;
}

export function DownloadCard({ download, onDelete }: DownloadCardProps) {
  const { progress, isCompleted, isFailed, error } = useDownloadProgress(
    download.downloadId
  );

  const handleCancel = async () => {
    try {
      await api.downloads.cancel(download.downloadId);
      onDelete(download.downloadId);
    } catch (err) {
      console.error("Failed to cancel download:", err);
    }
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-start justify-between">
          <CardTitle className="text-base truncate flex-1">
            {download.url}
          </CardTitle>
          <StatusBadge status={download.status} />
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Progress Bar */}
        <ProgressBar
          progress={progress?.progress ?? download.progress}
          status={download.status}
        />

        {/* Details */}
        <div className="grid grid-cols-2 gap-2 text-sm text-muted-foreground">
          {progress?.speed && (
            <div>Velocidade: {formatSpeed(progress.speed)}</div>
          )}
          {progress?.eta && download.status === "downloading" && (
            <div>ETA: {formatTimeRange(progress.eta)}</div>
          )}
          {progress?.downloadedBytes && progress.totalBytes && (
            <div>
              {formatBytes(progress.downloadedBytes)} /{" "}
              {formatBytes(progress.totalBytes)}
            </div>
          )}
        </div>

        {/* Error Message */}
        {isFailed && error && (
          <div className="p-2 text-sm text-destructive bg-destructive/10 rounded-md">
            {error}
          </div>
        )}

        {/* Actions */}
        <div className="flex justify-end">
          {download.status !== "completed" && download.status !== "failed" && (
            <Button
              onClick={handleCancel}
              variant="destructive"
              size="sm"
            >
              <Trash2 className="mr-2 h-4 w-4" />
              Cancelar
            </Button>
          )}
          {download.status === "completed" && download.filePath && (
            <Button variant="outline" size="sm" asChild>
              <a href={`/api/downloads/${download.downloadId}/file`}>
                <Download className="mr-2 h-4 w-4" />
                Baixar Arquivo
              </a>
            </Button>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
```

**components/ProgressBar.tsx:**

```typescript
// components/ProgressBar.tsx
import { Progress } from "@/components/ui/progress";
import { cn } from "@/lib/utils";

interface ProgressBarProps {
  progress: number;
  status: string;
}

export function ProgressBar({ progress, status }: ProgressBarProps) {
  const isProcessing = status === "processing";
  const isCompleted = status === "completed";
  const isFailed = status === "failed";

  return (
    <div className="space-y-2">
      <div className="flex justify-between text-sm">
        <span>Progresso</span>
        <span>{Math.round(progress)}%</span>
      </div>
      <Progress
        value={isFailed ? 0 : progress}
        className={cn(
          "h-2",
          isProcessing && "animate-pulse",
          isCompleted && "bg-primary"
        )}
      />
      {isProcessing && (
        <p className="text-xs text-muted-foreground">
          Processando vídeo (ffmpeg)...
        </p>
      )}
    </div>
  );
}
```

**components/StatusBadge.tsx:**

```typescript
// components/StatusBadge.tsx
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { DownloadStatus } from "@/types";

interface StatusBadgeProps {
  status: DownloadStatus;
}

const statusConfig: Record<
  DownloadStatus,
  { label: string; className: string }
> = {
  queued: { label: "Na fila", className: "bg-secondary text-secondary-foreground" },
  downloading: {
    label: "Baixando",
    className: "bg-blue-500 text-white",
  },
  processing: {
    label: "Processando",
    className: "bg-yellow-500 text-white",
  },
  completed: {
    label: "Concluído",
    className: "bg-green-500 text-white",
  },
  failed: {
    label: "Falhou",
    className: "bg-destructive text-destructive-foreground",
  },
  cancelled: {
    label: "Cancelado",
    className: "bg-muted text-muted-foreground",
  },
};

export function StatusBadge({ status }: StatusBadgeProps) {
  const config = statusConfig[status];

  return (
    <Badge className={cn("font-normal", config.className)} variant="secondary">
      {config.label}
    </Badge>
  );
}
```

**components/DownloadsList.tsx:**

```typescript
// components/DownloadsList.tsx
import { DownloadCard } from "@/components/DownloadCard";
import type { DownloadSummary } from "@/types";

interface DownloadsListProps {
  downloads: DownloadSummary[];
  isLoading: boolean;
  onDownloadDeleted: (downloadId: string) => void;
}

export function DownloadsList({
  downloads,
  isLoading,
  onDownloadDeleted,
}: DownloadsListProps) {
  if (isLoading) {
    return (
      <div className="text-center py-8 text-muted-foreground">
        Carregando downloads...
      </div>
    );
  }

  if (downloads.length === 0) {
    return (
      <div className="text-center py-8 text-muted-foreground">
        Nenhum download ainda. Crie o primeiro acima!
      </div>
    );
  }

  return (
    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
      {downloads.map((download) => (
        <DownloadCard
          key={download.downloadId}
          download={download}
          onDelete={onDownloadDeleted}
        />
      ))}
    </div>
  );
}
```

**Checklist:**
- [ ] Criar app/page.tsx (Dashboard)
- [ ] Criar DownloadForm com validações
- [ ] Criar DownloadCard com progresso
- [ ] Criar ProgressBar animada
- [ ] Criar StatusBadge colorida
- [ ] Criar DownloadsList
- [ ] Conectar API REST (criar download)
- [ ] Conectar SignalR (progresso em tempo real)
- [ ] Testar fluxo completo

**Critérios de aceito:**
- ✅ Dashboard funcional
- [ ] Formulário de download funcionando
- [ ] Downloads listados
- [ ] Progresso em tempo real via WebSocket
- [ ] Cancelamento funcionando
- [ ] Design responsivo

---

### 2.4.7 Criar página: Downloads (Detalhes)

**Estimativa:** 4 horas
**Arquivos:**
```
app/downloads/
  ├── page.tsx                      - Lista de todos downloads
  └── [id]/
      └── page.tsx                  - Detalhes de download específico
```

**app/downloads/page.tsx:**

```typescript
// app/downloads/page.tsx
"use client";

import { useState, useEffect } from "react";
import { api } from "@/lib/api";
import type { DownloadSummary } from "@/types";
import { DownloadsList } from "@/components/DownloadsList";
import { Button } from "@/components/ui/button";
import { ArrowLeft, RefreshCw } from "lucide-react";
import Link from "next/link";

export default function DownloadsPage() {
  const [downloads, setDownloads] = useState<DownloadSummary[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const fetchDownloads = async () => {
    setIsLoading(true);
    try {
      const response = await api.downloads.list();
      setDownloads(response.downloads);
    } catch (err) {
      console.error("Failed to fetch downloads:", err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchDownloads();
  }, []);

  return (
    <div className="min-h-screen bg-background">
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="flex items-center justify-between mb-8">
          <div className="flex items-center space-x-4">
            <Link href="/">
              <Button variant="ghost" size="icon">
                <ArrowLeft className="h-4 w-4" />
              </Button>
            </Link>
            <div>
              <h1 className="text-3xl font-bold tracking-tight">Downloads</h1>
              <p className="text-muted-foreground">
                Histórico completo de downloads
              </p>
            </div>
          </div>
          <Button
            onClick={fetchDownloads}
            disabled={isLoading}
            variant="outline"
            size="icon"
          >
            <RefreshCw className={`h-4 w-4 ${isLoading ? "animate-spin" : ""}`} />
          </Button>
        </div>

        {/* Downloads List */}
        <DownloadsList
          downloads={downloads}
          isLoading={isLoading}
          onDownloadDeleted={(id) => setDownloads((prev) => prev.filter((d) => d.downloadId !== id))}
        />
      </div>
    </div>
  );
}
```

**app/downloads/[id]/page.tsx:**

```typescript
// app/downloads/[id]/page.tsx
"use client";

import { useState, useEffect } from "react";
import { useParams } from "next/navigation";
import { api } from "@/lib/api";
import type { DownloadDetails } from "@/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ProgressBar } from "@/components/ProgressBar";
import { StatusBadge } from "@/components/StatusBadge";
import { ArrowLeft, Trash2, Download, Calendar, Clock, HardDrive } from "lucide-react";
import Link from "next/link";
import { formatBytes, formatSpeed, formatTimeRange } from "@/lib/utils";
import { format } from "date-fns";
import { ptBR } from "date-fns/locale";

export default function DownloadDetailsPage() {
  const params = useParams();
  const downloadId = params.id as string;

  const [download, setDownload] = useState<DownloadDetails | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchDownload = async () => {
      setIsLoading(true);
      try {
        const details = await api.downloads.get(downloadId);
        setDownload(details);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to fetch download");
      } finally {
        setIsLoading(false);
      }
    };

    fetchDownload();

    // Poll a cada 2s para atualizar detalhes
    const interval = setInterval(fetchDownload, 2000);
    return () => clearInterval(interval);
  }, [downloadId]);

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <p>Carregando detalhes...</p>
      </div>
    );
  }

  if (error || !download) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <p className="text-destructive">{error || "Download não encontrado"}</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <Link href="/downloads">
            <Button variant="ghost" size="sm">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Voltar
            </Button>
          </Link>
        </div>

        <div className="grid gap-6 lg:grid-cols-3">
          {/* Main Details */}
          <div className="lg:col-span-2 space-y-6">
            {/* Status Card */}
            <Card>
              <CardHeader>
                <div className="flex items-start justify-between">
                  <CardTitle>Download</CardTitle>
                  <StatusBadge status={download.status} />
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <p className="text-sm font-medium mb-1">URL</p>
                  <p className="text-sm text-muted-foreground break-all">{download.url}</p>
                </div>

                <ProgressBar progress={download.progress} status={download.status} />

                {download.errorMessage && (
                  <div className="p-3 text-sm text-destructive bg-destructive/10 rounded-md">
                    {download.errorMessage}
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Actions */}
            <Card>
              <CardContent className="pt-6">
                <div className="flex space-x-2">
                  {download.status !== "completed" &&
                    download.status !== "failed" && (
                      <Button variant="destructive">
                        <Trash2 className="mr-2 h-4 w-4" />
                        Cancelar Download
                      </Button>
                    )}
                  {download.status === "completed" && download.filePath && (
                    <Button asChild>
                      <a href={`/api/downloads/${downloadId}/file`}>
                        <Download className="mr-2 h-4 w-4" />
                        Baixar Arquivo
                      </a>
                    </Button>
                  )}
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Info Sidebar */}
          <div className="space-y-6">
            {/* Dates */}
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Datas</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="flex items-start space-x-2">
                  <Calendar className="h-4 w-4 mt-0.5 text-muted-foreground" />
                  <div>
                    <p className="text-sm font-medium">Criado em</p>
                    <p className="text-xs text-muted-foreground">
                      {format(new Date(download.createdAt), "PPp", { locale: ptBR })}
                    </p>
                  </div>
                </div>
                {download.completedAt && (
                  <div className="flex items-start space-x-2">
                    <Clock className="h-4 w-4 mt-0.5 text-muted-foreground" />
                    <div>
                      <p className="text-sm font-medium">Concluído em</p>
                      <p className="text-xs text-muted-foreground">
                        {format(new Date(download.completedAt), "PPp", { locale: ptBR })}
                      </p>
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Stats */}
            {download.speed > 0 && (
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">Estatísticas</CardTitle>
                </CardHeader>
                <CardContent className="space-y-3">
                  {download.speed > 0 && (
                    <div className="flex items-start space-x-2">
                      <Download className="h-4 w-4 mt-0.5 text-muted-foreground" />
                      <div>
                        <p className="text-sm font-medium">Velocidade</p>
                        <p className="text-xs text-muted-foreground">
                          {formatSpeed(download.speed)}
                        </p>
                      </div>
                    </div>
                  )}
                  {download.eta && download.status === "downloading" && (
                    <div className="flex items-start space-x-2">
                      <Clock className="h-4 w-4 mt-0.5 text-muted-foreground" />
                      <div>
                        <p className="text-sm font-medium">Tempo Restante</p>
                        <p className="text-xs text-muted-foreground">
                          {formatTimeRange(download.eta)}
                        </p>
                      </div>
                    </div>
                  )}
                </CardContent>
              </Card>
            )}

            {/* File Info */}
            {download.status === "completed" && download.filePath && (
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">Arquivo</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="flex items-start space-x-2">
                    <HardDrive className="h-4 w-4 mt-0.5 text-muted-foreground" />
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium truncate">
                        {download.filePath.split("/").pop()}
                      </p>
                      <p className="text-xs text-muted-foreground truncate">
                        {download.filePath}
                      </p>
                    </div>
                  </div>
                </CardContent>
              </Card>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
```

**Checklist:**
- [ ] Criar página /downloads
- [ ] Criar página /downloads/[id]
- [ ] Listar todos downloads
- [ ] Mostrar detalhes específicos
- [ ] Implementar filtros (status, date)
- [ ] Implementar ações (cancel, retry)
- [ ] Navegação OK

**Critérios de aceito:**
- ✅ Páginas funcionais
- ✅ Detalhes completos mostrados
- ✅ Navegação entre páginas OK
- ✅ Design responsivo

---

### 2.4.8 Dark Mode

**Estimativa:** 3 horas
**Arquivos:**
```
components/
  ├── theme-provider.tsx            - Provider de tema
  └── theme-toggle.tsx              - Botão toggle tema
```

**components/theme-provider.tsx:**

```typescript
// components/theme-provider.tsx
"use client";

import * as React from "react";
import { ThemeProvider as NextThemesProvider } from "next-themes";
import { type ThemeProviderProps } from "next-themes/dist/types";

export function ThemeProvider({ children, ...props }: ThemeProviderProps) {
  return <NextThemesProvider {...props}>{children}</NextThemesProvider>;
}
```

**components/theme-toggle.tsx:**

```typescript
// components/theme-toggle.tsx
"use client";

import * as React from "react";
import { Moon, Sun } from "lucide-react";
import { useTheme } from "next-themes";
import { Button } from "@/components/ui/button";

export function ThemeToggle() {
  const { setTheme, theme } = useTheme();
  const [mounted, setMounted] = React.useState(false);

  React.useEffect(() => {
    setMounted(true);
  }, []);

  if (!mounted) {
    return null;
  }

  return (
    <Button
      variant="ghost"
      size="icon"
      onClick={() => setTheme(theme === "dark" ? "light" : "dark")}
    >
      {theme === "dark" ? (
        <Sun className="h-5 w-5" />
      ) : (
        <Moon className="h-5 w-5" />
      )}
      <span className="sr-only">Toggle theme</span>
    </Button>
  );
}
```

**app/layout.tsx:**

```typescript
// app/layout.tsx
import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { ThemeProvider } from "@/components/theme-provider";
import { ThemeToggle } from "@/components/theme-toggle";

const inter = Inter({ subsets: ["latin"] });

export const metadata: Metadata = {
  title: "Cutube - Video Downloader",
  description: "Gerenciador de downloads de vídeos",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="pt-BR" suppressHydrationWarning>
      <body className={inter.className}>
        <ThemeProvider
          attribute="class"
          defaultTheme="system"
          enableSystem
          disableTransitionOnChange
        >
          <div className="fixed top-4 right-4 z-50">
            <ThemeToggle />
          </div>
          {children}
        </ThemeProvider>
      </body>
    </html>
  );
}
```

**Checklist:**
- [ ] Instalar next-themes
- [ ] Criar ThemeProvider
- [ ] Criar ThemeToggle button
- [ ] Integrar no root layout
- [ ] Testar em light/dark mode
- [ ] Verificar persistência de tema

**Critérios de aceito:**
- ✅ Dark mode funcionando
- ✅ Toggle button funcional
- ✅ Tema persiste entre sessões
- ✅ Transições suaves

---

### 2.4.9 Responsividade

**Estimativa:** 4 horas
**Checklist:**
- [ ] Mobile (320px+)
  - Cards empilhados verticalmente
  - Form com campos em coluna única
  - Botões full-width
- [ ] Tablet (768px+)
  - Cards em grid 2 colunas
  - Sidebar em detalhes colapsada
- [ ] Desktop (1024px+)
  - Cards em grid 3 colunas
  - Sidebar visível
  - Espaçamento adequado
- [ ] Testar em múltiplos devices
- [ ] Tailwind breakpoints configurados

**Critérios de aceito:**
- ✅ Responsivo em todos tamanhos
- ✅ Mobile-first approach
- ✅ Lighthouse score > 90

---

### 2.4.10 Testes E2E com Playwright

**Estimativa:** 6 horas
**Arquivos:**
```
e2e/
  ├── downloads.spec.ts             - Testes de downloads
  └── video-info.spec.ts            - Testes de metadados

playwright.config.ts
```

**Setup Playwright:**

```bash
npm install -D @playwright/test
npx playwright install
```

**playwright.config.ts:**

```typescript
import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: "html",
  use: {
    baseURL: "http://localhost:3000",
    trace: "on-first-retry",
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
    {
      name: "firefox",
      use: { ...devices["Desktop Firefox"] },
    },
    {
      name: "webkit",
      use: { ...devices["Desktop Safari"] },
    },
    {
      name: "Mobile Chrome",
      use: { ...devices["Pixel 5"] },
    },
  ],
  webServer: {
    command: "npm run dev",
    url: "http://localhost:3000",
    reuseExistingServer: !process.env.CI,
  },
});
```

**e2e/downloads.spec.ts:**

```typescript
// e2e/downloads.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Downloads", () => {
  test("should create download", async ({ page }) => {
    await page.goto("/");

    // Preencher form
    await page.fill('input[name="url"]', "https://youtube.com/watch?v=test");
    await page.click('button[type="submit"]');

    // Verificar que download foi criado
    await expect(page.locator("text=Baixando")).toBeVisible({ timeout: 5000 });
  });

  test("should show progress updates", async ({ page }) => {
    await page.goto("/");

    // Criar download
    await page.fill('input[name="url"]', "https://youtube.com/watch?v=test2");
    await page.click('button[type="submit"]');

    // Esperar progresso mudar
    await expect(page.locator("text=Progresso")).toBeVisible();
    const progressText = await page.locator("text=Progresso").textContent();
    expect(progressText).toContain("%");
  });

  test("should cancel download", async ({ page }) => {
    await page.goto("/");

    // Criar download
    await page.fill('input[name="url"]', "https://youtube.com/watch?v=test3");
    await page.click('button[type="submit"]');

    // Cancelar
    await page.click("button:has-text('Cancelar')");

    // Verificar que não está mais na lista
    await expect(page.locator(`text=test3`)).not.toBeVisible();
  });
});
```

**Checklist:**
- [ ] Configurar Playwright
- [ ] Criar testes para downloads
- [ ] Criar testes para metadados
- [ ] Testar fluxos principais
- [ ] Testar WebSocket (events)
- [ ] Testar responsividade (mobile/desktop)

**Critérios de aceito:**
- ✅ Testes passam em todos browsers
- ✅ Cobertura de fluxos principais
- ✅ Testes rápidos (< 30s)

---

## Qualidade Gates - Fase 2.4

**ANTES de passar para Fase 2.5, TODOS os itens abaixo devem ser concluídos:**

- [ ] **npm run build** - **ZERO errors**
- [ ] **npm run lint** - **ZERO warnings**
- [ ] **npm run test** - **100% pass** (Playwright E2E)
- [ ] Lighthouse score > 90 (Performance, Accessibility, Best Practices)
- [ ] Responsivo (mobile, tablet, desktop)
- [ ] Dark mode funcionando
- [ ] WebSocket (SignalR) conectado
- [ ] REST API client funcionando
- [ ] Type safety (TypeScript strict mode)
- [ ] Code review aprovado

---

## Cronograma Detalhado

| Tarefa | Estimativa | Dependencies | Blocker |
|--------|-----------|--------------|---------|
| 2.4.1 Criar projeto Next.js | 2h | Fase 2.3 | Não |
| 2.4.2 Setup shadcn/ui | 3h | 2.4.1 | Não |
| 2.4.3 Criar tipos TypeScript | 2h | 2.4.1 | Não |
| 2.4.4 Criar API client | 4h | 2.4.3 | **Sim** |
| 2.4.5 Criar SignalR client | 4h | 2.4.3 | **Sim** |
| 2.4.6 Criar Dashboard | 6h | 2.4.4, 2.4.5 | **Sim** |
| 2.4.7 Criar páginas Downloads | 4h | 2.4.6 | **Sim** |
| 2.4.8 Dark Mode | 3h | 2.4.6 | Não |
| 2.4.9 Responsividade | 4h | 2.4.6 | Não |
| 2.4.10 Testes E2E | 6h | 2.4.7 | **Sim** |

**Total:** 38 horas (7-10 dias)

---

## Tecnologias

- **Next.js 15** - React framework (App Router)
- **React 19** - UI library
- **TypeScript** - Type safety (strict mode)
- **TailwindCSS** - Styling
- **shadcn/ui** - UI components
- **@microsoft/signalr** - WebSocket client
- **next-themes** - Dark mode
- **date-fns** - Formatação de datas
- **lucide-react** - Icons
- **react-hook-form** - Form validation
- **zod** - Schema validation
- **Playwright** - E2E tests

---

## Próximos Passos

Após completar Fase 2.4:

1. ✅ **Fase 2.5**: Integração CLI ↔ API
   - CLI funcionar em modo local ou remoto
   - Config file para API URL
   - Testar ambos modos

2. 🎯 **Deploy em Staging**
   - Docker compose (API + Frontend)
   - Testar em produção-like environment
   - Performance tuning

3. 📝 **Documentação**
   - README do frontend
   - Como desenvolver
   - Troubleshooting

---

## Troubleshooting Comum

**Problema: WebSocket não conecta**

```
Error: WebSocket connection to 'ws://localhost:5000/hubs/downloads' failed
```

**Solução:**
1. Verificar se API está rodando (localhost:5000)
2. Verificar CORS configurado com AllowCredentials
3. Verificar NEXT_PUBLIC_API_URL no .env.local
4. Verificar firewall não bloqueando WebSockets

**Problema: TypeScript erros em shadcn/ui**

```
error TS2307: Cannot find module '@/components/ui/button'
```

**Solução:**
1. Verificar se components.json configurado corretamente
2. Verificar tsconfig.json com paths aliases
3. Rodar `npx shadcn@latest add` novamente

**Problema: Progresso não atualiza em tempo real**

```
Expected: Progress bar updates every second
Actual: Progress bar stuck at 0%
```

**Solução:**
1. Verificar se useDownloadProgress hook está sendo usado
2. Verificar se ws.joinDownload(downloadId) foi chamado
3. Verificar console para erros SignalR
4. Verificar se BackgroundWorker está enviando eventos

**Problema: Lighthouse score baixo**

```
Performance: 65
Accessibility: 80
```

**Solução:**
1. Otimizar imagens (usar next/image)
2. Lazy loading de componentes
3. Minificar CSS/JS (já vem por padrão)
4. Adicionar loading states
5. Melhorar contrast ratios (WCAG AA)
