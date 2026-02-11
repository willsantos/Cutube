# Fase 3.5.4: Dashboard de Dead Letter Queue

**Status:** 🎯 Planejamento
**Épico:** Cutube-858 (Épico 3: Integração com RabbitMQ)
**Duração:** 1-2 dias
**Responsável:** Frontend Developer
**Prioridade:** 🔥 Alta
**Dependência:** ✅ Fase 3.5.3 (Endpoints de Reprocessamento) completa

---

## Objetivo

Criar uma interface visual dedicada para gerenciar mensagens na **Dead Letter Queue (DLQ)**. O dashboard permitirá que administradores visualizem downloads que falharam permanentemente, vejam detalhes dos erros e reprocessem ou descartem mensagens manualmente.

**Benefícios:**
- ✅ **Visibilidade**: Admins podem ver facilmente todos os downloads na DLQ
- ✅ **Ação Rápida**: Botões de Retry/Discard para operações manuais
- ✅ **Detalhamento**: Informações completas do erro (tipo, mensagem, stack trace)
- ✅ **Batch Operations**: Reprocessar todas as mensagens da DLQ de uma vez
- ✅ **Contador de Retentativas**: Visualizar quantas vezes um download falhou
- ✅ **UX Aprimorada**: Separação clara entre failed (retratável) e dead_letter (manual)

---

## Visão Arquitetural

### Fluxo de Reprocessamento Manual

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Fluxo de Reprocessamento Manual                    │
│                                                                         │
│  ┌──────────────┐                                                       │
│  │   Monitor    │  1. Admin abre página /monitor                      │
│  │    Page      │                                                       │
│  └──────┬───────┘                                                       │
│         │                                                               │
│         ▼                                                               │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │              DeadLetterQueue Component                         │   │
│  │                                                                   │   │
│  │  ┌────────────────────────────────────────────────────────┐    │   │
│  │  │  Lista de mensagens na DLQ                         │    │   │
│  │  │                                                        │    │   │
│  │  │  ┌────────────────────────────────────────┐           │    │   │
│  │  │  │ DLQ Card                              │           │    │   │
│  │  │  │ • Correlation ID                      │           │    │   │
│  │  │  │ • URL do vídeo                        │           │    │   │
│  │  │  │ • Error Message (expansível)          │           │       │   │
│  │  │  │ • Retry Count                         │           │    │   │
│  │  │  │ • Timestamp                          │           │    │   │
│  │  │  │                                    │           │    │   │
│  │  │  │ [Retry] [Discard]                  │           │    │   │
│  │  │  └────────────────────────────────────────┘           │    │   │
│  │  └────────────────────────────────────────────────────────┘    │   │
│  │                                                                   │   │
│  │  [Retry All] [Refresh]                                         │   │
│  └───────────────────────────────────────────────────────────────────┘   │
│         │                                                               │
│         │ 2. Admin clica em [Retry]                                    │
│         ▼                                                               │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                    API Call                                    │   │
│  │  POST /api/downloads/dlq/{correlationId}/retry               │   │
│  └──────────────────────────────┬─────────────────────────────────┘   │
│                               │                                      │
│                               ▼                                      │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                   RabbitMQ Queue                               │   │
│  │  Mensagem republicada em cutube.downloads                     │   │
│  └───────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Layout do Componente DeadLetterQueue

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Dead Letter Queue Management                      │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │  Header                                                        │  │
│  │  ⚠️ Dead Letter Queue (5 messages)                              │  │
│  │  Messages that failed permanently and need manual intervention       │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │  Actions Bar                                                   │  │
│  │  [🔄 Retry All] [🔄 Refresh] [🗑️ Clear All]                 │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │  ┌────────────────────────────────────────────────────────┐      │  │
│  │  │ DLQ Card - Expanded Error View               │      │  │
│  │  │                                                     │      │  │
│  │  │ ┌──────────────────────────────────────────┐         │      │  │
│  │  │ │ Header                                │         │      │  │
│  │  │ │ 🎵 Amazing Video Title               │         │      │  │
│  │  │ │ https://youtube.com/watch?v=abc123    │         │      │  │
│  │  │ │ ⚠️ Dead Letter • Retry #3           │         │      │  │
│  │  │ └──────────────────────────────────────────┘         │      │  │
│  │  │                                                     │      │  │
│  │  │ ┌──────────────────────────────────────────┐         │      │  │
│  │  │ │ Error Details                          │         │      │  │
│  │  │ │ 📌 Type: TransientException         │         │      │  │
│  │  │ │ 💬 Message: Network timeout...        │         │      │  │
│  │  │ │ ⏱️ Failed: 2 minutes ago           │         │      │  │
│  │  │ │ [▼ Show Stack Trace]                 │         │      │  │
│  │  │ └──────────────────────────────────────────┘         │      │  │
│  │  │                                                     │      │  │
│  │  │ ┌──────────────────────────────────────────┐         │      │  │
│  │  │ │ Actions                                │         │      │  │
│  │  │ │ [🔄 Retry] [🗑️ Discard]               │         │      │  │
│  │  │ └──────────────────────────────────────────┘         │      │  │
│  │  └─────────────────────────────────────────────────────┘      │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Estados e Transições UI

