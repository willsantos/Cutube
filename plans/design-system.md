# Cutube Design System

**Status:** 📐 Ativo
**Versão:** 1.0
**Stack:** Next.js 16 + TailwindCSS v4 + shadcn/ui
**Referência:** [Fase 2.4 — Frontend](./fase-2.4-frontend-nextjs.md)

---

## Filosofia

> **Restrained elegance.** Cada pixel tem propósito. Espaço é luxo. A interface deve ser invisível — o conteúdo é o protagonista.

**Princípios Fundamentais:**

1. **Clareza** — informação hierárquica, sem ambiguidade
2. **Consistência** — mesmos padrões visuais em toda aplicação
3. **Acessibilidade** — WCAG 2.1 AA desde o design, não como afterthought
4. **Composabilidade** — componentes pequenos que se combinam, nunca monolitos
5. **Elegância** — detalhes sutis que elevam: micro-animações, espaçamento generoso, tipografia cuidada

---

## 1. Cores

### Identidade Visual

A paleta do Cutube usa **Teal** como cor primária — transmite modernidade, tecnologia e confiança sem cair nos clichês de azul corporativo. A escolha segue o princípio de cores que funcionam bem tanto em light quanto dark mode.

### Escala de Cores (HSL)

```
Primary (Teal):
  50:  hsl(172, 67%, 96%)    ← background sutil
  100: hsl(172, 60%, 90%)
  200: hsl(172, 55%, 80%)
  300: hsl(172, 50%, 65%)
  400: hsl(172, 50%, 50%)
  500: hsl(172, 66%, 40%)    ← PRIMARY BASE
  600: hsl(172, 70%, 32%)
  700: hsl(172, 75%, 25%)
  800: hsl(172, 78%, 18%)
  900: hsl(172, 80%, 12%)    ← dark mode accent
  950: hsl(172, 82%, 8%)
```

### Paleta Semântica

| Token | Light Mode | Dark Mode | Uso |
|-------|-----------|-----------|-----|
| `--background` | `hsl(0, 0%, 99%)` | `hsl(220, 15%, 8%)` | Fundo principal |
| `--foreground` | `hsl(220, 15%, 10%)` | `hsl(220, 10%, 92%)` | Texto principal |
| `--card` | `hsl(0, 0%, 100%)` | `hsl(220, 15%, 11%)` | Fundo de cards |
| `--card-foreground` | `hsl(220, 15%, 10%)` | `hsl(220, 10%, 92%)` | Texto em cards |
| `--primary` | `hsl(172, 66%, 40%)` | `hsl(172, 55%, 50%)` | Ações principais, links |
| `--primary-foreground` | `hsl(0, 0%, 100%)` | `hsl(172, 80%, 8%)` | Texto sobre primary |
| `--secondary` | `hsl(220, 14%, 96%)` | `hsl(220, 15%, 15%)` | Backgrounds secundários |
| `--secondary-foreground` | `hsl(220, 15%, 30%)` | `hsl(220, 10%, 75%)` | Texto secundário |
| `--muted` | `hsl(220, 14%, 96%)` | `hsl(220, 15%, 15%)` | Elementos desabilitados |
| `--muted-foreground` | `hsl(220, 10%, 45%)` | `hsl(220, 10%, 55%)` | Texto muted, placeholders |
| `--accent` | `hsl(172, 60%, 95%)` | `hsl(172, 40%, 18%)` | Hover states, destaques |
| `--accent-foreground` | `hsl(172, 66%, 30%)` | `hsl(172, 50%, 80%)` | Texto sobre accent |
| `--destructive` | `hsl(0, 72%, 51%)` | `hsl(0, 62%, 55%)` | Erros, ações destrutivas |
| `--destructive-foreground` | `hsl(0, 0%, 100%)` | `hsl(0, 0%, 100%)` | Texto sobre destructive |
| `--border` | `hsl(220, 14%, 90%)` | `hsl(220, 15%, 18%)` | Bordas |
| `--input` | `hsl(220, 14%, 90%)` | `hsl(220, 15%, 18%)` | Borda de inputs |
| `--ring` | `hsl(172, 66%, 40%)` | `hsl(172, 55%, 50%)` | Focus ring |

