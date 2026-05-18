I want a COMPLETE UI/UX redesign and visual enhancement of this application so it looks like a professional, modern SaaS product suitable for schools, colleges, and universities.

🚫 CRITICAL RULE (MUST FOLLOW STRICTLY):
- DO NOT modify business logic
- DO NOT modify backend code
- DO NOT change API endpoints or controller logic
- DO NOT break or refactor existing functionality
- DO NOT change database models or services
- ONLY improve UI, styling, layout, and frontend visuals

✅ Focus ONLY on:
- CSS
- HTML / Razor layout
- UI components
- Styling improvements
- Minor frontend JS (only for visual behavior like animations, toggles, UI effects)

---

# ✅ PHASE 1 — GLOBAL DESIGN SYSTEM

Create a modern, clean design system:

1. Colors:
   - Professional academic theme (blue, white, subtle accents)
   - Consistent palette across app

2. Typography:
   - Use modern font (Inter / Segoe UI / Roboto)
   - Clear hierarchy (title, subtitle, body)

3. Layout:
   - Add spacing consistency (padding/margin)
   - Use cards instead of plain layouts
   - Improve alignment everywhere

4. Components:
   - Styled buttons (primary, secondary, danger)
   - Clean inputs and dropdowns
   - Better tables and modals

---

# ✅ PHASE 2 — DASHBOARD REDESIGN

Improve dashboard visually ONLY:

- Add cards (Students, Courses, Revenue, etc.)
- Add icons and colors
- Improve layout grid
- Add clean sections (no clutter)

🚫 DO NOT change data source or backend logic

---

# ✅ PHASE 3 — SIDEBAR & NAVIGATION

- Add icons to menu
- Improve spacing and grouping
- Highlight active menu
- Add smooth hover effects
- Optional: collapsible sidebar

🚫 Do NOT change routing or menu logic

---

# ✅ PHASE 4 — PAGE UI IMPROVEMENTS

Improve visuals across pages:

1. Tables:
   - Modern design
   - Better spacing
   - Hover rows
   - Sticky headers (if possible)

2. Forms:
   - Align inputs properly
   - Add cleaner labels
   - Improve spacing

3. Modals:
   - Better design
   - Smooth animations

4. Empty states:
   - Friendly messages
   - Icons/illustrations

🚫 Do NOT change form submission logic or APIs

---

# ✅ PHASE 5 — UX IMPROVEMENTS

- Add loaders (spinners/skeletons)
- Add toast notifications (only UI)
- Improve error display styling
- Add smooth transitions

🚫 Do NOT change backend validation

---

# ✅ PHASE 6 — BRANDING

- Improve header layout
- Add logo and institution name styling
- Improve user profile dropdown UI
- Improve notification icon UI

---

# ✅ PHASE 7 — AI CHATBOT UI (IMPORTANT)

Enhance chatbot UI ONLY (no backend change):

- Floating button (bottom-right)
- Modern chat panel design
- Styled messages (user vs AI)
- Smooth open/close animation

🚫 MUST reuse existing AI Chat backend (AiChat module)
🚫 DO NOT change chatbot API logic

---

# ✅ PHASE 8 — RESPONSIVE DESIGN

- Make UI responsive for:
  - Mobile
  - Tablet
  - Desktop

---

# ✅ PHASE 9 — FINAL UI POLISH

- Ensure consistent spacing
- Align all components
- Remove visual inconsistencies
- Improve overall professional look

---

# ✅ OUTPUT REQUIREMENTS

Provide:

1. Updated UI code only (HTML / Razor / CSS / minimal JS)
2. NO backend changes
3. List of modified frontend files
4. Clear instructions

---

# ✅ GOAL

The application should:

- Look like a PREMIUM SaaS product
- Be clean, modern, and attractive
- Impress clients (schools, colleges, universities)
- Maintain full existing functionality WITHOUT BREAKING ANYTHING

---

## 2026-05-18 Implementation Summary

- Reworked the shared portal shell into a cleaner SaaS-style layout with upgraded header, glassmorphism top bar, stronger branding treatment, icon-led navigation, active-menu highlighting, and mobile sidebar behavior.
- Replaced the global stylesheet with a unified academic design system covering typography, color palette, spacing, cards, buttons, forms, tables, modals, responsive behavior, loaders, toast visuals, and AI chatbot presentation.
- Redesigned the dashboard view into a premium hero-plus-card layout while preserving the existing connection form submission flow and route behavior.
- Added minimal frontend-only JavaScript enhancements for page loader fade-out, visual toast surfacing from existing alerts/validation messages, and responsive sidebar interactions.

## 2026-05-18 Validation Summary

- `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed after the Razor/JS shell redesign changes.
- `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed again after the global CSS design-system replacement.
- Workspace diagnostics reported no errors in `_Layout.cshtml`, `Dashboard.cshtml`, `site.js`, or `site.css`.
- Backend/controller/API/database logic was left unchanged; validation scope was limited to frontend compilation and diagnostics.