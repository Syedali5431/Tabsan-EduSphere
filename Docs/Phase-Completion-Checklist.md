# Phase Completion Checklist

> Run this checklist after EVERY ISO phase implementation. Nothing is done until all items are checked.

---

## Completed Phases

| Phase | Name | Status | Commit |
|-------|------|--------|--------|
| 0 | Gap Analysis | ✅ COMPLETED | — |
| 1 | Audit Logging | ✅ COMPLETED | `f6141e5` |
| 2 | Security | ✅ COMPLETED | `6d6a37f` |
| 3 | User Activity Monitoring | ✅ COMPLETED | `1dcb854`+ |
| 4 | Backup & DR | ✅ COMPLETED | `c992590`+ |
| 5 | Data Protection | ✅ COMPLETED | `1f39506`+ |
| 6 | Incident Management | ⬜ Pending | — |
| 7 | Document Management | ⬜ Pending | — |
| 8 | Backup Validation | ⬜ Pending | — |
| 9 | Data Integrity | ⬜ Pending | — |
| 10 | Compliance Dashboard | ⬜ Pending | — |

---

## After EVERY Phase Completion

### 1. Enhancement-ISO.md
- [ ] Add `### ✅ Implementation Summary` at the END of the phase section (not end of document)
- [ ] Add `### ✅ Validation Summary` at the END of the phase section
- [ ] Mark phase header as `✅ COMPLETED`
- [ ] Summaries placed at end of phase, before the next phase's `---` separator

### 2. Function-List.md
- [ ] Add ALL newly created functions in `| Function Name | Purpose | Location |` table format
- [ ] Avoid duplicate function entries — check before adding
- [ ] Remove any Implementation/Validation summaries (this file is a clean function index only)
- [ ] File location: `Docs/Function-List.md`

### 3. Project Documentation Suite — Add Phase Entry
Each of these files gets a brief update entry at the top (matching existing format). Do NOT add full ISO summaries — keep it brief and in each file's native style.

| File | Format | Location |
|------|--------|----------|
| `Functionality.md` | `## YYYY-MM-DD Update - Title` → `### Implementation sync` + `### Validation sync` | `Docs/` |
| `PRD.md` | `### YYYY-MM-DD - Product Requirements Synchronization (Title)` → bullet points | `Project startup Docs/` |
| `Modules.md` | `## Execution Update - YYYY-MM-DD (Title)` → `### Module impact summary` + `### Validation summary` | `Project startup Docs/` |
| `Development Plan - ASP.NET.md` | `## YYYY-MM-DD Update - Development Plan Synchronization (Title)` → `### Plan sync` + `### Validation sync` | `Project startup Docs/` |
| `Database Schema.md` | `## YYYY-MM-DD Update - Title (Schema Posture)` → Implementation + Validation bullet points | `Project startup Docs/` |

### 4. Git Operations
- [ ] `git add -A` — stage all changes
- [ ] Commit with message: `"ISO Phase X Completed - [Phase Name]"`
- [ ] `git push` — push to origin/main
- [ ] `git pull` — pull latest to confirm sync

---

## What Goes WHERE

| Content Type | Belongs In | Does NOT Belong In |
|-------------|-----------|-------------------|
| Full ISO Implementation Summary | `Enhancement-ISO.md` (end of phase) | Function-List.md, other docs |
| Full ISO Validation Summary | `Enhancement-ISO.md` (end of phase) | Function-List.md, other docs |
| Function names, signatures, locations | `Function-List.md` (clean table) | Enhancement-ISO.md |
| Brief phase update entry | Functionality.md, PRD.md, Modules.md, Dev Plan, DB Schema | — |
| Permanent design/business info | Functionality.md, PRD.md, Modules.md, Dev Plan, DB Schema | — |

---

## Implementation Rules (All Phases)

- **Additive only** — new tables, new columns, new indexes. No drops, no renames, no destructive changes.
- **Backward compatible** — no breaking API changes, no route removals.
- **EF Core migrations** — all schema changes through `dotnet ef migrations add`.
- **Tenant/campus isolation** — must be preserved in all changes.
- **GPA/CGPA logic** — must NOT be modified.
