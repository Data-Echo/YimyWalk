# Cross-display Walking and Landing Safety Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task with review checkpoints.

**Goal:** Allow pets to walk across the virtual desktop with global edge wrapping and prevent maximized windows from blocking pets at the top of the screen.

**Architecture:** Keep display geometry and walking rules in Core. Add a global horizontal travel span to the display snapshot/world-surface contracts, while retaining per-display working-area floors for vertical physics. Mark maximized Windows candidates in the App adapter and reject them in the platform-neutral surface selector.

**Tech Stack:** .NET 8, C#, xUnit Core tests, WinForms/Win32 display and window inspection.

---

### Task 1: Add failing Core geometry and surface tests

**Files:**
- Modify: `tests/YimyWalk.Core.Tests/DisplayContractTests.cs`
- Modify: `tests/YimyWalk.Core.Tests/PetPhysicsTests.cs`
- Modify: `tests/YimyWalk.Core.Tests/SurfaceSelectionTests.cs`

- [x] Add a display snapshot test asserting the global horizontal travel bounds use the minimum working-area left and maximum working-area right across two displays.
- [x] Add physics tests that a walking pet crossing the global right bound appears at the global left bound, a pet crossing the global left bound appears at the global right bound, and a pet can move from the first display range into the second before wrapping.
- [x] Add a surface-selection test asserting a candidate with `IsMaximized=true` falls back to the floor while an otherwise identical ordinary candidate remains selectable.
- [x] Run `dotnet test YimyWalk.sln --configuration Release --no-restore`; confirm the new tests fail because the new contract and behavior do not exist yet.

### Task 2: Implement global horizontal travel geometry

**Files:**
- Modify: `src/YimyWalk.Core/Display/DisplayContracts.cs`
- Modify: `src/YimyWalk.Core/Simulation/SimulationContracts.cs`
- Modify: `src/YimyWalk.Core/Simulation/PetEngine.cs`

- [x] Add a `HorizontalTravelBounds` value to `DisplaySnapshot`, returning a valid bounding `WorldRect` or no value when no displays are available.
- [x] Add `IWorldSurface.HorizontalTravelBounds` with a default of `WorkingArea`, preserving existing fake surfaces and fallback behavior.
- [x] Change walking integration to use the global horizontal bounds for x, wrap at the two global edges, and continue using the selected display surface for y and vertical state.
- [x] Keep drag clamping, airborne clamping, display-loss relocation, and per-display floor selection unchanged.
- [x] Run the focused display and physics tests, then the complete Core suite; confirm all pass.

### Task 3: Reject maximized window surfaces

**Files:**
- Modify: `src/YimyWalk.Core/Display/SurfaceSelection.cs`
- Modify: `src/YimyWalk.App/Windows/NativeMethods.cs`
- Modify: `src/YimyWalk.App/Host/WindowSurfaceResolver.cs`

- [x] Append an optional `IsMaximized` field to `WindowSurfaceCandidate` so existing callers remain source-compatible.
- [x] Make `SurfaceSelector` skip maximized candidates alongside hidden, minimized, stale, excluded, and occluded candidates.
- [x] Add the `IsZoomed` Win32 import and mark candidates maximized when the native state is true or when the candidate covers its resolved display working area.
- [x] Keep candidate refresh, occlusion ordering, live checks, and user-window immutability unchanged.
- [x] Run surface-selection tests and the App Release build; confirm no warnings or errors.

### Task 4: Add targeted Windows smoke coverage

**Files:**
- Modify: `tools/DesktopSmoke/Program.cs` or the smallest existing desktop smoke harness that can host a probe window and inspect the host session.
- Modify: `scripts/desktop-smoke.ps1` only if a targeted option is required.

- [x] Add a maximized probe assertion that the resolver reports the probe as maximized and surface selection does not choose it.
- [x] Add a two-display targeted walk assertion only when the current provider exposes at least two displays; otherwise print an explicit `SKIP` and do not claim cross-display Windows verification.
- [x] Do not rerun the existing 83/83 dual-display matrix solely because of this change.
- [x] Run the available targeted smoke and record whether the current topology exercised it.

### Task 5: Verify, document, and commit

**Files:**
- Modify: `docs/STATUS.md`

- [x] Run Core tests, App build, publish/package checks, and the available targeted Windows smoke.
- [x] Run `git diff --check` and inspect the final diff for unintended changes.
- [x] Update `docs/STATUS.md` with separate code, build, Core, Windows desktop verified, and Windows desktop unverified statements.
- [ ] Commit the implementation and documentation with a focused message.