```
┌──────────┐     ┌──────────┐     ┌──────────────┐     ┌─────────────┐
│  Queued  │────►│Processing│────►│   Completed  │     │             │
└──────────┘     └────┬─────┘     └──────────────┘     │  Success    │
                       │                                 └─────────────┘
                       │
                       ▼ Error temporário
                ┌──────────────┐
                │   Failed     │
                │ (retryable)  │
                └──────┬───────┘
                       │
                       │ 3 tentativas com backoff
                       │
                       ▼ Error permanente
                ┌──────────────┐     ┌──────────────┐
                │  DeadLetter  │────►│   Queued     │
                │    (DLQ)     │     │  (retry)     │
                └──────────────┘     └──────────────┘
                       │
                       ▼ Descartado
                ┌──────────────┐
                │  Cancelled  │
                │  (discarded) │
                └──────────────┘
```

### Diferença: Failed vs DeadLetter

```
┌─────────────────────────────────────────────────────────────────────────┐
│               Distinção Visual: Failed vs DeadLetter                │
│                                                                         │
│  FAILED (retryable automatically)                                    │
│  ┌────────────────────────────────────────────────────────┐             │
│  │ ❌ Failed                                           │             │
│  │    Error: Network timeout                              │             │
│  │    Will retry automatically in 30s...                 │             │
│  │    [View Details]                                     │             │
│  └────────────────────────────────────────────────────────┘             │
│                                                                         │
│  DEAD LETTER (manual intervention required)                         │
│  ┌────────────────────────────────────────────────────────┐             │
│  │ ⚠️ Dead Letter                                      │             │
│  │    Error: Video unavailable                           │             │
│  │    Failed after 3 attempts                           │             │
│  │    [🔄 Retry Now] [🗑️ Discard]                     │             │
│  └────────────────────────────────────────────────────────┘             │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Tarefas

---

### 3.5.4.1 Criar API Client para DLQ

**Estimativa:** 1-2 horas

**Arquivos:**
```
cutube-web/
  └── lib/
      └── api.ts (atualizar)
```

#### 3.5.4.1.1 Adicionar métodos DLQ na API

```typescript
// cutube-web/lib/api.ts

export const api = {
  // ... métodos existentes ...

  dlq: {
    getAll: async (): Promise<DownloadListResponse> => {
      return fetchApi<DownloadListResponse>("/api/downloads/dlq");
    },

    retry: async (correlationId: string): Promise<{ message: string; status: string }> => {
      return fetchApi<{ message: string; status: string }>(
        `/api/downloads/dlq/${correlationId}/retry`,
        { method: "POST" }
      );
    },

    retryAll: async (): Promise<{
      message: string;
      totalCount: number;
      successCount: number;
      failedCount: number;
    }> => {
      return fetchApi<{
        message: string;
        totalCount: number;
        successCount: number;
        failedCount: number;
      }>("/api/downloads/dlq/retry-all", { method: "POST" });
    },

    discard: async (correlationId: string): Promise<void> => {
      return fetchApi<void>(`/api/downloads/dlq/${correlationId}`, {
        method: "DELETE",
      });
    },
  },
};
```

**Checklist:**
- [ ] Adicionar seção `dlq` no objeto `api`
- [ ] Implementar `dlq.getAll()` para buscar mensagens DLQ
- [ ] Implementar `dlq.retry(correlationId)` para reprocessar mensagem
- [ ] Implementar `dlq.retryAll()` para reprocessar todas
- [ ] Implementar `dlq.discard(correlationId)` para descartar mensagem
- [ ] Verificar tipos TypeScript compatíveis

**Critérios de aceite:**
- ✅ Todos os endpoints da DLQ estão mapeados
- ✅ Tipos TypeScript corretos para requests/responses
- ✅ Tratamento de erros apropriado
- ✅ Código segue padrão existente do `api.ts`

---

### 3.5.4.2 Criar Hook Personalizado para DLQ

**Estimativa:** 1-2 horas

**Arquivos:**
```
cutube-web/
  └── hooks/
      └── use-dlq.ts (novo)