### Cores de Status (Downloads)

| Status | Cor | Token |
|--------|-----|-------|
| Queued | Neutral/Gray | `--secondary` |
| Downloading | Blue | `hsl(210, 75%, 55%)` |
| Processing | Amber | `hsl(38, 92%, 50%)` |
| Completed | Emerald | `hsl(160, 72%, 42%)` |
| Failed | Red | `--destructive` |
| Cancelled | Gray | `--muted` |

### Regras de Uso

```
60% → Background (--background, --card)
     Superfícies neutras que não competem com conteúdo

30% → Secondary (--secondary, --muted, --border)
     Áreas de suporte: sidebars, separadores, badges

10% → Accent (--primary, --destructive, status colors)
     CTAs, status indicators, links, ações importantes
```

**Dark Mode Rules:**
- Nunca `#000000` — usar `hsl(220, 15%, 8%)` (cinza escuro com tint azul)
- Nunca `#FFFFFF` para texto — usar `hsl(220, 10%, 92%)` (~92% lightness)
- Reduzir saturação de cores accent em 10-15% no dark mode
- Elevação = brilho: cards ligeiramente mais claros que background
- Focus ring deve ser claramente visível em ambos modos

---

## 2. Tipografia

### Font Stack

```css
--font-sans: "Inter", -apple-system, BlinkMacSystemFont, "Segoe UI",
  Roboto, "Helvetica Neue", Arial, sans-serif;
--font-mono: "JetBrains Mono", "Fira Code", "SF Mono", Menlo, monospace;
```

**Inter** — escolhida por:
- Excelente legibilidade em tamanhos pequenos (UI)
- Variações de peso completas (300-800)
- Suporte a tabular numbers (alinhamento de dados)
- Open source, Google Fonts

### Escala Tipográfica

Ratio: **1.25** (Major Third) — balanceada para UI, nem muito compacta nem muito dramática.

| Token | Tamanho | Line Height | Peso | Uso |
|-------|---------|-------------|------|-----|
| `text-xs` | 12px / 0.75rem | 1.5 (18px) | 400 | Captions, labels menores |
| `text-sm` | 14px / 0.875rem | 1.5 (21px) | 400 | Texto secundário, metadata |
| `text-base` | 16px / 1rem | 1.6 (25.6px) | 400 | Body text (base) |
| `text-lg` | 18px / 1.125rem | 1.5 (27px) | 500 | Body destaque |
| `text-xl` | 20px / 1.25rem | 1.4 (28px) | 600 | Subtítulos |
| `text-2xl` | 24px / 1.5rem | 1.3 (31.2px) | 600 | Títulos de seção |
| `text-3xl` | 30px / 1.875rem | 1.2 (36px) | 700 | Títulos de página |
| `text-4xl` | 36px / 2.25rem | 1.1 (39.6px) | 700 | Hero, grandes números |

### Tracking (Letter Spacing)

| Contexto | Tracking | Uso |
|----------|----------|-----|
| Tight | -0.025em | Headings grandes (2xl+) |
| Normal | 0 | Body text |
| Wide | 0.025em | Labels, overlines, badges |
| Wider | 0.05em | All-caps text |

### Regras

- **Body text:** mínimo 16px (1rem) — nunca menor que 14px
- **Line length:** 45-75 caracteres (ideal: 65ch)
- **Line height:** 1.5-1.6 para body, 1.1-1.3 para headings
- **Font weight:** usar no máximo 3 pesos por projeto (400, 600, 700)
- **Números tabulares:** ativar `font-variant-numeric: tabular-nums` para dados numéricos (progresso, velocidade, ETA)

---

## 3. Espaçamento

### Grid de 8px

Todo espaçamento é múltiplo de 8px. Meio-step (4px) para micro-ajustes.

