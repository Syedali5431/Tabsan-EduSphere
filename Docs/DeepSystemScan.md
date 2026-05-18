I want you to perform a FULL SYSTEM AUDIT + VALIDATION + UI ENHANCEMENT of this project.

You MUST analyze the project deeply based on:
- PRD.md
- Complete-Functionality-Reference.md
- Consolidated-Execution-Enhancements-Issues.md

The system is a large ASP.NET Core enterprise application with:
- 60+ controllers
- 50+ services
- RBAC, notifications, reports, AI chat, etc.

Your goal is to:
1. Verify EVERYTHING is working correctly
2. Detect gaps, bugs, inconsistencies
3. Suggest and implement fixes where needed
4. Replace the existing AI chatbot UI with a modern floating chatbot

---

# ✅ PHASE 1 — SYSTEM UNDERSTANDING (MANDATORY)

- Scan the entire codebase and map it to documented functionality
- Cross-check:
  - Controllers → Services → Repositories
  - DTOs → API Contracts → UI bindings
- Identify:
  - Missing implementations
  - Broken mappings
  - Outdated or unused code
- Validate module coverage:
  - Academic
  - Enrollment
  - Notifications (Email/SMS)
  - Reports & Analytics
  - Authentication & MFA
  - AI Chat module

Expected Output:
- List of mismatches between docs and code
- Missing or partially implemented features
- Risk areas

---

# ✅ PHASE 2 — API & BACKEND VALIDATION

Perform deep validation of:

## 2.1 API correctness
- Verify all endpoints:
  - Proper routes (/api/v1 consistency)
  - Correct HTTP verbs
  - Validation handling
- Check:
  - Request DTO vs Response DTO match
  - Error handling consistency

## 2.2 Business logic
- Ensure services enforce rules described in PRD:
  - Enrollment rules (waitlist, capacity)
  - Result calculation
  - Role-based authorization
  - Notification triggers

## 2.3 Notifications system
- Validate:
  - Email flow
  - SMS flow (PhoneNumber usage)
  - Fan-out logic
- Ensure:
  - No silent failures
  - Proper async handling

## 2.4 Authentication & Security
- Validate:
  - JWT token handling
  - MFA (TOTP + recovery codes)
  - Session handling
- Ensure:
  - No bypass scenarios
  - Proper role checks

Output:
- Bugs / vulnerabilities
- Fix recommendations
- Refactored code if needed

---

# ✅ PHASE 3 — DATABASE & DATA FLOW VALIDATION

- Check:
  - Entity relationships
  - EF Core configuration
  - Index usage
  - Query filters

- Validate:
  - No missing Includes
  - No N+1 query issues
  - No broken FK relationships

- Verify:
  - Migration consistency
  - Schema matches PRD

Output:
- Data integrity issues
- Query performance issues
- Fixes

---

# ✅ PHASE 4 — UI / FRONTEND VALIDATION

- Check all portal pages:
  - Sidebar behavior
  - API data binding
  - Role-based visibility

- Validate:
  - Data loading issues
  - Forms & CRUD operations
  - Error handling display

- Detect:
  - Broken pages
  - Empty dropdowns
  - Missing filters

Output:
- List of UI bugs
- Fix implementation

---

# ✅ PHASE 5 — PERFORMANCE & STABILITY CHECK

- Validate:
  - Caching usage
  - Background jobs
  - Async/await correctness
- Detect:
  - Blocking calls (.Result / .Wait)
  - Memory leaks risks
  - Inefficient queries

Output:
- Performance issues
- Suggested optimizations

---

# ✅ PHASE 6 — AI CHATBOT REDESIGN (CRITICAL REQUIREMENT)

Replace the EXISTING chatbot UI completely.

## New chatbot must:

### 6.1 Floating button
- Fixed bottom-right corner
- Circular
- Modern UI (like ChatGPT / Copilot / Intercom)
- Smooth hover animation

### 6.2 Chat panel
- Opens on click
- Slide/fade animation
- Includes:
  - Header: "AI Assistant"
  - Close (X)
  - Scrollable messages
  - Input + send button

### 6.3 Messages UI
- User → right aligned
- AI → left aligned
- Nice bubble styling
- Auto-scroll to latest message

### 6.4 Integration
- MUST reuse existing AI Chat backend (AiChat module)
- DO NOT break API
- Ensure message history works

### 6.5 Behavior
- Maintain state when opened/closed
- Responsive design (mobile + desktop)

### 6.6 Code requirements
- Remove old chatbot entry points (menu/button)
- Add:
  - FloatingChatButton component
  - ChatPanel component
- Clean modular code

Output:
- Full implementation (HTML + CSS + JS / Razor)
- Integration instructions
- Modified files list

---

# ✅ PHASE 7 — FINAL CONSOLIDATED REPORT

Provide:

## ✅ System Health Report
- ✅ Working features
- ⚠️ Issues found
- ❌ Broken features

## ✅ Fix Summary
- Code changes applied
- Files updated

## ✅ Risk Analysis
- Security risks
- Data risks
- Performance risks

## ✅ Chatbot Upgrade Summary
- Old vs new UI
- Improvements

---

# ✅ IMPORTANT RULES

- Do NOT give generic answers — analyze REAL code
- Do NOT skip phases
- Be specific with file names and fixes
- Provide actual working code wherever needed
- Follow existing project architecture strictly

---

# ✅ GOAL

Ensure the entire system:
- Works correctly
- Matches documented functionality
- Has zero critical bugs
- Includes modern floating AI chatbot UI