```

#### 3.5.4.2.1 Implementar hook useDlq

```typescript
// cutube-web/hooks/use-dlq.ts
"use client";

import { useState, useEffect, useCallback } from "react";
import type { MonitorDownloadStatus } from "@/types";
import { api } from "@/lib/api";

interface UseDlqReturn {
  dlqMessages: MonitorDownloadStatus[];
  isLoading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
  retryMessage: (correlationId: string) => Promise<boolean>;
  retryAll: () => Promise<{ totalCount: number; successCount: number; failedCount: number }>;
  discardMessage: (correlationId: string) => Promise<boolean>;
  lastUpdated: Date | null;
}

export function useDlq(refreshInterval = 10000): UseDlqReturn {
  const [dlqMessages, setDlqMessages] = useState<MonitorDownloadStatus[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setError(null);
      const response = await api.dlq.getAll();
      setDlqMessages(response.downloads);
      setLastUpdated(new Date());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unknown error");
    } finally {
      setIsLoading(false);
    }
  }, []);

  const retryMessage = useCallback(async (correlationId: string) => {
    try {
      await api.dlq.retry(correlationId);
      await fetchData(); // Refresh lista
      return true;
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to retry");
      return false;
    }
  }, [fetchData]);

  const retryAll = useCallback(async () => {
    try {
      const result = await api.dlq.retryAll();
      await fetchData(); // Refresh lista
      return result;
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to retry all");
      return { totalCount: 0, successCount: 0, failedCount: 0 };
    }
  }, [fetchData]);

  const discardMessage = useCallback(async (correlationId: string) => {
    try {
      await api.dlq.discard(correlationId);
      await fetchData(); // Refresh lista
      return true;
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to discard");
      return false;
    }
  }, [fetchData]);

  useEffect(() => {
    fetchData();
    const interval = setInterval(fetchData, refreshInterval);
    return () => clearInterval(interval);
  }, [fetchData, refreshInterval]);

  return {
    dlqMessages,
    isLoading,
    error,
    refresh: fetchData,
    retryMessage,
    retryAll,
    discardMessage,
    lastUpdated,
  };
}
```

**Checklist:**
- [ ] Criar hook `useDlq` com estado das mensagens
- [ ] Implementar busca automática (polling)
- [ ] Implementar `retryMessage` para retry individual
- [ ] Implementar `retryAll` para retry em lote
- [ ] Implementar `discardMessage` para descartar
- [ ] Adicionar tratamento de erros
- [ ] Exportar tipos TypeScript apropriados

**Critérios de aceite:**
- ✅ Hook busca mensagens da DLQ automaticamente
- ✅ Operações de retry/discard atualizam o estado
- ✅ Erros são propagados para o componente
- ✅ Polling configurável (default: 10s)
- ✅ TypeScript sem erros

---

### 3.5.4.3 Criar Componente DeadLetterQueue

**Estimativa:** 3-4 horas

**Arquivos:**
```
cutube-web/
  └── components/
      └── monitor/
          └── dead-letter-queue.tsx (novo)
          └── dlq-card.tsx (novo)
```

#### 3.5.4.3.1 Criar DlqCard (Card Individual)

```typescript
// cutube-web/components/monitor/dlq-card.tsx
"use client";

import type { MonitorDownloadStatus } from "@/types";
import { useState } from "react";

