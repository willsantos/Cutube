# Fase 2.4: Frontend Next.js + TypeScript

**Status:** 🎯 Planejamento
**Épico:** Cutube-2i6 (Épico 2: Arquitetura Híbrida CLI + API)
**Duração:** 7-10 dias
**Responsável:** Frontend Developer (full-stack)
**Prioridade:** 🔥 Alta
**Dependência:** ✅ Fase 2.3 completa (WebSocket + REST API)
**Design System:** 📐 [plans/design-system.md](./design-system.md)

---

## Objetivo

Criar interface web moderna usando **Next.js 16**, **React 19**, **TypeScript** e **TailwindCSS v4** que se comunica com a API REST e SignalR para gerenciar downloads de vídeos em tempo real.

**Princípios Fundamentais:**
- **Component-first** — componentes reutilizáveis e composáveis, nunca recriar o que já existe
- **Design System driven** — toda UI derivada do Design System documentado
- **Acessibilidade nativa** — WCAG 2.1 AA desde o dia 1
- **Type-safe** — TypeScript strict mode, sem `any`
- **Performance** — Server Components onde possível, Client Components só quando necessário

**Benefícios:**
- Interface visual intuitiva para downloads
- Visualização de progresso em tempo real via WebSocket
- Design responsivo (mobile, tablet, desktop)
- Dark mode com suporte a preferência do sistema
- UI moderna com componentes reutilizáveis baseados no Design System
- Type safety com TypeScript strict mode

---

## Tooling & Convenções

| Item | Valor |
|------|-------|
| **Package Manager** | `pnpm` (obrigatório, sem npm/yarn) |
| **Dev Server Port** | `4000` (configurado no `package.json`) |
| **Framework** | Next.js 16 (App Router) |
| **React** | React 19 |
| **Styling** | TailwindCSS v4 (CSS-first config) |
| **Components** | shadcn/ui + Design System próprio |
| **Icons** | Lucide React |
| **Forms** | React Hook Form + Zod |
| **State** | React Query (TanStack Query) |
| **WebSocket** | @microsoft/signalr |
| **Dark Mode** | next-themes |
| **Tests E2E** | Playwright |
| **Linter** | ESLint (flat config) |
| **Formatter** | Prettier |

---

## Arquitetura de Componentes

A arquitetura segue o princípio **Atomic Design** adaptado: componentes primitivos (UI), compostos (features), e layouts (pages). Todo componente deve ser reutilizável por padrão.

```
cutube-web/
├── app/                               # Next.js 16 App Router
│   ├── layout.tsx                     # Root layout (ThemeProvider, fonts)
│   ├── page.tsx                       # Dashboard principal
│   ├── downloads/
│   │   ├── page.tsx                   # Lista de downloads
│   │   └── [id]/
│   │       └── page.tsx               # Detalhes de download
│   └── globals.css                    # TailwindCSS v4 + design tokens
│
├── components/                        # Componentes reutilizáveis
│   ├── ui/                            # Primitivos (shadcn/ui customizados)
│   │   ├── button.tsx
│   │   ├── card.tsx
│   │   ├── input.tsx
│   │   ├── badge.tsx
│   │   ├── progress.tsx
│   │   ├── dialog.tsx
│   │   ├── toast.tsx
│   │   ├── switch.tsx
│   │   ├── select.tsx
│   │   ├── separator.tsx
│   │   ├── skeleton.tsx               # Loading placeholders
│   │   └── label.tsx
│   │
│   ├── layout/                        # Componentes de layout
│   │   ├── header.tsx                 # Header com nav, theme toggle
│   │   ├── page-container.tsx         # Container padrão de página
│   │   ├── section.tsx                # Seção com título e conteúdo
│   │   └── empty-state.tsx            # Estado vazio reutilizável
│   │
│   ├── feedback/                      # Componentes de feedback
│   │   ├── error-message.tsx          # Mensagem de erro inline
│   │   ├── loading-spinner.tsx        # Spinner animado
│   │   ├── loading-skeleton.tsx       # Skeleton para cards/listas
│   │   └── status-badge.tsx           # Badge de status (queued, downloading, etc)
│   │
│   ├── data-display/                  # Componentes de exibição de dados
│   │   ├── progress-bar.tsx           # Barra de progresso com animação
│   │   ├── stat-card.tsx              # Card de estatística (velocidade, ETA)
│   │   ├── info-row.tsx               # Linha label: value
│   │   └── date-display.tsx           # Exibição de data formatada
│   │
│   ├── downloads/                     # Feature: Downloads
│   │   ├── download-form.tsx          # Form para criar download
│   │   ├── download-card.tsx          # Card individual de download
│   │   ├── download-list.tsx          # Lista de download cards
│   │   ├── download-actions.tsx       # Ações (cancel, retry, download file)
│   │   └── download-detail.tsx        # Visão detalhada de um download
│   │
│   └── theme/                         # Theme system
│       ├── theme-provider.tsx         # next-themes provider
│       └── theme-toggle.tsx           # Botão toggle dark/light
│
├── hooks/                             # Custom React hooks
│   ├── use-downloads.ts               # CRUD de downloads (React Query)
│   ├── use-download-progress.ts       # Progresso via WebSocket
│   ├── use-video-metadata.ts          # Fetch de metadados de vídeo
│   └── use-websocket.ts              # Hook base para WebSocket
│
├── lib/                               # Utilitários e serviços
│   ├── api.ts                         # REST API client
│   ├── api-helpers.ts                 # fetch wrapper, error handling
│   ├── websocket.ts                   # SignalR WebSocket service
│   ├── utils.ts                       # cn(), formatBytes, formatSpeed, etc
│   └── constants.ts                   # URLs, timeouts, configs
│
├── types/                             # TypeScript types
│   ├── download.ts                    # Tipos de download
│   ├── video.ts                       # Tipos de vídeo
│   ├── api.ts                         # Tipos de resposta HTTP
│   └── index.ts                       # Barrel export
│
├── next.config.ts                     # Next.js 16 config
├── tailwind.config.ts                 # TailwindCSS (se necessário override)
├── tsconfig.json                      # TypeScript strict
├── components.json                    # shadcn/ui config
├── .env.local                         # Variáveis de ambiente
├── .env.example                       # Template de variáveis
└── package.json                       # Scripts com pnpm, porta 4000
```