| Token | Valor | Uso |
|-------|-------|-----|
| `space-0.5` | 2px | Micro: border offsets |
| `space-1` | 4px | Tight: entre ícone e texto inline |
| `space-2` | 8px | Small: padding interno de badges |
| `space-3` | 12px | Medium-small: gap entre items compactos |
| `space-4` | 16px | Medium: padding de inputs, gap de cards |
| `space-5` | 20px | Medium-large |
| `space-6` | 24px | Large: padding de cards |
| `space-8` | 32px | XL: gap entre seções internas |
| `space-10` | 40px | 2XL |
| `space-12` | 48px | 3XL: gap entre seções principais |
| `space-16` | 64px | 4XL: margin de page sections |
| `space-20` | 80px | 5XL: hero padding |

### Padrões de Espaçamento

```
Page padding:
  Mobile:  px-4 (16px)
  Tablet:  px-6 (24px)
  Desktop: px-8 (32px)

Section gap:
  Vertical: py-12 (48px) entre seções
  Título → conteúdo: mb-6 (24px)

Card padding:
  Interno: p-6 (24px)
  Compacto: p-4 (16px)

Form fields gap:
  Vertical: space-y-4 (16px)
  Label → input: mb-2 (8px)
```

### Container

```css
.container {
  max-width: 1280px;
  margin-inline: auto;
  padding-inline: var(--space-4); /* 16px mobile */
}

@media (min-width: 768px) {
  .container { padding-inline: var(--space-6); } /* 24px tablet */
}

@media (min-width: 1024px) {
  .container { padding-inline: var(--space-8); } /* 32px desktop */
}
```

---

## 4. Componentes

### 4.1 Primitivos (UI Layer)

Componentes do shadcn/ui customizados com nossos tokens. Estes são os blocos base — nunca use HTML direto para estes elementos.

#### Button

| Variante | Uso | Visual |
|----------|-----|--------|
| `default` | Ação primária | Background primary, text white |
| `secondary` | Ação secundária | Background secondary |
| `outline` | Ação terciária | Border only |
| `ghost` | Ação sutil | Sem background, hover accent |
| `destructive` | Ação destrutiva | Background destructive |
| `link` | Link estilizado | Underline, primary color |

| Size | Height | Padding | Font | Uso |
|------|--------|---------|------|-----|
| `sm` | 32px | px-3 | text-sm | Actions compactas |
| `default` | 40px | px-4 | text-sm | Padrão |
| `lg` | 48px | px-6 | text-base | CTAs principais |
| `icon` | 40px | p-0 (w-10) | — | Botões icon-only |

**Regras:**
- Sempre texto descritivo ou `aria-label` para icon-only
- Loading state: ícone `Loader2` com `animate-spin` + texto "Carregando..."
- Disabled: `opacity-50`, `cursor-not-allowed`
- Focus: `ring-2 ring-ring ring-offset-2`

#### Card

```
┌─────────────────────────────────┐
│ CardHeader                       │
│   CardTitle                      │
│   CardDescription (opcional)     │
├─────────────────────────────────┤
│ CardContent                      │
│   (conteúdo principal)           │
├─────────────────────────────────┤
│ CardFooter (opcional)            │
│   (ações)                        │
└─────────────────────────────────┘
```

- Border: `1px solid --border`
- Radius: `--radius` (12px)
- Background: `--card`
- Shadow: `0 1px 3px 0 rgb(0 0 0 / 0.05)` (sutil)
- Hover: `shadow-md` (elevação sutil no hover — opcional)
- Padding: `p-6` (24px)

#### Input

- Height: 40px (default), 48px (lg)
- Border: `1px solid --input`
- Radius: `--radius-sm` (8px)
- Focus: `ring-2 ring-ring`
- Placeholder: `--muted-foreground`
- Error state: border `--destructive`, text `--destructive`

#### Badge

| Variante | Uso |
|----------|-----|
| `default` | Status neutro |
| `secondary` | Info |
| `outline` | Sutil |
| `destructive` | Erro/alerta |

- Font: `text-xs`, `font-medium`
- Padding: `px-2.5 py-0.5`
- Radius: `rounded-full`

#### Progress