interface DlqCardProps {
  download: MonitorDownloadStatus;
  onRetry: (correlationId: string) => Promise<boolean>;
  onDiscard: (correlationId: string) => Promise<boolean>;
}

export function DlqCard({ download, onRetry, onDiscard }: DlqCardProps) {
  const [isRetrying, setIsRetrying] = useState(false);
  const [isDiscarding, setIsDiscarding] = useState(false);
  const [showStackTrace, setShowStackTrace] = useState(false);

  const handleRetry = async () => {
    setIsRetrying(true);
    await onRetry(download.correlationId);
    setIsRetrying(false);
  };

  const handleDiscard = async () => {
    if (!confirm("Are you sure you want to discard this message?")) return;
    setIsDiscarding(true);
    await onDiscard(download.correlationId);
    setIsDiscarding(false);
  };

  const timeSinceFailed = download.updatedAt
    ? new Date(download.updatedAt).toLocaleString()
    : "Unknown";

  return (
    <div className="bg-white rounded-lg shadow-md border-2 border-orange-300 p-4">
      {/* Header */}
      <div className="flex justify-between items-start mb-3">
        <div className="flex-1 min-w-0">
          <h3 className="font-semibold text-gray-800 truncate" title={download.url}>
            🎵 {download.metadata?.title || "Untitled"}
          </h3>
          <p className="text-xs text-gray-500 truncate">{download.url}</p>
        </div>
        <span className="text-xs px-2 py-1 rounded-full bg-orange-100 text-orange-700 border border-orange-300">
          ⚠️ Dead Letter • Retry #{download.retryCount}
        </span>
      </div>

      {/* Error Details */}
      <div className="mb-3 bg-orange-50 border border-orange-200 rounded p-3">
        <div className="flex items-center gap-2 mb-2">
          <span className="text-sm font-semibold text-orange-800">Error Details</span>
          <span className="text-xs text-gray-500">{timeSinceFailed}</span>
        </div>
        <p className="text-sm text-orange-700 mb-2">
          💬 {download.errorMessage || "Unknown error"}
        </p>

        {/* Stack Trace (expansível) */}
        {download.errorMessage && download.errorMessage.length > 100 && (
          <button
            onClick={() => setShowStackTrace(!showStackTrace)}
            className="text-xs text-orange-600 hover:text-orange-800 underline"
          >
            {showStackTrace ? "▼ Hide" : "▶ Show"} Details
          </button>
        )}
      </div>

      {/* Actions */}
      <div className="flex gap-2 justify-end">
        <button
          onClick={handleRetry}
          disabled={isRetrying}
          className="px-3 py-1.5 bg-blue-600 text-white text-sm rounded hover:bg-blue-700 disabled:opacity-50 flex items-center gap-1"
        >
          {isRetrying ? "🔄 Retrying..." : "🔄 Retry"}
        </button>
        <button
          onClick={handleDiscard}
          disabled={isDiscarding}
          className="px-3 py-1.5 bg-red-600 text-white text-sm rounded hover:bg-red-700 disabled:opacity-50 flex items-center gap-1"
        >
          {isDiscarding ? "🗑️ Discarding..." : "🗑️ Discard"}
        </button>
      </div>
    </div>
  );
}
```

#### 3.5.4.3.2 Criar DeadLetterQueue (Lista de Cards)

```typescript
// cutube-web/components/monitor/dead-letter-queue.tsx
"use client";

import { useDlq } from "@/hooks/use-dlq";
import { DlqCard } from "./dlq-card";

interface DeadLetterQueueProps {
  className?: string;
}

