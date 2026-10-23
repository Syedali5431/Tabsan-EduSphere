# Tabsan EduSphere — Accessibility Conformance Report

Based on the VPAT® 2.5 (WCAG edition) structure. Plan M, Phase M14.1.

| | |
|---|---|
| Product | Tabsan EduSphere web portal (students, teachers, administrators, parents) |
| Version | Plan M build of 2026-10-23 |
| Report date | 2026-10-23 |
| Standards | WCAG 2.1 Level A and Level AA |
| Contact | Tabsan support (accessibility issues are handled as defects) |

## Scope

- **Covered:** every page of the web portal (the 111 page actions of the portal for staff, 30 student pages, the sign-in page), in English; layout and controls are shared by the Urdu and Arabic (right-to-left) versions.
- **Not covered:** content that institutions or third parties supply (uploaded documents and videos, SCORM packages, LTI tools, xAPI activities, live-class services), the EduSphere mobile app (Plan M12; uses the platform's native controls but has not been audited), printed PDFs and Word/Excel exports, and the user-selectable dark themes (spot-checked only).

## Evaluation methods

1. **Automated:** axe-core 4.13 (via `@axe-core/playwright`) with the WCAG 2.0/2.1 A and AA rule sets on every page above, signed in as an administrator and as a student (`tests/playwright/tests/a11y/accessibility.spec.ts`, tag `@a11y`). Result on 2026-10-23: **0 violations** (from 105 before the fixes in this phase).
2. **Keyboard:** an automated keyboard walk on the dashboard, quiz builder, grading workspace, question banks and course content pages: the skip link is the first stop and reaches the content, Tab moves through every control, focus is never trapped and every focused element is visible.
3. **Review:** code review of the custom widgets (quiz builder and quiz taking, grading workspace and annotation tools, SCORM player, module editor) for names, roles, states and live regions.

Not used: testing with screen-reader users or other assistive-technology users. Automated tools find only part of the issues WCAG covers; the conformance levels below say where a criterion rests on automated or code-review evidence only.

## Conformance terms

**Supports** — the functionality meets the criterion without known defects. **Partially supports** — some functionality does not meet the criterion. **Does not support** — the majority does not. **Not applicable** — the criterion is not relevant to the product.

## WCAG 2.1 Level A

| Criterion | Level | Remarks |
|---|---|---|
| 1.1.1 Non-text content | Supports | Icons are hidden from assistive technology or named; images carry alt text; icon-only links/buttons have names (fixed in M14.1). Institution-uploaded images depend on the uploader. |
| 1.2.1 Audio-only and video-only (prerecorded) | Not applicable | The portal does not author media; uploaded media is institution content. |
| 1.2.2 Captions (prerecorded) | Not applicable | As above (course videos are institution content; the video player supports captions where the file provides them). |
| 1.2.3 Audio description or media alternative | Not applicable | As above. |
| 1.3.1 Info and relationships | Supports | Headings, lists and data tables with header cells; every form control has an accessible name tied to its visible label (`for`/`id`, or `aria-label` with the label's text for controls in repeated rows) — 470+ controls fixed in M14.1. |
| 1.3.2 Meaningful sequence | Supports | DOM order follows the visual order, also in right-to-left languages (logical CSS properties, Plan M11). |
| 1.3.3 Sensory characteristics | Supports | Instructions do not rely on shape, size or position alone. |
| 1.4.1 Use of color | Supports | Status uses text as well as colour (badges carry words); links inside running text are underlined (M14.1). |
| 1.4.2 Audio control | Not applicable | No audio plays automatically. |
| 2.1.1 Keyboard | Partially supports | All pages and controls work with the keyboard. Exception: drawing area/freehand annotations on a submission page needs a pointer; teachers can give the same feedback by keyboard as text comments and rubric scores. |
| 2.1.2 No keyboard trap | Supports | Verified by the keyboard walk; modal dialogs (Bootstrap) return focus and close with Esc. |
| 2.1.4 Character key shortcuts | Supports | No single-key shortcuts. |
| 2.2.1 Timing adjustable | Partially supports | Timed quizzes enforce the teacher's time limit; extra time can be granted per student (accommodations, Plan M2.5). The sign-in session expires after inactivity without an advance warning. |
| 2.2.2 Pause, stop, hide | Supports | Notification toasts can be dismissed; nothing moves for more than five seconds without control. |
| 2.3.1 Three flashes or below | Supports | No flashing content. |
| 2.4.1 Bypass blocks | Supports | "Skip to main content" is the first focusable element on every page. |
| 2.4.2 Page titled | Supports | Every page has a descriptive, translated title. |
| 2.4.3 Focus order | Supports | Focus follows the reading order (keyboard walk). |
| 2.4.4 Link purpose (in context) | Supports | Links have descriptive text or names. |
| 2.5.1 Pointer gestures | Supports | No multipoint or path-based gestures are required (annotation drawing has the keyboard alternative above). |
| 2.5.2 Pointer cancellation | Supports | Actions fire on click (up-event). |
| 2.5.3 Label in name | Supports | Accessible names repeat the visible label text. |
| 2.5.4 Motion actuation | Not applicable | No motion-operated functions. |
| 3.1.1 Language of page | Supports | `<html lang>` and `dir` follow the chosen language (en, ur, ar). |
| 3.2.1 On focus | Supports | Focus never changes context. |
| 3.2.2 On input | Partially supports | Some filter drop-downs (institution, campus, department, semester) reload the page when changed; the behaviour is consistent across the portal but not announced beforehand. |
| 3.3.1 Error identification | Supports | Validation errors are shown in text next to the field and in a summary. |
| 3.3.2 Labels or instructions | Supports | All controls are labelled; formats are explained in hints. |
| 4.1.1 Parsing | Supports | (Obsolete in WCAG 2.2; no duplicate ids or ARIA errors reported.) |
| 4.1.2 Name, role, value | Supports | Native controls throughout; custom widgets (viewer toolbar with `aria-pressed`, live status regions, labelled regions in the grading workspace and submission viewer) checked by axe and review. |

## WCAG 2.1 Level AA

| Criterion | Level | Remarks |
|---|---|---|
| 1.2.4 Captions (live) | Not applicable | Live classes run in BigBlueButton, Zoom or Teams, which provide their own captions. |
| 1.2.5 Audio description (prerecorded) | Not applicable | Institution content. |
| 1.3.4 Orientation | Supports | Layouts work in portrait and landscape. |
| 1.3.5 Identify input purpose | Supports | Sign-in fields use `autocomplete` (username, current-password). Other personal fields have not been reviewed. |
| 1.4.3 Contrast (minimum) | Partially supports | Default (light) themes meet 4.5:1 for text — two low-contrast cases fixed in M14.1. The four dark themes were spot-checked only. Branding colours chosen by an institution may lower contrast; the Dashboard Settings preview shows the result. |
| 1.4.4 Resize text | Supports | Relative units and a responsive layout (code review; not measured at 200% zoom on every page). |
| 1.4.5 Images of text | Supports | No images of text except logos. |
| 1.4.10 Reflow | Supports | Responsive layout down to phone width (code review); wide data tables scroll horizontally inside their own region (allowed exception). |
| 1.4.11 Non-text contrast | Partially supports | Focus indicator strengthened to a 3 px solid ring (M14.1); form-field borders and chart colours have not been measured systematically. |
| 1.4.12 Text spacing | Supports | No fixed-height text containers (code review). |
| 1.4.13 Content on hover or focus | Supports | Tooltips are dismissible and stay while hovered. |
| 2.4.5 Multiple ways | Supports | Sidebar navigation, search and dashboard links. |
| 2.4.6 Headings and labels | Supports | Descriptive headings and labels. |
| 2.4.7 Focus visible | Supports | Keyboard focus shows a 3 px ring (M14.1); verified visible on every focus stop in the keyboard walk. |
| 3.1.2 Language of parts | Supports | Language names in the language switcher carry their own `lang`. |
| 3.2.3 Consistent navigation | Supports | The same sidebar and top bar on every page. |
| 3.2.4 Consistent identification | Supports | Same names for the same functions. |
| 3.3.3 Error suggestion | Supports | Messages say how to correct the input. |
| 3.3.4 Error prevention (legal, financial, data) | Supports | Destructive and submitting actions ask for confirmation; quiz and assignment submissions can be reviewed before they are final; grades can be corrected. |
| 4.1.3 Status messages | Supports | Toasts and status areas use live regions (`role="status"`/`aria-live`). |

## Known limitations and plan

| Item | Plan |
|---|---|
| Area/freehand annotation needs a pointer | Keyboard placement of annotations (arrow keys + Enter) in a later release. |
| Session time-out without warning | Add a warning with "stay signed in" two minutes before expiry. |
| Dark themes and institution branding colours | Run the axe audit per theme and warn in Dashboard Settings when chosen colours fail 4.5:1. |
| No assistive-technology user testing yet | Test with NVDA (Windows), VoiceOver (iOS/macOS) and TalkBack (Android) before the next report. |
| Mobile app | Separate audit of the MAUI app (TalkBack/VoiceOver labels, dynamic type). |

## Keeping this report current

Run `npx playwright test tests/a11y --project=chromium` with `SUPER_ADMIN_USERNAME/PASSWORD` and `STUDENT_USERNAME/PASSWORD` set (against the Release portal and the local fake API, or a test environment). The test fails on any serious or critical violation and writes the full findings to `A11Y_REPORT` (default `test-results/a11y-report.json`). New pages: add them to `tests/playwright/tests/a11y/portal-pages.json`.