- Height: 8px (default), 4px (compact)
- Background: `--secondary`
- Fill: variável por status (ver cores de status)
- Animação: `transition-all duration-500 ease-out`
- Em "processing": `animate-pulse`

### 4.2 Compostos (Feature Layer)

Componentes de negócio compostos dos primitivos. Cada um é uma unidade reutilizável.

#### StatusBadge

```tsx
// Mapeia DownloadStatus → Badge visual
// Reutilizado em: DownloadCard, DownloadDetail, DownloadList

<StatusBadge status="downloading" />
→ Badge azul com texto "Baixando"

<StatusBadge status="completed" />
→ Badge verde com texto "Concluído"
```

| Status | Label | Cor | Ícone (opcional) |
|--------|-------|-----|-----------------|
| queued | Na fila | Secondary (gray) | Clock |
| downloading | Baixando | Blue | ArrowDown |
| processing | Processando | Amber | Cog (animate-spin) |
| completed | Concluído | Emerald | Check |
| failed | Falhou | Destructive | X |
| cancelled | Cancelado | Muted | Slash |

#### ProgressBar

```tsx
// Reutilizado em: DownloadCard, DownloadDetail

<ProgressBar value={65} status="downloading" />
→ Barra 65% preenchida em azul, animação suave

<ProgressBar value={100} status="completed" />
→ Barra 100% preenchida em verde

<ProgressBar value={30} status="processing" />
→ Barra 30% com pulse animation
```

- Mostra percentual ao lado: `65%`
- Status `processing`: texto "Processando (ffmpeg)..." abaixo
- Transição suave de valor: `transition-all duration-500`

#### EmptyState

```tsx
// Reutilizado em: qualquer lista ou página vazia

<EmptyState
  icon={Download}
  title="Nenhum download ainda"
  description="Crie o primeiro download usando o formulário acima."
  action={{ label: "Criar download", onClick: scrollToForm }}
/>
```

- Centralizado vertical e horizontalmente
- Ícone: `48px`, `--muted-foreground`, opacity 50%
- Título: `text-lg`, `font-semibold`
- Descrição: `text-sm`, `--muted-foreground`
- Action (opcional): Button `variant="outline"`

#### ErrorMessage

```tsx
// Reutilizado em: forms, fetch errors, inline errors

<ErrorMessage message="Falha ao criar download" />
<ErrorMessage message={error} onRetry={refetch} />
```

- Background: `destructive/10`
- Border-left: `3px solid --destructive`
- Text: `text-sm`, `--destructive`
- Retry button (opcional): `variant="ghost" size="sm"`

#### InfoRow

```tsx
// Reutilizado em: DownloadDetail, qualquer exibição label: value

<InfoRow label="Velocidade" value="2.5 MB/s" icon={Zap} />
<InfoRow label="Criado em" value="10 fev 2026, 14:30" icon={Calendar} />
```

- Layout: flex, `justify-between` ou icon + label + value
- Label: `text-sm`, `--muted-foreground`
- Value: `text-sm`, `--foreground`, `font-medium`
- Ícone (opcional): `16px`, `--muted-foreground`

#### StatCard

```tsx
// Reutilizado em: DownloadDetail sidebar, Dashboard stats

<StatCard label="Velocidade" value="2.5 MB/s" icon={Zap} />
<StatCard label="Restante" value="3m 45s" icon={Clock} />
<StatCard label="Tamanho" value="150 MB" icon={HardDrive} />
```

- Baseado no Card primitivo
- Layout: ícone no topo ou esquerda, label acima do value
- Value: `text-xl` ou `text-2xl`, `font-semibold`, `tabular-nums`
- Label: `text-xs`, `--muted-foreground`, `uppercase`, tracking wide

#### LoadingSkeleton

```tsx
// Reutilizado em: qualquer componente em loading state

<LoadingSkeleton variant="card" />        // Skeleton de um card
<LoadingSkeleton variant="card" count={6} /> // 6 skeletons em grid
<LoadingSkeleton variant="list" count={3} /> // 3 linhas de lista
<LoadingSkeleton variant="detail" />      // Skeleton de página de detalhe
```