export function DeadLetterQueue({ className = "" }: DeadLetterQueueProps) {
  const {
    dlqMessages,
    isLoading,
    error,
    refresh,
    retryAll,
    lastUpdated,
  } = useDlq(10000); // Poll a cada 10s

  const handleRetryAll = async () => {
    const confirm = window.confirm(
      `Are you sure you want to retry all ${dlqMessages.length} messages in the DLQ?`
    );
    if (!confirm) return;

    const result = await retryAll();
    alert(
      `Batch retry completed:\n` +
      `Total: ${result.totalCount}\n` +
      `Success: ${result.successCount}\n` +
      `Failed: ${result.failedCount}`
    );
  };

  return (
    <div className={`space-y-4 ${className}`}>
      {/* Header */}
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold flex items-center gap-2 text-orange-600">
          <span>⚠️</span>
          Dead Letter Queue
          <span className="text-sm font-normal text-gray-500">
            ({dlqMessages.length} messages)
          </span>
        </h2>
        <div className="flex gap-2">
          <button
            onClick={handleRetryAll}
            disabled={dlqMessages.length === 0}
            className="px-3 py-1.5 bg-green-600 text-white text-sm rounded hover:bg-green-700 disabled:opacity-50 flex items-center gap-1"
          >
            🔄 Retry All
          </button>
          <button
            onClick={refresh}
            disabled={isLoading}
            className="px-3 py-1.5 bg-blue-600 text-white text-sm rounded hover:bg-blue-700 disabled:opacity-50 flex items-center gap-1"
          >
            {isLoading ? "🔄 Refreshing..." : "🔄 Refresh"}
          </button>
        </div>
      </div>

      {/* Error State */}
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-red-700">{error}</p>
        </div>
      )}

      {/* Loading State */}
      {isLoading && dlqMessages.length === 0 ? (
        <div className="bg-gray-50 border border-gray-200 rounded-lg p-8 text-center">
          <p className="text-gray-500">Loading DLQ messages...</p>
        </div>
      ) : null}

      {/* Empty State */}
      {!isLoading && dlqMessages.length === 0 ? (
        <div className="bg-green-50 border border-green-200 rounded-lg p-8 text-center">
          <p className="text-green-700 text-lg mb-2">✅ No messages in Dead Letter Queue</p>
          <p className="text-green-600 text-sm">All downloads are healthy!</p>
        </div>
      ) : null}

      {/* DLQ Messages List */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {dlqMessages.map((download) => (
          <DlqCard
            key={download.correlationId}
            download={download}
            onRetry={async (id) => {
              const result = await api.dlq.retry(id);
              refresh();
              return true;
            }}
            onDiscard={async (id) => {
              await api.dlq.discard(id);
              refresh();
              return true;
            }}
          />
        ))}
      </div>

      {/* Footer Info */}
      {lastUpdated && (
        <div className="text-center text-xs text-gray-400 mt-4">
          Last updated: {lastUpdated.toLocaleString()}
        </div>
      )}
    </div>
  );
}
```

**Checklist:**
- [ ] Criar `DlqCard` componente individual
- [ ] Adicionar visualização expandida de erros
- [ ] Adicionar botões de Retry e Discard
- [ ] Adicionar estado de loading durante operações
- [ ] Criar `DeadLetterQueue` componente lista
- [ ] Adicionar cabeçalho com contador de mensagens
- [ ] Adicionar botão "Retry All" com confirmação
- [ ] Adicionar loading e empty states
- [ ] Adicionar polling automático (10s)
- [ ] Seguir padrão de design existente (Tailwind)

**Critérios de aceite:**
- ✅ Componente lista todas as mensagens da DLQ
- ✅ Cada card mostra detalhes completos do erro
- ✅ Botões Retry e Discard funcionam corretamente
- ✅ "Retry All" processa todas as mensagens em lote
- ✅ Loading states durante operações
- ✅ Confirmação antes de descartar mensagens
- ✅ Visual responsivo (grid: 1→2→3 colunas)

---

### 3.5.4.4 Integrar DLQ Dashboard na Página Monitor

**Estimativa:** 1-2 horas

**Arquivos:**
```
cutube-web/
  └── app/
      └── monitor/
          └── page.tsx (atualizar)
```

#### 3.5.4.4.1 Atualizar página Monitor

```typescript
// cutube-web/app/monitor/page.tsx
"use client";

import { useMonitor } from "@/hooks/use-monitor";
import { useSignalR } from "@/hooks/use-signalr";
import { MonitorMetrics } from "@/components/monitor/monitor-metrics";
import { ActiveQueue } from "@/components/monitor/active-queue";
import { FailedDownloads } from "@/components/monitor/failed-downloads";
import { DeadLetterQueue } from "@/components/monitor/dead-letter-queue"; // NOVO
import { useCallback } from "react";

