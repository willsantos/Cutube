# Phase 1: Specify

**Goal**: Capture WHAT to build with testable requirements.

## Process

### 1. Clarify Requirements

Ask the user (2-3 questions to start):
- "What problem are you solving?"
- "Who is the user and what's their pain?"
- "What does success look like?"

If needed:
- "What are the constraints (time, tech, resources)?"
- "What is explicitly out of scope?"

### 2. Capture User Stories with Priorities

**P1 = MVP** (must ship), **P2** (should have), **P3** (nice to have)

Each story MUST be **independently testable** - you can implement and demo just that story.

### 3. Write Acceptance Criteria

Use **WHEN/THEN/SHALL** format - it's precise and testable:
- WHEN [event/action] THEN [system] SHALL [response/behavior]

---

## Template: `.specs/[feature]/spec.md`

```markdown
# [Feature Name] Specification

## Problem Statement

[Describe the problem in 2-3 sentences. What pain point are we solving? Why now?]

## Goals

- [ ] [Primary goal with measurable outcome]
- [ ] [Secondary goal with measurable outcome]

## Out of Scope

- [Explicitly NOT building: X]
- [Explicitly NOT building: Y]

---

## User Stories

### P1: [Story Title] ⭐ MVP

**User Story**: As a [role], I want [capability] so that [benefit].

**Why P1**: [Why this is critical for MVP]

**Acceptance Criteria**:
1. WHEN [user action/event] THEN system SHALL [expected behavior]
2. WHEN [user action/event] THEN system SHALL [expected behavior]
3. WHEN [edge case] THEN system SHALL [graceful handling]

**Independent Test**: [How to verify this story works alone - e.g., "Can demo by doing X and seeing Y"]

---

### P2: [Story Title]

**User Story**: As a [role], I want [capability] so that [benefit].

**Why P2**: [Why this isn't MVP but important]

**Acceptance Criteria**:
1. WHEN [event] THEN system SHALL [behavior]
2. WHEN [event] THEN system SHALL [behavior]

**Independent Test**: [How to verify]

---

### P3: [Story Title]

**User Story**: As a [role], I want [capability] so that [benefit].

**Why P3**: [Why this is nice-to-have]

**Acceptance Criteria**:
1. WHEN [event] THEN system SHALL [behavior]

---

## Edge Cases

- WHEN [boundary condition] THEN system SHALL [behavior]
- WHEN [error scenario] THEN system SHALL [graceful handling]
- WHEN [unexpected input] THEN system SHALL [validation response]

---

## Success Criteria

How we know the feature is successful:
- [ ] [Measurable outcome - e.g., "User can complete X in < 2 minutes"]
- [ ] [Measurable outcome - e.g., "Zero errors in Y scenario"]
```

---

## Tips

- **P1 = Vertical Slice** - A complete, demo-able feature, not just backend or frontend
- **WHEN/THEN is code** - If you can't write it as a test, rewrite it
- **Edge cases matter** - What breaks? What's empty? What's huge?
- **Confirm before Design** - User must approve spec before moving on

---

## Epic/Macro Specs

**When to use**: migrations, repo splits, multi-app initiatives — anything too
big for one feature spec, where the deliverable of planning is a list of
**features** (each later becoming its own spec/design/tasks), not tasks.

**Key differences from feature specs**:

| Aspect | Feature spec | Epic/macro spec |
|--------|--------------|-----------------|
| Stories | Independently testable user stories | Tracks/product increments containing features |
| Acceptance criteria | WHEN/THEN per story | WHEN/THEN per track; features listed with one-line scope |
| Output | `spec.md` → design → tasks | `spec.md` only; each feature spawns `.specs/[epic]/[feature]/` later |
| Decisions | Tech decisions go to design.md | Product/architecture decisions MUST be recorded here (status: decided/open) |

**Process**:
1. Clarify scope: what exists today, what moves where, what is new.
2. Ask the user about genuine product/architecture decisions (tech choices,
   repo strategy, fate of branches). Record every decision and its status —
   never bury a provisional default inside a story.
3. Define tracks (parallel workstreams) and the features inside each.
4. Define ordering constraints between tracks (e.g. "preserve history before
   deleting branches").
5. User must approve the macro spec before any child feature spec starts.

**Template**: `.specs/[epic-slug]/spec.md`

```markdown
# [Epic Name] Macro Specification

## Problem Statement
[2-3 sentences: the pain, why now]

## Goals
- [ ] [Measurable outcome]

## Out of Scope
- [Explicitly NOT doing: X]

## Decisions
| # | Decision | Choice | Status |
|---|----------|--------|--------|
| D1 | [e.g. desktop tech] | [e.g. Tauri] | decided | open |
> Status: `decided` (user confirmed) or `open` (provisional default — review
> before Design phase).

## Current State
[What exists today — projects, branches, working functionality that must survive]

## Tracks

### Track A: [Name]
**Goal**: [one sentence]
**Features**:
- A1 `slug`: [one-line scope]
- A2 `slug`: [one-line scope]

**Acceptance Criteria**:
1. WHEN [event] THEN [track] SHALL [outcome]

### Track B: [Name]
[same structure]

## Ordering Constraints
- [e.g. B1 before A4: history must be preserved before branch reset]

## Cross-cutting Edge Cases
- WHEN [scenario] THEN [repos] SHALL [handling]

## Success Criteria
- [ ] [Measurable, verifiable per repo]
```