- Usa componente `Skeleton` do shadcn/ui
- Anima com `animate-pulse`
- Mantém layout proporcional ao conteúdo real

---

## 5. Layout

### Page Structure

```
┌─────────────────────────────────────────────────────────────┐
│ Header (fixed top)                                          │
│  Logo    Nav (Dashboard | Downloads)         ThemeToggle    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ PageContainer                                               │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Page Title + Description                             │   │
│  │                                                      │   │
│  │ Section                                              │   │
│  │  ┌──────────────────────────────────────────────┐   │   │
│  │  │ Content                                       │   │   │
│  │  └──────────────────────────────────────────────┘   │   │
│  │                                                      │   │
│  │ Section                                              │   │
│  │  ┌──────┐ ┌──────┐ ┌──────┐                        │   │
│  │  │ Card │ │ Card │ │ Card │  ← responsive grid     │   │
│  │  └──────┘ └──────┘ └──────┘                        │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Header

- Position: `sticky top-0 z-50`
- Background: `--background/80` com `backdrop-blur-lg`
- Border bottom: `1px solid --border`
- Height: `64px`
- Content: Logo (left), Nav (center ou left), ThemeToggle (right)
- Mobile: Logo + hamburger menu

### PageContainer

```tsx
<PageContainer>
  <PageContainer.Header
    title="Dashboard"
    description="Gerencie seus downloads"
    actions={<Button>Nova...</Button>}
  />
  <PageContainer.Content>
    {children}
  </PageContainer.Content>
</PageContainer>
```

- Max-width: `1280px`
- Padding horizontal responsivo (16px → 24px → 32px)
- Padding top: `32px` (abaixo do header)

### Grid Responsivo (Downloads)

```css
/* Mobile: 1 col */
grid-template-columns: 1fr;

/* Tablet (md): 2 cols */
grid-template-columns: repeat(2, 1fr);

/* Desktop (lg): 3 cols */
grid-template-columns: repeat(3, 1fr);

gap: var(--space-4); /* 16px */
```

### Detail Page Layout

```
Desktop (lg+):
┌──────────────────────────┬───────────────┐
│ Main Content (2/3)       │ Sidebar (1/3) │
│ ┌──────────────────────┐ │ ┌───────────┐ │
│ │ Status + Progress    │ │ │ Stats     │ │
│ └──────────────────────┘ │ ├───────────┤ │
│ ┌──────────────────────┐ │ │ Dates     │ │
│ │ Actions              │ │ ├───────────┤ │
│ └──────────────────────┘ │ │ File Info │ │
│                          │ └───────────┘ │
└──────────────────────────┴───────────────┘

Mobile/Tablet:
┌──────────────────────────────────────────┐
│ Status + Progress                         │
│ Stats (grid 2 cols)                       │
│ Dates                                     │
│ File Info                                 │
│ Actions                                   │
└──────────────────────────────────────────┘
```

---

## 6. Acessibilidade (a11y)

### WCAG 2.1 AA — Requisitos Obrigatórios

#### Contraste

| Tipo | Ratio Mínimo | Verificação |
|------|-------------|-------------|
| Texto normal (< 18px) | 4.5:1 | Todas combinações foreground/background |
| Texto grande (≥ 18px bold ou ≥ 24px) | 3:1 | Headings, CTAs |
| Componentes UI | 3:1 | Borders, icons informativos |
| Focus indicators | 3:1 | Ring vs background |

#### Semântica

```html
<!-- Landmarks obrigatórios -->
<header role="banner">...</header>
<nav role="navigation" aria-label="Principal">...</nav>
<main role="main">...</main>

<!-- Skip link (primeiro elemento focável) -->
<a href="#main-content" class="sr-only focus:not-sr-only">
  Pular para conteúdo principal
</a>

<!-- Headings hierárquicos -->
<h1>Dashboard</h1>
  <h2>Novo Download</h2>
  <h2>Downloads</h2>
    <h3>Download: video-title</h3>
