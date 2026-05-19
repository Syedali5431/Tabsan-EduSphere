# Front-End Fixing

## Phase 1 - Base Layout Recovery
- Scope:
  - removed conflicting layout-shell overrides from scoped layout CSS,
  - repaired malformed theme/body CSS blocks that were breaking downstream styles,
  - normalized Rooms page structure using container/grid spacing and card grouping.

### Implementation Summary
- Updated `src/Tabsan.EduSphere.Web/Views/Shared/_Layout.cshtml.css` to keep only chat-widget presentation concerns and stop overriding global shell/header/sidebar behavior.
- Updated `src/Tabsan.EduSphere.Web/wwwroot/css/site.css` malformed sections so global style parsing correctly applies sidebar/header/profile/navigation classes.
- Updated `src/Tabsan.EduSphere.Web/Views/Portal/Rooms.cshtml` with structured container/row/col sections and spacing utilities (`g-4`, `mb-3`, `pe-xl-4`, grouped action rows).

### Validation Summary
- UI-only changes applied with no controller/service/API modifications.
- `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -c Debug`: succeeded.
- `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~Phase36Stage4HealthAndLicenseGateTests"`: passed (3/3).

## Phase 2 - Chat And Header Alignment Stabilization
- Scope:
  - verify AI assistant panel/fab positioning across desktop/mobile,
  - fix any residual overlap against Rooms forms and header chips,
  - keep existing design tokens and classes.

### Implementation Summary
- Pending.

### Validation Summary
- Pending.

## Phase 3 - Final Responsive Pass And Regression Validation
- Scope:
  - run final responsive spacing/alignment pass,
  - confirm no raw controls or overlapping sections remain,
  - final web build/tests and repo sync.

### Implementation Summary
- Pending.

### Validation Summary
- Pending.