### Princípio de Reutilização

```
Regra de 3: se um padrão aparece 3+ vezes, vira componente.

✅ CERTO:
  <StatusBadge status={download.status} />           # Reusado em Card, Detail, List
  <ProgressBar value={progress} status={status} />   # Reusado em Card, Detail
  <EmptyState icon={...} title="..." action={...} /> # Reusado em qualquer lista vazia
  <ErrorMessage message={error} />                    # Reusado em qualquer form/fetch
  <InfoRow label="Velocidade" value={speed} />        # Reusado em qualquer detalhe

❌ ERRADO:
  Copiar JSX de status badge direto no CardContent
  Criar ProgressBar diferente para cada página
  Duplicar loading states em cada componente
```

---

## Visão Arquitetural

```
┌─────────────────────────────────────────────────────────────┐
│                      Frontend Layer                          │
│                    Next.js 16 App Router                     │
│                    pnpm | porta :4000                        │
│                                                              │
│  /app/page.tsx                 - Dashboard principal          │
│  /app/downloads/page.tsx       - Lista de downloads          │
│  /app/downloads/[id]/page.tsx  - Detalhes de download        │
└────────────────────────┬─────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                    Components Layer                          │
│             Atomic Design (UI → Feature → Page)              │
│                                                              │
│  ui/          → Primitivos: Button, Card, Input, Badge       │
│  layout/      → PageContainer, Header, Section, EmptyState   │
│  feedback/    → ErrorMessage, StatusBadge, LoadingSkeleton    │
│  data-display/→ ProgressBar, StatCard, InfoRow, DateDisplay  │
│  downloads/   → DownloadForm, DownloadCard, DownloadList     │
└────────────────────────┬─────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                     Services Layer                           │
│                                                              │
│  lib/api.ts              - REST API client                   │
│  lib/websocket.ts        - SignalR client                    │
│  hooks/use-downloads.ts  - React Query (cache + mutations)   │
│  hooks/use-download-progress.ts - WebSocket progress hook    │
└────────────────────────┬─────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                      Backend Layer                           │
│              ASP.NET Core API (Fase 2.2/2.3)                 │
│                                                              │
│  /api/downloads         - REST endpoints                     │
│  /api/videos/info       - Video metadata                     │
│  /hubs/downloads        - SignalR Hub (WebSocket)            │
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

### 2.4.1 Criar projeto Next.js 16

**Estimativa:** 2 horas
**Comandos:**

```bash
# Criar projeto Next.js 16 com pnpm
pnpx create-next-app@latest cutube-web \
  --typescript \
  --tailwind \
  --app \
  --no-src-dir \
  --import-alias "@/*" \
  --eslint \
  --use-pnpm

cd cutube-web

# Dependências core
pnpm add @microsoft/signalr
pnpm add @tanstack/react-query
pnpm add date-fns
pnpm add lucide-react
pnpm add class-variance-authority
pnpm add clsx tailwind-merge
pnpm add next-themes
pnpm add react-hook-form @hookform/resolvers zod

# Dev dependencies
pnpm add -D prettier eslint-config-prettier

# Inicializar shadcn/ui
pnpx shadcn@latest init
```

**Configuração da porta 4000 (package.json):**

```json
{
  "scripts": {
    "dev": "next dev --port 4000",
    "build": "next build",
    "start": "next start --port 4000",
    "lint": "next lint",
    "format": "prettier --write .",
    "format:check": "prettier --check .",
    "type-check": "tsc --noEmit"
  }
}
```

**Configuração Next.js 16 (next.config.ts):**

```typescript
import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Next.js 16 usa React 19 por padrão
  reactStrictMode: true,

  // Proxy para API backend em dev
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: `${process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000"}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;
```