```

#### Formulários

```tsx
// Cada input DEVE ter label associado
<label htmlFor="url">URL do vídeo</label>
<input id="url" type="url" aria-required="true" aria-invalid={!!error} />

// Erros devem ser anunciados
<p role="alert" aria-live="assertive">{errorMessage}</p>

// Grupos de campos relacionados
<fieldset>
  <legend>Intervalo de tempo</legend>
  <label htmlFor="startTime">Início</label>
  <input id="startTime" />
  <label htmlFor="endTime">Fim</label>
  <input id="endTime" />
</fieldset>
```

#### Interatividade

```css
/* Focus ring visível em TODOS elementos interativos */
:focus-visible {
  outline: 2px solid var(--ring);
  outline-offset: 2px;
}

/* Respeitar preferência de movimento reduzido */
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}
```

#### Live Regions (Progresso de Download)

```tsx
// Progresso de download deve ser anunciado para leitores de tela
<div
  role="progressbar"
  aria-valuenow={65}
  aria-valuemin={0}
  aria-valuemax={100}
  aria-label="Progresso do download"
>
  <span className="sr-only">65% concluído</span>
</div>

// Status changes devem ser anunciados
<div aria-live="polite" aria-atomic="true">
  Download concluído com sucesso
</div>
```

#### Checklist a11y por Componente

| Componente | Requisitos |
|------------|-----------|
| Button | Texto visível OU `aria-label`. Focus ring. Disabled state. |
| Input | `<label>` associado. `aria-required`. `aria-invalid` + `aria-describedby` para erros. |
| Badge | `role="status"` quando dinâmico. Cor não é único indicador (+ texto). |
| Progress | `role="progressbar"`. `aria-valuenow/min/max`. `aria-label`. |
| Dialog | `role="dialog"`. `aria-modal="true"`. Focus trap. Esc to close. |
| Toast | `role="alert"`. `aria-live="assertive"`. Auto-dismiss com tempo suficiente. |
| ThemeToggle | `aria-label` descritivo ("Mudar para modo escuro/claro"). |
| EmptyState | Texto descritivo (não apenas ícone). |
| Card | Semântico (`<article>` se independente). Heading para título. |

---

## 7. Animações & Transições

### Princípios

- **Propósito:** toda animação comunica algo (feedback, transição, atenção)
- **Sutileza:** preferir animações curtas e suaves, nunca chamativos
- **Performance:** animar apenas `transform` e `opacity`
- **Acessibilidade:** respeitar `prefers-reduced-motion`

### Timing Tokens

| Token | Duração | Uso |
|-------|---------|-----|
| `--transition-fast` | 100ms | Hover states, opacity toggles |
| `--transition-normal` | 200ms | Buttons, inputs, badges |
| `--transition-slow` | 300ms | Cards, modals, page transitions |
| `--transition-slower` | 500ms | Progress bar fill, skeleton pulse |

### Easing

| Easing | Curva | Uso |
|--------|-------|-----|
| `ease-out` | `cubic-bezier(0, 0, 0.2, 1)` | Elementos entrando (aparecendo) |
| `ease-in` | `cubic-bezier(0.4, 0, 1, 1)` | Elementos saindo (desaparecendo) |
| `ease-in-out` | `cubic-bezier(0.4, 0, 0.2, 1)` | Movimentos de posição, progress |

### Padrões de Animação

```css
/* Hover em cards — elevação sutil */
.card {
  transition: box-shadow var(--transition-normal) ease-out,
              transform var(--transition-normal) ease-out;
}
.card:hover {
  box-shadow: 0 8px 24px -8px rgb(0 0 0 / 0.12);
  transform: translateY(-1px);
}

/* Button press feedback */
.button:active {
  transform: scale(0.98);
}

/* Loading spinner */
@keyframes spin {
  to { transform: rotate(360deg); }
}
.spinner { animation: spin 1s linear infinite; }

/* Skeleton pulse */
@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.4; }
}
.skeleton { animation: pulse 2s ease-in-out infinite; }

/* Progress bar fill */
.progress-fill {
  transition: width var(--transition-slower) ease-in-out;
}