export default function MonitorPage() {
  const {
    activeDownloads,
    failedDownloads,
    metrics,
    isLoading,
    error,
    refresh,
    lastUpdated,
  } = useMonitor(5000);

  const handleStatusChanged = useCallback(() => {
    refresh();
  }, [refresh]);

  const { isConnected } = useSignalR({
    onDownloadStatusChanged: handleStatusChanged,
    onDownloadCompleted: handleStatusChanged,
    onDownloadFailed: handleStatusChanged,
  });

  if (error) {
    return (
      <div className="min-h-screen bg-gray-50 p-6">
        <div className="max-w-7xl mx-auto">
          <div className="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
            <p className="text-4xl mb-2">❌</p>
            <h2 className="text-lg font-semibold text-red-700 mb-2">Error loading monitor</h2>
            <p className="text-red-600 mb-4">{error}</p>
            <button
              onClick={refresh}
              className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700"
            >
              Retry
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center mb-6 gap-4">
          <div>
            <h1 className="text-3xl font-bold text-gray-800">Monitor</h1>
            <p className="text-gray-500">
              System monitoring and queue status
              {isConnected ? (
                <span className="ml-2 text-green-600 font-medium">● Live</span>
              ) : (
                <span className="ml-2 text-yellow-600">○ Polling</span>
              )}
              {lastUpdated && (
                <span className="ml-2 text-xs text-gray-400">
                  Updated: {lastUpdated.toLocaleTimeString()}
                </span>
              )}
            </p>
          </div>
          <button
            onClick={refresh}
            disabled={isLoading}
            className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50 flex items-center gap-2"
          >
            {isLoading ? "🔄 Refreshing..." : "🔄 Refresh"}
          </button>
        </div>

        {/* Metrics */}
        <MonitorMetrics metrics={metrics} />

        {/* Active Queue */}
        <div className="mb-8">
          <ActiveQueue downloads={activeDownloads} />
        </div>

        {/* Failed Downloads */}
        <div className="mb-8">
          <FailedDownloads downloads={failedDownloads} />
        </div>

        {/* Dead Letter Queue - NOVO */}
        <div className="mb-8">
          <DeadLetterQueue />
        </div>
      </div>
    </div>
  );
}
```

**Checklist:**
- [ ] Importar componente `DeadLetterQueue`
- [ ] Adicionar seção DLQ abaixo de Failed Downloads
- [ ] Manter consistência visual com outras seções
- [ ] Testar integração com SignalR updates
- [ ] Verificar responsividade

**Critérios de aceite:**
- ✅ Dashboard DLQ aparece na página Monitor
- ✅ Layout consistente com outras seções
- ✅ SignalR updates funcionam para DLQ
- ✅ Polling não conflita com SignalR

---

### 3.5.4.5 Melhorar FailedDownloads para Separar DLQ

**Estimativa:** 1-2 horas

**Arquivos:**
```
cutube-web/
  └── components/
      └── monitor/
          └── failed-downloads.tsx (atualizar)
```

#### 3.5.4.5.1 Atualizar componente FailedDownloads

```typescript
// cutube-web/components/monitor/failed-downloads.tsx
"use client";

import type { MonitorDownloadStatus } from "@/types";
import { QueueStatusCard } from "./queue-status-card";

interface FailedDownloadsProps {
  downloads: MonitorDownloadStatus[];
}

export function FailedDownloads({ downloads }: FailedDownloadsProps) {
  // Separar: failed (retryable) vs dead_letter (manual)
  const failed = downloads.filter((d) => d.state === "failed");
  const deadLetter = downloads.filter((d) => d.state === "dead_letter");

  if (failed.length === 0 && deadLetter.length === 0) {
    return null;
  }

  return (
    <div className="space-y-4">
      {/* Failed (Retryable Automatically) */}
      {failed.length > 0 && (
        <>
          <div className="flex items-center justify-between">
            <h2 className="text-xl font-semibold flex items-center gap-2 text-red-600">
              <span>❌</span>
              Failed Downloads
              <span className="text-sm font-normal text-gray-500">
                (will retry automatically)
              </span>
            </h2>
            <span className="inline-flex items-center px-2 py-1 bg-red-100 text-red-700 rounded text-sm">
              {failed.length} messages
            </span>
          </div>

          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {failed.map((download) => (
              <QueueStatusCard key={download.correlationId} download={download} />
            ))}
          </div>
        </>
      )}

      {/* Dead Letter (Manual Intervention) */}
      {deadLetter.length > 0 && (
        <div className="mt-6 pt-6 border-t-2 border-gray-200">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-xl font-semibold flex items-center gap-2 text-orange-600">
              <span>⚠️</span>
              Dead Letter Queue
              <span className="text-sm font-normal text-gray-500">
                (requires manual intervention)
              </span>
            </h2>
            <div className="text-sm">
              <span className="inline-flex items-center px-2 py-1 bg-orange-100 text-orange-700 rounded">
                {deadLetter.length} messages
              </span>
            </div>
          </div>

          <div className="bg-orange-50 border border-orange-200 rounded-lg p-4 mb-4">
            <p className="text-sm text-orange-700">
              <strong>⚠️ Attention:</strong> Messages in the Dead Letter Queue have failed
              permanently and cannot be retried automatically. Please review each message and
              decide whether to retry or discard it.
            </p>
          </div>

          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {deadLetter.map((download) => (
              <QueueStatusCard key={download.correlationId} download={download} />
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
```

**Checklist:**
- [ ] Separar visualmente `failed` de `dead_letter`
- [ ] Adicionar aviso explicativo sobre DLQ
- [ ] Manter cards existentes para dead_letter
- [ ] Adicionar divider visual entre seções
- [ ] Melhorar legibilidade com texto descritivo

**Critérios de aceite:**
- ✅ Failed downloads (automático) visualmente distinto de DLQ
- ✅ Aviso claro sobre necessidade de intervenção manual
- ✅ Cards dead_letter mostram informações completas
- ✅ Layout responsivo mantido

---

## Checklist de Fase

### Implementação
- [ ] API client para endpoints DLQ criado
- [ ] Hook `useDlq` implementado com polling
- [ ] Componente `DlqCard` criado com ações
- [ ] Componente `DeadLetterQueue` criado
- [ ] Dashboard DLQ integrado na página Monitor
- [ ] FailedDownloads atualizado para separar DLQ
- [ ] Estados de loading e empty implementados

### Testes
- [ ] Listagem de mensagens DLQ funciona
- [ ] Retry individual reprocessa mensagem corretamente
- [ ] Retry All processa todas as mensagens
- [ ] Discard remove mensagem da DLQ
- [ ] Confirmações aparecem antes de ações destrutivas
- [ ] Polling atualiza lista automaticamente
- [ ] SignalR updates funcionam com DLQ

### Design & UX
- [ ] Cores e ícones consistentes (laranja para DLQ)
- [ ] Cards responsivos em mobile (1 coluna)
- [ ] Animações de loading suaves
- [ ] Feedback visual imediato para ações
- [ ] Acessibilidade (contraste, tamanhos)

### Documentação
- [ ] Comentários em código explicativos
- [ ] Props dos componentes documentados
- [ ] API client com JSDoc

### Validação
- [ ] Todos os critérios de aceite atendidos
- [ ] Código segue padrões do projeto (Tailwind, TypeScript)
- [ ] Sem erros TypeScript
- [ ] Build funciona sem warnings
- [ ] Testes manuais passam

---

## Notas

- **Polling vs SignalR:** DLQ usa polling (10s) ao invés de SignalR para simplicidade, pois reprocessamentos são menos frequentes
- **Confirmação:** Usar `window.confirm` para simplicidade; pode ser substituído por modal customizado depois
- **Separação Visual:** Falhas automáticas (failed) vs manuais (dead_letter) devem ser visualmente distintas
- **Batch Retry:** "Retry All" deve ter confirmação dupla para evitar operações acidentais
- **Empty State:** Mostrar mensagem positiva quando DLQ está vazia ("All downloads are healthy!")

---

**Criado em:** 11/02/2026
**Última atualização:** 11/02/2026

---

## Referências

- [Fase 3.5: Retry & Dead Letter Queue](/home/willsantos/dev/Cutube/plans/fase-3.5-retry-dead-letter-queue.md)
- [Frontend Design System](/home/willsantos/dev/Cutube/cutube-web/design-system.md)
- [Épico 3: Integração com RabbitMQ](/home/willsantos/dev/Cutube/plans/epico-3-integracao-rabbitmq.md)