**Variáveis de ambiente (.env.local):**

```bash
# Backend API
NEXT_PUBLIC_API_URL=http://localhost:5000
```

**tsconfig.json:**

```json
{
  "compilerOptions": {
    "target": "ES2022",
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
    "plugins": [{ "name": "next" }],
    "paths": {
      "@/*": ["./*"]
    }
  },
  "include": ["next-env.d.ts", "**/*.ts", "**/*.tsx", ".next/types/**/*.ts"],
  "exclude": ["node_modules"]
}
```

**Checklist:**
- [ ] Criar projeto Next.js 16 com `pnpm`
- [ ] Configurar porta 4000 no `dev` e `start`
- [ ] Configurar TypeScript strict mode
- [ ] Configurar TailwindCSS v4 com design tokens
- [ ] Configurar path aliases (@/*)
- [ ] Adicionar ESLint + Prettier
- [ ] Adicionar todas as dependências
- [ ] Configurar proxy para API backend
- [ ] Criar `.env.example`
- [ ] Build sem erros: `pnpm build`
- [ ] Dev server rodando: `pnpm dev` (http://localhost:4000)

**Critérios de aceito:**
- ✅ Projeto criado com Next.js 16 + pnpm
- ✅ Dev server roda na porta 4000
- ✅ TypeScript sem erros
- ✅ TailwindCSS funcionando (classes aplicadas)
- ✅ App Router configurado
- ✅ Build OK

---

### 2.4.2 Setup Design System + shadcn/ui

**Estimativa:** 4 horas
**Referência:** [Design System](./design-system.md)

```bash
# Inicializar shadcn/ui
pnpx shadcn@latest init

# Adicionar componentes primitivos
pnpx shadcn@latest add button card input label form select badge progress toast switch dialog separator skeleton
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
    "baseColor": "neutral",
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

**globals.css — Design Tokens (ver [Design System](./design-system.md) para valores):**

Os design tokens CSS são definidos no `globals.css` seguindo o Design System. Incluem:
- Cores semânticas (background, foreground, primary, accent, destructive, etc.)
- Espaçamentos baseados em 8px grid
- Tipografia com escala 1.25 (Major Third)
- Radius, shadows, transitions
- Tokens para light e dark mode

**Criar componentes base do Design System:**

Todos os componentes abaixo seguem as diretrizes do [Design System](./design-system.md):

1. **layout/page-container.tsx** — Container padrão com max-width e padding
2. **layout/header.tsx** — Header com logo, nav e theme toggle
3. **layout/section.tsx** — Seção com título opcional
4. **layout/empty-state.tsx** — Estado vazio com ícone, título, descrição e ação
5. **feedback/error-message.tsx** — Mensagem de erro inline
6. **feedback/loading-spinner.tsx** — Spinner com tamanhos (sm, md, lg)
7. **feedback/loading-skeleton.tsx** — Skeleton para cards e listas
8. **feedback/status-badge.tsx** — Badge colorido para status de download
9. **data-display/progress-bar.tsx** — Barra de progresso animada
10. **data-display/stat-card.tsx** — Card com label, valor e ícone
11. **data-display/info-row.tsx** — Linha label: value
12. **data-display/date-display.tsx** — Data formatada com ícone

**lib/utils.ts:**

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
  return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + " " + sizes[i];
}

export function formatSpeed(bytesPerSecond: number): string {
  return formatBytes(bytesPerSecond) + "/s";
}

export function formatDuration(seconds: number): string {
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const s = Math.floor(seconds % 60);
  return `${h.toString().padStart(2, "0")}:${m.toString().padStart(2, "0")}:${s.toString().padStart(2, "0")}`;
}

export function formatTimeRange(timeString: string): string {
  const parts = timeString.split(":");
  if (parts.length !== 3) return timeString;
  const hours = parseInt(parts[0], 10);
  const minutes = parseInt(parts[1], 10);
  const seconds = parseInt(parts[2], 10);
  const result: string[] = [];
  if (hours > 0) result.push(`${hours}h`);
  if (minutes > 0) result.push(`${minutes}m`);
  if (seconds > 0) result.push(`${seconds}s`);
  return result.join(" ") || "0s";
}
```

**Checklist:**
- [ ] Inicializar shadcn/ui com components.json
- [ ] Adicionar todos componentes primitivos (button, card, input, etc.)
- [ ] Implementar design tokens no globals.css conforme Design System
- [ ] Criar componentes de layout (page-container, header, section, empty-state)
- [ ] Criar componentes de feedback (error-message, loading-spinner, loading-skeleton, status-badge)
- [ ] Criar componentes de data-display (progress-bar, stat-card, info-row, date-display)
- [ ] Criar lib/utils.ts com utilitários
- [ ] Verificar dark mode funcionando
- [ ] Testar todos componentes isoladamente

**Critérios de aceito:**
- ✅ shadcn/ui configurado com design tokens do Design System
- ✅ Componentes base criados e reutilizáveis
- ✅ Dark mode funcionando
- ✅ Todos componentes renderizam corretamente
- ✅ WCAG 2.1 AA: contraste mínimo 4.5:1

---

### 2.4.3 Criar tipos TypeScript

**Estimativa:** 2 horas
**Arquivos:**
```
types/
  ├── download.ts        - Tipos de download
  ├── video.ts           - Tipos de vídeo
  ├── api.ts             - Tipos gerais de API
  └── index.ts           - Barrel export
```

**types/download.ts:**

```typescript
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
  startTime?: string;       // "HH:MM:SS"
  endTime?: string;         // "HH:MM:SS"
  audioOnly?: boolean;
  customFilename?: string;
}

export interface DownloadProgress {
  downloadId: string;
  progress: number;          // 0-100
  speed: number;             // bytes/s
  eta?: string;              // "HH:MM:SS"
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
  createdAt: string;         // ISO date
  completedAt?: string;      // ISO date
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
  startedAt: string;
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
  duration: number;
  completedAt: string;
}

export interface DownloadFailedEvent {
  downloadId: string;
  error: string;
  failedAt: string;
}
```

**types/video.ts:**

```typescript
export interface VideoMetadata {
  id: string;
  title: string;
  uploader: string;
  duration: string;
  thumbnailUrl: string;
  viewCount?: number;
  uploadDate?: string;
  formats: VideoFormat[];
}

export interface VideoFormat {
  formatId: string;
  extension: string;
  resolution?: string;
  fileSize?: number;
}
```

**types/api.ts:**

```typescript
import type { DownloadSummary } from "./download";

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
- [ ] Criar types/index.ts (barrel export)
- [ ] TypeScript compila sem erros

**Critérios de aceito:**
- ✅ Todos tipos definidos
- ✅ TypeScript compila sem erros
- ✅ Tipos compatíveis com API backend

---

### 2.4.4 Criar API client (REST) + React Query

**Estimativa:** 4 horas
**Arquivos:**
```
lib/
  ├── api.ts              - REST API client
  ├── api-helpers.ts       - Helpers para fetch
  └── constants.ts         - Constantes (URLs, timeouts)

hooks/
  └── use-downloads.ts     - React Query hooks para downloads
```

O API client usa `fetch` nativo (sem axios) e React Query (TanStack Query) para cache e mutations.

**lib/constants.ts:**

```typescript
export const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

export const QUERY_KEYS = {
  downloads: ["downloads"] as const,
  download: (id: string) => ["downloads", id] as const,
  videoInfo: (url: string) => ["video-info", url] as const,
} as const;

export const POLL_INTERVAL = 5000; // 5s para lista de downloads
```

**lib/api-helpers.ts:**

```typescript
import { API_BASE_URL } from "./constants";
import type { ApiError } from "@/types/api";

export async function fetchApi<T>(
  endpoint: string,
  options?: RequestInit,
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
    const error = (await response.json().catch(() => ({
      title: "Unknown error",
      detail: response.statusText,
    }))) as ApiError;

    throw new Error(error.detail || error.title || "API request failed");
  }

  // 204 No Content
  if (response.status === 204) return undefined as T;

  return response.json();
}

export function buildQueryString(
  params: Record<string, string | number | undefined>,
): string {
  const searchParams = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined) {
      searchParams.append(key, String(value));
    }
  });
  const qs = searchParams.toString();
  return qs ? `?${qs}` : "";
}
```

**lib/api.ts:**

```typescript
import { fetchApi, buildQueryString } from "./api-helpers";
import type {
  DownloadRequest,
  DownloadDetails,
  VideoMetadata,
  CreateDownloadResponse,
  GetDownloadsResponse,
} from "@/types";

export const api = {
  downloads: {
    create: async (request: DownloadRequest): Promise<string> => {
      const response = await fetchApi<CreateDownloadResponse>(
        "/api/downloads",
        { method: "POST", body: JSON.stringify(request) },
      );
      return response.downloadId;
    },

    list: async (params?: {
      status?: string;
      limit?: number;
      offset?: number;
    }): Promise<GetDownloadsResponse> => {
      const qs = buildQueryString(params || {});
      return fetchApi<GetDownloadsResponse>(`/api/downloads${qs}`);
    },

    get: async (id: string): Promise<DownloadDetails> => {
      return fetchApi<DownloadDetails>(`/api/downloads/${id}`);
    },

    cancel: async (id: string): Promise<void> => {
      await fetchApi<void>(`/api/downloads/${id}`, { method: "DELETE" });
    },
  },

  videos: {
    getInfo: async (url: string): Promise<VideoMetadata> => {
      const encodedUrl = encodeURIComponent(url);
      return fetchApi<VideoMetadata>(`/api/videos/info?url=${encodedUrl}`);
    },
  },

  health: {
    check: async (): Promise<{ status: string }> => {
      return fetchApi<{ status: string }>("/health");
    },
  },
};
```

**hooks/use-downloads.ts (React Query):**

```typescript
"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { QUERY_KEYS, POLL_INTERVAL } from "@/lib/constants";
import type { DownloadRequest } from "@/types";

export function useDownloads(params?: {
  status?: string;
  limit?: number;
  offset?: number;
}) {
  return useQuery({
    queryKey: [...QUERY_KEYS.downloads, params],
    queryFn: () => api.downloads.list(params),
    refetchInterval: POLL_INTERVAL,
  });
}

export function useDownload(id: string) {
  return useQuery({
    queryKey: QUERY_KEYS.download(id),
    queryFn: () => api.downloads.get(id),
    refetchInterval: 2000,
  });
}

export function useCreateDownload() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: DownloadRequest) => api.downloads.create(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.downloads });
    },
  });
}

export function useCancelDownload() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => api.downloads.cancel(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.downloads });
    },
  });
}
```

**Checklist:**
- [ ] Criar lib/constants.ts com constantes
- [ ] Criar lib/api-helpers.ts com fetchApi e buildQueryString
- [ ] Criar lib/api.ts com todos endpoints REST
- [ ] Criar hooks/use-downloads.ts com React Query
- [ ] Configurar NEXT_PUBLIC_API_URL no .env.local
- [ ] Error handling adequado
- [ ] TypeScript types corretos

**Critérios de aceito:**
- ✅ API client funcional
- ✅ React Query configurado com polling
- ✅ Todos endpoints implementados
- ✅ Error handling OK
- ✅ TypeScript sem erros

---

### 2.4.5 Criar SignalR client (WebSocket)

**Estimativa:** 4 horas
**Arquivos:**
```
lib/
  └── websocket.ts          - SignalR WebSocket service

hooks/
  ├── use-websocket.ts       - Hook base para conexão
  └── use-download-progress.ts - Hook para progresso de download
```

**lib/websocket.ts:**

Implementação do `WebSocketService` como singleton com:
- Connect/disconnect com auto-reconnect (delays: 0s, 2s, 10s, 30s)
- Join/leave download groups
- Listeners tipados por downloadId (progress, completed, failed)
- Cleanup automático de listeners

**hooks/use-download-progress.ts:**

```typescript
"use client";

import { useState, useEffect } from "react";
import { ws } from "@/lib/websocket";
import type {
  DownloadProgressEvent,
  DownloadCompletedEvent,
} from "@/types";

interface UseDownloadProgressResult {
  progress: DownloadProgressEvent | null;
  isCompleted: boolean;
  isFailed: boolean;
  error: string | null;
  completedData: DownloadCompletedEvent | null;
}

export function useDownloadProgress(
  downloadId: string,
): UseDownloadProgressResult {
  const [progress, setProgress] = useState<DownloadProgressEvent | null>(null);
  const [isCompleted, setIsCompleted] = useState(false);
  const [isFailed, setIsFailed] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [completedData, setCompletedData] =
    useState<DownloadCompletedEvent | null>(null);

  useEffect(() => {
    let mounted = true;

    ws.connect().catch(console.error);
    ws.joinDownload(downloadId).catch(console.error);

    const unsubProgress = ws.onProgress(downloadId, (data) => {
      if (mounted) setProgress(data);
    });

    const unsubCompleted = ws.onCompleted(downloadId, (data) => {
      if (mounted) {
        setIsCompleted(true);
        setCompletedData(data);
        setProgress((prev) =>
          prev ? { ...prev, progress: 100, status: "completed" } : prev,
        );
      }
    });

    const unsubFailed = ws.onFailed(downloadId, (data) => {
      if (mounted) {
        setIsFailed(true);
        setError(data.error);
      }
    });

    return () => {
      mounted = false;
      unsubProgress();
      unsubCompleted();
      unsubFailed();
      ws.leaveDownload(downloadId);
    };
  }, [downloadId]);

  return { progress, isCompleted, isFailed, error, completedData };
}
```

**Checklist:**
- [ ] Criar WebSocketService em lib/websocket.ts
- [ ] Implementar connect/disconnect com auto-reconnect
- [ ] Implementar joinDownload/leaveDownload
- [ ] Implementar listeners tipados (onProgress, onCompleted, onFailed)
- [ ] Criar hook use-download-progress
- [ ] Cleanup automático no unmount

**Critérios de aceito:**
- ✅ WebSocket conecta com sucesso
- ✅ Events recebidos em tempo real
- ✅ Auto-reconnect funcionando
- ✅ Hook useDownloadProgress funcional
- ✅ Sem memory leaks (cleanup correto)

---

### 2.4.6 Criar páginas: Dashboard + Downloads

**Estimativa:** 8 horas

Esta tarefa implementa todas as páginas usando os **componentes reutilizáveis** criados em 2.4.2.

**app/layout.tsx (Root Layout):**

```tsx
import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { ThemeProvider } from "@/components/theme/theme-provider";
import { Header } from "@/components/layout/header";
import { QueryProvider } from "@/components/providers/query-provider";

const inter = Inter({ subsets: ["latin"], variable: "--font-inter" });

export const metadata: Metadata = {
  title: "Cutube — Video Downloader",
  description: "Gerenciador de downloads de vídeos",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="pt-BR" suppressHydrationWarning>
      <body className={`${inter.variable} font-sans antialiased`}>
        <ThemeProvider
          attribute="class"
          defaultTheme="system"
          enableSystem
          disableTransitionOnChange
        >
          <QueryProvider>
            <Header />
            <main>{children}</main>
          </QueryProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
```

**app/page.tsx (Dashboard):**

Composto de componentes reutilizáveis:
- `<PageContainer>` — wrapper padrão
- `<Section>` — seções com título
- `<DownloadForm>` — formulário de download
- `<DownloadList>` — lista de downloads (usa React Query via `useDownloads`)
- `<EmptyState>` — quando não há downloads
- `<ErrorMessage>` — quando API falha

**app/downloads/page.tsx (Lista):**

Reutiliza exatamente os mesmos componentes:
- `<PageContainer>` + `<DownloadList>` + `<EmptyState>`

**app/downloads/[id]/page.tsx (Detalhes):**

Composto de:
- `<PageContainer>`
- `<DownloadDetail>` (card principal com status, URL, progress)
- `<ProgressBar>` (reutilizado do design system)
- `<StatusBadge>` (reutilizado)
- `<StatCard>` (velocidade, ETA, tamanho)
- `<InfoRow>` (datas, caminhos)
- `<DownloadActions>` (cancelar, baixar arquivo)
- `<DateDisplay>` (criado em, concluído em)
- `<ErrorMessage>` (quando download falha)

**Checklist:**
- [ ] Criar app/layout.tsx com ThemeProvider + QueryProvider + Header
- [ ] Criar app/page.tsx (Dashboard) usando componentes reutilizáveis
- [ ] Criar app/downloads/page.tsx reutilizando DownloadList
- [ ] Criar app/downloads/[id]/page.tsx reutilizando componentes de display
- [ ] Conectar React Query (useDownloads, useDownload)
- [ ] Conectar WebSocket (useDownloadProgress)
- [ ] Loading states com Skeleton
- [ ] Error states com ErrorMessage
- [ ] Empty states com EmptyState
- [ ] Navegação entre páginas funcional

**Critérios de aceito:**
- ✅ Dashboard funcional com form + lista
- ✅ Downloads listados com progresso em tempo real
- ✅ Detalhes de download completos
- ✅ Cancelamento funcionando
- ✅ Todos os componentes são reutilizados (nenhum JSX duplicado)
- ✅ Loading/Error/Empty states em todas as páginas

---

### 2.4.7 Dark Mode + Theme System

**Estimativa:** 2 horas

**components/theme/theme-provider.tsx:**

```tsx
"use client";

import { ThemeProvider as NextThemesProvider } from "next-themes";

export function ThemeProvider({
  children,
  ...props
}: React.ComponentProps<typeof NextThemesProvider>) {
  return <NextThemesProvider {...props}>{children}</NextThemesProvider>;
}
```

**components/theme/theme-toggle.tsx:**

```tsx
"use client";

import { useTheme } from "next-themes";
import { Moon, Sun } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useEffect, useState } from "react";

export function ThemeToggle() {
  const { setTheme, theme } = useTheme();
  const [mounted, setMounted] = useState(false);

  useEffect(() => setMounted(true), []);

  if (!mounted) return <Button variant="ghost" size="icon" aria-label="Toggle theme" />;

  return (
    <Button
      variant="ghost"
      size="icon"
      onClick={() => setTheme(theme === "dark" ? "light" : "dark")}
      aria-label={`Switch to ${theme === "dark" ? "light" : "dark"} mode`}
    >
      {theme === "dark" ? (
        <Sun className="h-5 w-5" />
      ) : (
        <Moon className="h-5 w-5" />
      )}
    </Button>
  );
}
```

O Dark mode segue as diretrizes do [Design System](./design-system.md):
- Nunca `#000` puro — usar cinza escuro com tint da marca
- Nunca `#FFF` puro em texto — usar `~92%` lightness
- Reduzir saturação de cores accent em 10-20%
- Elevação = brilho (cards mais claros que background)

**Checklist:**
- [ ] Instalar next-themes
- [ ] Criar ThemeProvider
- [ ] Criar ThemeToggle com aria-label
- [ ] Integrar no root layout
- [ ] Tokens de dark mode no globals.css
- [ ] Testar em light e dark mode
- [ ] Verificar persistência de tema
- [ ] Verificar contraste WCAG AA em ambos modos

**Critérios de aceito:**
- ✅ Dark mode funcionando
- ✅ Toggle button acessível (aria-label, keyboard)
- ✅ Tema persiste entre sessões (localStorage)
- ✅ Respeita preferência do sistema
- ✅ Contraste WCAG 2.1 AA em ambos modos

---

### 2.4.8 Responsividade

**Estimativa:** 4 horas

**Breakpoints (TailwindCSS defaults):**

| Breakpoint | Min Width | Layout |
|------------|-----------|--------|
| `sm` | 640px | Mobile landscape |
| `md` | 768px | Tablet |
| `lg` | 1024px | Desktop |
| `xl` | 1280px | Wide desktop |

**Responsividade por componente:**

| Componente | Mobile (< 768px) | Tablet (768px+) | Desktop (1024px+) |
|------------|-------------------|-----------------|-------------------|
| Header | Logo + menu hamburguer | Logo + nav | Logo + nav + actions |
| DownloadForm | Campos empilhados | 2 colunas | 2 colunas com sidebar |
| DownloadList | 1 coluna | 2 colunas | 3 colunas |
| DownloadDetail | Empilhado | Empilhado | 2/3 content + 1/3 sidebar |
| StatCards | 2 colunas | 3 colunas | 4 colunas |

**Checklist:**
- [ ] Mobile-first CSS (base → sm → md → lg)
- [ ] DownloadList grid responsivo
- [ ] DownloadForm layout adaptativo
- [ ] DownloadDetail com sidebar colapsável
- [ ] Touch targets mínimo 44×44px em mobile
- [ ] Testar em DevTools (320px, 768px, 1024px, 1440px)
- [ ] Testar em dispositivos reais se possível

**Critérios de aceito:**
- ✅ Responsivo em todos tamanhos
- ✅ Mobile-first approach
- ✅ Touch targets adequados
- ✅ Nenhum scroll horizontal

---

### 2.4.9 Acessibilidade (a11y)

**Estimativa:** 3 horas

A acessibilidade é parte core do Design System. Esta tarefa garante conformidade:

**Requisitos WCAG 2.1 AA:**
- Contraste mínimo 4.5:1 para texto normal, 3:1 para texto grande
- Todos inputs com `<label>` associado
- Todos botões com texto visível ou `aria-label`
- Focus ring visível em todos elementos interativos
- Navegação por teclado funcional (Tab, Enter, Escape)
- Landmarks semânticos (`<header>`, `<main>`, `<nav>`, `<footer>`)
- `aria-live` regions para updates em tempo real (progresso de download)
- `prefers-reduced-motion` respeitado em animações

**Checklist:**
- [ ] Rodar audit de acessibilidade (Lighthouse, axe-core)
- [ ] Verificar contraste de todas combinações de cores
- [ ] Verificar labels em todos inputs
- [ ] Verificar aria-labels em botões icon-only
- [ ] Testar navegação por teclado completa
- [ ] Adicionar skip-to-content link
- [ ] `aria-live="polite"` para updates de progresso
- [ ] `prefers-reduced-motion` em animações CSS
- [ ] Testar com leitor de tela (NVDA/VoiceOver)

**Critérios de aceito:**
- ✅ Lighthouse Accessibility score > 95
- ✅ axe-core: zero violations
- ✅ Navegação por teclado completa
- ✅ Funcional com leitor de tela

---

### 2.4.10 Testes E2E com Playwright

**Estimativa:** 6 horas

```bash
pnpm add -D @playwright/test
pnpx playwright install
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
    baseURL: "http://localhost:4000",
    trace: "on-first-retry",
  },
  projects: [
    { name: "chromium", use: { ...devices["Desktop Chrome"] } },
    { name: "firefox", use: { ...devices["Desktop Firefox"] } },
    { name: "webkit", use: { ...devices["Desktop Safari"] } },
    { name: "Mobile Chrome", use: { ...devices["Pixel 5"] } },
  ],
  webServer: {
    command: "pnpm dev",
    url: "http://localhost:4000",
    reuseExistingServer: !process.env.CI,
  },
});
```

**Cenários de teste:**

```
e2e/
  ├── dashboard.spec.ts      - Dashboard load, form submit, lista
  ├── downloads.spec.ts      - CRUD de downloads, progresso
  ├── navigation.spec.ts     - Navegação entre páginas
  ├── dark-mode.spec.ts      - Toggle tema, persistência
  ├── responsive.spec.ts     - Layout em diferentes viewports
  └── accessibility.spec.ts  - a11y checks com axe-core
```

**Checklist:**
- [ ] Configurar Playwright com porta 4000
- [ ] Testes de dashboard (load, form, lista)
- [ ] Testes de downloads (criar, cancelar, detalhes)
- [ ] Testes de navegação
- [ ] Testes de dark mode
- [ ] Testes de responsividade (mobile, desktop)
- [ ] Testes de acessibilidade (@axe-core/playwright)

**Critérios de aceito:**
- ✅ Testes passam em todos browsers
- ✅ Cobertura de fluxos principais
- ✅ Testes de a11y passam
- ✅ Testes rápidos (< 60s total)

---

## Quality Gates — Fase 2.4

**ANTES de passar para Fase 2.5, TODOS os itens abaixo devem ser concluídos:**

```bash
# Todos os comandos usam pnpm
pnpm build          # ZERO errors
pnpm lint           # ZERO warnings
pnpm type-check     # ZERO TypeScript errors
pnpm test:e2e       # 100% pass (Playwright)
```

- [ ] **pnpm build** — ZERO errors
- [ ] **pnpm lint** — ZERO warnings
- [ ] **pnpm type-check** — ZERO TypeScript errors
- [ ] **pnpm test:e2e** — 100% pass (Playwright)
- [ ] Lighthouse score > 90 (Performance, Accessibility, Best Practices)
- [ ] Responsivo (mobile, tablet, desktop)
- [ ] Dark mode funcionando
- [ ] WebSocket (SignalR) conectado
- [ ] REST API client funcionando
- [ ] WCAG 2.1 AA compliance
- [ ] Todos componentes reutilizáveis (zero JSX duplicado)
- [ ] Design System aplicado consistentemente
- [ ] Code review aprovado

---

## Cronograma Detalhado

| Tarefa | Estimativa | Dependências | Blocker |
|--------|-----------|--------------|---------|
| 2.4.1 Criar projeto Next.js 16 | 2h | Fase 2.3 | Não |
| 2.4.2 Setup Design System + shadcn/ui | 4h | 2.4.1 | Não |
| 2.4.3 Criar tipos TypeScript | 2h | 2.4.1 | Não |
| 2.4.4 Criar API client + React Query | 4h | 2.4.3 | **Sim** |
| 2.4.5 Criar SignalR client | 4h | 2.4.3 | **Sim** |
| 2.4.6 Criar páginas (Dashboard + Downloads) | 8h | 2.4.2, 2.4.4, 2.4.5 | **Sim** |
| 2.4.7 Dark Mode + Theme System | 2h | 2.4.2 | Não |
| 2.4.8 Responsividade | 4h | 2.4.6 | Não |
| 2.4.9 Acessibilidade (a11y) | 3h | 2.4.6 | Não |
| 2.4.10 Testes E2E | 6h | 2.4.6 | **Sim** |

**Total:** 39 horas (7-10 dias)

---

## Tecnologias

| Categoria | Tecnologia | Versão |
|-----------|-----------|--------|
| Framework | Next.js | 16 (App Router) |
| UI Library | React | 19 |
| Language | TypeScript | strict mode |
| Styling | TailwindCSS | v4 |
| Components | shadcn/ui + Design System | latest |
| Package Manager | pnpm | latest |
| WebSocket | @microsoft/signalr | latest |
| Data Fetching | TanStack React Query | v5 |
| Dark Mode | next-themes | latest |
| Dates | date-fns | latest |
| Icons | lucide-react | latest |
| Forms | react-hook-form + zod | latest |
| Tests E2E | Playwright | latest |
| Linter | ESLint (flat config) | latest |
| Formatter | Prettier | latest |

---

## Próximos Passos

Após completar Fase 2.4:

1. ✅ **Fase 2.5**: Integração CLI ↔ API
   - CLI funcionar em modo local ou remoto
   - Config file para API URL
   - Testar ambos modos

2. **Deploy em Staging**
   - Docker compose (API + Frontend)
   - Testar em produção-like environment
   - Performance tuning

3. **Documentação**
   - README do frontend
   - Como desenvolver (pnpm dev, porta 4000)
   - Troubleshooting

---

## Troubleshooting Comum

**Problema: Dev server não sobe na porta 4000**

```bash
# Verificar se porta está em uso
lsof -i :4000
# Matar processo se necessário
kill -9 <PID>
# Ou usar outra porta temporariamente
pnpm dev --port 4001
```

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
3. Rodar `pnpx shadcn@latest add` novamente

**Problema: Progresso não atualiza em tempo real**

**Solução:**
1. Verificar se useDownloadProgress hook está sendo usado
2. Verificar se ws.joinDownload(downloadId) foi chamado
3. Verificar console para erros SignalR
4. Verificar se BackgroundWorker está enviando eventos

**Problema: Lighthouse score baixo**

**Solução:**
1. Usar Server Components onde possível (sem "use client" desnecessário)
2. Otimizar imagens com next/image
3. Lazy loading de componentes pesados
4. Melhorar contrast ratios (WCAG AA)
5. Adicionar loading states (Suspense boundaries)