/* Page content fade in */
@keyframes fadeIn {
  from { opacity: 0; transform: translateY(4px); }
  to { opacity: 1; transform: translateY(0); }
}
.page-content { animation: fadeIn var(--transition-slow) ease-out; }
```

### Reduzir Movimento

```css
@media (prefers-reduced-motion: reduce) {
  .card:hover { transform: none; }
  .progress-fill { transition: none; }
  .page-content { animation: none; }
  /* Manter spinners de loading (feedback essencial) mas sem bounce */
}
```

---

## 8. Shadows & Elevação

### Sistema de Sombras

| Level | Sombra | Uso |
|-------|--------|-----|
| `shadow-xs` | `0 1px 2px 0 rgb(0 0 0 / 0.03)` | Inputs, badges |
| `shadow-sm` | `0 1px 3px 0 rgb(0 0 0 / 0.05)` | Cards em repouso |
| `shadow-md` | `0 4px 12px -2px rgb(0 0 0 / 0.08)` | Cards em hover |
| `shadow-lg` | `0 8px 24px -8px rgb(0 0 0 / 0.12)` | Dropdowns, modals |
| `shadow-xl` | `0 16px 48px -12px rgb(0 0 0 / 0.15)` | Dialogs, overlays |

### Dark Mode Shadows

Em dark mode, sombras tradicionais são quase invisíveis. Usar:
- Borders mais claras (`--border` mais luminoso)
- Background ligeiramente mais claro para elevação
- Glow sutil com cor primary para destaque (opcional, usado com moderação)

```css
/* Dark mode elevation via brightness */
.dark .card { background: hsl(220, 15%, 11%); }
.dark .card:hover { background: hsl(220, 15%, 13%); }
.dark .dialog { background: hsl(220, 15%, 14%); }
```

---

## 9. Radius

| Token | Valor | Uso |
|-------|-------|-----|
| `--radius-sm` | 8px | Inputs, selects |
| `--radius` | 12px | Cards, dialogs |
| `--radius-lg` | 16px | Containers, modals |
| `--radius-full` | 9999px | Badges, avatars, pills |

**Regra:** manter consistência. Se cards usam 12px, modals também. Nunca misturar radius diferentes sem motivo.

---

## 10. Iconografia

### Lucide React

Biblioteca: `lucide-react` — consistente, clean, stroke-based.

| Size | Pixels | Uso |
|------|--------|-----|
| `sm` | 16px (`h-4 w-4`) | Inline com texto, badges |
| `md` | 20px (`h-5 w-5`) | Buttons, nav items |
| `lg` | 24px (`h-6 w-6`) | Headers, destaque |
| `xl` | 32px (`h-8 w-8`) | Empty states, heroes |
| `2xl` | 48px (`h-12 w-12`) | Page-level empty states |

**Regras:**
- Stroke width: 2px (default do Lucide)
- Cor: herda do texto parent (`currentColor`)
- Sempre com `aria-hidden="true"` quando decorativo
- Sempre com texto alternativo quando informativo

### Ícones por Contexto

| Ação | Ícone |
|------|-------|
| Download | `Download` |
| Cancelar | `X` ou `Trash2` |
| Refresh | `RefreshCw` |
| Voltar | `ArrowLeft` |
| Settings | `Settings` |
| Dark mode | `Moon` / `Sun` |
| Speed | `Zap` |
| Time/ETA | `Clock` |
| File | `FileVideo` / `FileAudio` |
| Storage | `HardDrive` |
| Error | `AlertCircle` |
| Success | `CheckCircle` |
| Info | `Info` |
| Link/URL | `Link` |
| Calendar | `Calendar` |

---

## 11. Padrões de Estado

### Loading States

| Contexto | Padrão |
|----------|--------|
| Página inteira carregando | `LoadingSkeleton variant="page"` |
| Lista carregando | `LoadingSkeleton variant="card" count={6}` |
| Botão processando | Spinner + "Carregando..." (text muda) |
| Dados inline | Skeleton line (`h-4 w-24 animate-pulse`) |
| Fetch de vídeo metadata | Skeleton dentro do form area |

### Error States

| Contexto | Padrão |
|----------|--------|
| Form validation | Inline abaixo do campo, `text-sm text-destructive` |
| API error | `<ErrorMessage>` com borda esquerda destructive |
| Fetch error (lista) | `<ErrorMessage>` com botão "Tentar novamente" |
| Fetch error (página) | Página centralizada com mensagem e ação |
| WebSocket disconnect | Toast com "Reconectando..." |

### Empty States

| Contexto | Padrão |
|----------|--------|
| Lista sem items | `<EmptyState>` centralizado com ícone, texto e ação |
| Busca sem resultados | `<EmptyState>` com sugestão de limpar filtros |
| Sem dados | `<EmptyState>` com ação de criar primeiro item |

---

## 12. Referência de Implementação CSS

### globals.css (TailwindCSS v4)

```css
@import "tailwindcss";

