# Phase 40 AI Chatbot UI Redesign

## Details

This change replaces the previous bottom-right AI chat link with a modern floating chatbot widget in the shared portal layout. The redesign keeps the existing AI backend and existing full-page fallback route intact while moving the primary experience to a floating assistant panel that is available across portal pages.

The widget is implemented in the web layer only. Existing API endpoints under `api/ai/*` were reused without backend behavior changes.

The follow-up iteration refined the launcher to a more compact reference-inspired floating icon, added recent conversation switching inside the popup, and corrected the web client AI response mapping so the widget reads the actual API payload shape instead of relying on mismatched fields.

## Implementation Summary

- Replaced the old pill-style launcher in the shared layout with a floating assistant button and popup panel.
- Added a conversation-aware widget flow that keeps the active conversation ID in browser session storage so closing and reopening the panel does not reset the current chat.
- Added lightweight web JSON endpoints in `PortalController` for widget state loading and async message sending.
- Corrected the web client AI send contract so the returned `ConversationId` is preserved after the first message.
- Corrected AI conversation list and detail mapping in the web client to align with `UserRole`, `StartedAt`, `SentAt`, and nested `Messages` payloads returned by the API.
- Kept `Portal/AiChat` as a fallback route for direct navigation and no-JavaScript behavior.
- Added a `New` action in the widget header to start a fresh conversation from the floating UI.
- Added in-panel recent conversation switching so the user can reopen prior chats without leaving the floating assistant.
- Refined the floating launcher and popup composition to a more minimal compact style closer to the supplied reference while keeping responsive behavior for desktop and mobile.
- Added responsive styling and motion while keeping the widget isolated from the rest of the portal layout.

## Files Updated

- `src/Tabsan.EduSphere.Web/Views/Shared/_Layout.cshtml`
- `src/Tabsan.EduSphere.Web/Views/Shared/_Layout.cshtml.css`
- `src/Tabsan.EduSphere.Web/wwwroot/js/site.js`
- `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs`
- `src/Tabsan.EduSphere.Web/Services/EduApiClient.cs`
- `src/Tabsan.EduSphere.Web/Models/Portal/PortalViewModels.cs`

## Validation Summary

- Verified the shared layout, controller, web client, and widget script files reported no editor diagnostics after the change.
- Verified no remaining `TODO`, stub, or `NotImplementedException` markers were left in the AI chat files touched for this redesign.
- Confirmed the widget flow compiles with the rest of the solution.
- Confirmed the redesign did not introduce solution-level test regressions.

## Testing Results

### Build

- Command: `dotnet build Tabsan.EduSphere.sln -v minimal`
- Result: Passed

### Regression Tests

- Command: `dotnet test Tabsan.EduSphere.sln -v minimal --no-build`
- Result: Passed
- Total tests: 388
- Passed: 388
- Failed: 0
- Skipped: 0
- Duration: 35.0s

## Notes

- The old visible AI launcher was removed from the shared layout and replaced by the floating widget.
- The dynamic sidebar was already suppressing the `ai_chat` menu entry before this change and was left unchanged.
- No AI application service or API controller logic was modified.
- The remaining TODO-style hits in the repo are either vendor library comments or intentional startup placeholder guards, not unfinished application work.