@theme {
  /* Font */
  --font-sans: "Inter", -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
  --font-mono: "JetBrains Mono", "Fira Code", monospace;

  /* Radius */
  --radius-sm: 8px;
  --radius: 12px;
  --radius-lg: 16px;

  /* Transitions */
  --transition-fast: 100ms;
  --transition-normal: 200ms;
  --transition-slow: 300ms;
  --transition-slower: 500ms;
}

/* Light Mode (default) */
:root {
  --background: 0 0% 99%;
  --foreground: 220 15% 10%;
  --card: 0 0% 100%;
  --card-foreground: 220 15% 10%;
  --primary: 172 66% 40%;
  --primary-foreground: 0 0% 100%;
  --secondary: 220 14% 96%;
  --secondary-foreground: 220 15% 30%;
  --muted: 220 14% 96%;
  --muted-foreground: 220 10% 45%;
  --accent: 172 60% 95%;
  --accent-foreground: 172 66% 30%;
  --destructive: 0 72% 51%;
  --destructive-foreground: 0 0% 100%;
  --border: 220 14% 90%;
  --input: 220 14% 90%;
  --ring: 172 66% 40%;
}

/* Dark Mode */
.dark {
  --background: 220 15% 8%;
  --foreground: 220 10% 92%;
  --card: 220 15% 11%;
  --card-foreground: 220 10% 92%;
  --primary: 172 55% 50%;
  --primary-foreground: 172 80% 8%;
  --secondary: 220 15% 15%;
  --secondary-foreground: 220 10% 75%;
  --muted: 220 15% 15%;
  --muted-foreground: 220 10% 55%;
  --accent: 172 40% 18%;
  --accent-foreground: 172 50% 80%;
  --destructive: 0 62% 55%;
  --destructive-foreground: 0 0% 100%;
  --border: 220 15% 18%;
  --input: 220 15% 18%;
  --ring: 172 55% 50%;
}

/* Base styles */
body {
  background-color: hsl(var(--background));
  color: hsl(var(--foreground));
  font-feature-settings: "rlig" 1, "calt" 1;
}

/* Tabular numbers for data */
.tabular-nums {
  font-variant-numeric: tabular-nums;
}

/* Reduced motion */
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
    scroll-behavior: auto !important;
  }
}
```

---

## Checklist de Conformidade

Antes de marcar qualquer componente como "pronto", verificar:

- [ ] Segue tokens de cor (sem hex codes hardcoded)
- [ ] Segue escala de espaçamento (múltiplos de 8px)
- [ ] Segue escala tipográfica (tokens do Tailwind)
- [ ] Contraste WCAG AA verificado
- [ ] Focus ring visível em elementos interativos
- [ ] `aria-label` em botões icon-only
- [ ] `role` e `aria-*` em elementos dinâmicos
- [ ] Loading state implementado
- [ ] Error state implementado
- [ ] Empty state implementado (se aplicável)
- [ ] Responsivo (mobile → desktop)
- [ ] Dark mode funcional
- [ ] `prefers-reduced-motion` respeitado
- [ ] Nenhum JSX duplicado (componente reutilizável)
