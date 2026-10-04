# YimyWalk M2 Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the verified M1 desktop baseline into a maintainable M2 foundation with atomic role refresh, persistent settings, display/DPI abstractions, optional window-top landing, and honest multi-instance regression evidence.

**Architecture:** Keep `YimyWalk.Core` platform-neutral and testable without windows or desktop APIs. Add immutable display/surface contracts and refresh/settings models to Core; keep WinForms, `Screen`, `HWND`, DPI calls, layered bitmaps, and top-level window enumeration in `YimyWalk.App`. Preserve the M1 mapping of one pet instance to one layered window managed by one host process. A failed refresh keeps the last good role; a successful refresh affects only new instances, while leases keep old definitions and renderer resources alive until their last user releases them.

**Tech Stack:** .NET 8, C# 12, `net8.0` Core, `net8.0-windows` WinForms App, ImageSharp PNG decoding, Win32 display/window APIs, xUnit core tests, PowerShell desktop smoke scripts.

**Approved boundaries:** M2 may add role refresh, settings persistence/recovery, display/DPI adapters, window-top landing, and multi-instance measurements. M3 hunger, bubbles, external application events, real shortcut manipulation, and social/breeding remain out of scope. The second-display/DPI desktop matrix is explicitly **暂缓** because the current session exposes only `DISPLAY1`; code and core tests may proceed, but no real multi-display pass may be claimed until hardware is available.

---

## Current evidence and decisions

- M0 reuse decision remains the fixed eSheep snapshot `e2933305157a34818f47d238c74607593d81816d`; only window/motion/cache/test ideas are reused. No upstream runtime migration is planned.
- M1 uses one transparent layered window per pet, one host process, shared immutable role definitions/RGBA frames, platform `Bitmap` resources in `FrameSurfaceCache`, and `DefinitionLease` for definition/renderer lifetime.
- M1 real desktop input, tray menu item clicks, 1/10/50/100 creation and clearing, `cat`/`slime` sprite-only loading, and 20-round resource baseline are recorded in `docs/STATUS.md`. The baseline does not promise 100-instance long-term smoothness.

## File map

- Modify: `src/YimyWalk.Core/Characters/CharacterCatalog.cs`, `src/YimyWalk.Core/Simulation/PetSession.cs`, `src/YimyWalk.Core/Simulation/SimulationContracts.cs` for refresh and display contracts.
- Create: `src/YimyWalk.Core/Display/DisplayContracts.cs`, `src/YimyWalk.Core/Settings/SettingsDocument.cs` for pure data contracts and validation.
- Create: `tests/YimyWalk.Core.Tests/DisplayContractTests.cs`, `tests/YimyWalk.Core.Tests/SettingsDocumentTests.cs`; extend `CharacterCatalogTests.cs` and `ResourceLifecycleTests.cs`.
- Modify: `src/YimyWalk.App/Host/AppHost.cs`, `DesktopWorldSurface.cs`, `PetWindow.cs`, `PetWindowRegistry.cs`, `Rendering/FrameSurfaceCache.cs`, and `UI/ManagerForm.cs` for refresh, display mapping, landing and settings lifecycle.
- Create: `src/YimyWalk.App/Host/WindowsDisplayAdapter.cs`, `src/YimyWalk.App/Host/WindowSurfaceResolver.cs`, `src/YimyWalk.App/Settings/SettingsStore.cs`.
- Modify: `tools/DesktopInputSmoke/Program.cs`, `tools/DesktopSmoke/Program.cs`, `scripts/desktop-input-smoke.ps1`, `scripts/desktop-smoke.ps1`, `docs/ARCHITECTURE.md`, `docs/TESTING.md`, and `docs/STATUS.md` as each task produces evidence.

## Task 1: Make role refresh semantics explicit and testable

**Files:**
- Modify: `src/YimyWalk.Core/Characters/CharacterCatalog.cs`, `src/YimyWalk.Core/Simulation/PetSession.cs`.
- Create: `tests/YimyWalk.Core.Tests/CharacterRefreshTests.cs`.
- Test: existing `CharacterCatalogTests.cs`, `ResourceLifecycleTests.cs`, `PetSessionTests.cs`.

- [x] **Step 1: Add failing tests for refresh identity and failure retention.**

Create two temporary role directories with distinct sprite pixels. Assert that a successful `CharacterCatalog.Scan` replaces the catalog entry, a new `PetSession.Create` uses the new definition, and an existing `PetInstance.Definition` remains the old definition. Corrupt the new sprite and assert that `TryGet` still returns the last valid definition and `LastFailures` reports the directory. Assert that a removed role is no longer offered to new instances while existing instances remain valid until cleared.

- [x] **Step 2: Run the focused tests and confirm the missing behavior.**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-yimywalk.ps1
```

Expected: the new refresh tests fail only where the current catalog/session integration does not provide the asserted replacement behavior; existing 41 tests must remain green.

- [x] **Step 3: Implement the smallest refresh contract.**

Keep `CharacterCatalog.Scan` atomic: build `next` definitions and leases off to the side, retain the previous valid entry on a package failure, swap the dictionaries once, and dispose only definitions no longer referenced. Add a `PetSession.ReplaceDefinition(string id, CharacterDefinition definition)` path that registers the new lease without mutating existing `PetInstance.Definition` references. Do not add hot-switching or background file watching.

- [x] **Step 4: Run focused and full tests.**

Run `dotnet test tests/YimyWalk.Core.Tests/YimyWalk.Core.Tests.csproj --configuration Release --filter FullyQualifiedName~Refresh`, then `scripts/test-yimywalk.ps1`. Expected: refresh tests and all core tests pass; no Core assembly reference to WinForms or WindowsBase is introduced.

- [x] **Step 5: Commit the isolated core change.**

```powershell
git add src/YimyWalk.Core tests/YimyWalk.Core.Tests
git commit -m "feat: formalize role refresh lifetimes"
```

## Task 2: Add an explicit reload entry point and UI feedback

**Files:**
- Modify: `src/YimyWalk.App/Host/AppHost.cs`, `src/YimyWalk.App/UI/ManagerForm.cs`, `src/YimyWalk.App/UI/TrayIconHost.cs`.
- Create/modify: `tests` only through the existing desktop smoke harness; no fake GUI pass is accepted.

- [x] **Step 1: Add a reload command that reports failures.**

Implement `AppHost.ReloadCharacters()` to rescan `CharacterRoot`, replace only successful role definitions in the session, leave existing instances bound to their old definition, refresh the manager list, and return a read-only failure summary. Keep the previous catalog entry when a package is invalid. Add a manager `刷新角色` button and a status label that shows the number of loaded roles and failed directories; do not show a button that has no implementation.

- [x] **Step 2: Wire lifecycle tests through a real package change.**

Extend a Windows smoke invocation to create `cat`, modify a copy of a role in a temporary workspace, call reload, create a new instance, and assert old/new definition identity through the host reflection boundary. Restore the package before process exit. If the environment cannot safely run this GUI step, mark it unverified and keep the core refresh tests as the evidence.

- [x] **Step 3: Build and run regression checks.**

Run `scripts/test-yimywalk.ps1`, `scripts/build-yimywalk.ps1`, and `scripts/desktop-input-smoke.ps1`. Expected: existing input routing remains unchanged and invalid refresh never disposes a definition still used by a pet or renderer.

- [x] **Step 4: Commit.**

```powershell
git add src/YimyWalk.App tests tools scripts
git commit -m "feat: expose safe character reload"
```

## Task 3: Introduce validated settings documents and atomic recovery

**Files:**
- Create: `src/YimyWalk.Core/Settings/SettingsDocument.cs`, `tests/YimyWalk.Core.Tests/SettingsDocumentTests.cs`.
- Create: `src/YimyWalk.App/Settings/SettingsStore.cs`.
- Modify: `src/YimyWalk.App/Host/AppHost.cs`, `src/YimyWalk.App/UI/ManagerForm.cs`.

- [x] **Step 1: Write failing pure tests for defaults, bounds and recovery.**

Test that default settings select the current role, scale `1.0`, normal activity, automatic dialogue enabled, and no startup registration. Test invalid JSON, unknown fields, negative scale, non-finite numeric values, and counts above the configured soft warning are rejected to defaults without throwing out of startup. Test a valid document round-trips with UTF-8 and an atomic temporary-file replacement path.

- [x] **Step 2: Implement platform-neutral settings data.**

Define an immutable `SettingsDocument` with schema version, selected role ID, scale, activity rate, dialogue enabled, preferred display ID, and optional warning limit. Validate finite values and ranges in one method; do not include HWND, `Form`, `Bitmap`, user file content, startup registration, or external command fields.

- [x] **Step 3: Implement `SettingsStore`.**

Store under the user-local application-data directory, never the install directory. Write `settings.json.tmp`, flush/close it, replace the destination atomically where supported, and retain a `.bak` copy. On missing/corrupt/schema-incompatible input, return defaults and a diagnostic result; never delete unrelated files. Tests use a temporary directory only.

- [x] **Step 4: Load/save at host lifecycle boundaries.**

Load before role selection, apply only validated role/scale/display preferences, save on explicit manager changes and clean exit, and keep startup registration disabled. Add a visible recovery status when defaults were used.

- [x] **Step 5: Run tests and commit.**

Run the settings filter, full core tests, and `scripts/build-yimywalk.ps1`; commit `feat: add recoverable user settings`.

Task 3 evidence: `SettingsDocumentTests` and the full Core suite pass (53 tests). `DesktopSmoke` verifies missing-file defaults, two saves with an atomic replacement, corrupt-primary recovery from `.bak`, role reload retention, and 1/10/50/100 creation and clear on the real Windows desktop. The host loads before role selection, saves selection and validated settings on change/clean exit, applies the stored scale to new windows, and reports default/backup recovery in the manager. Preferred display is retained as a validated setting and remains unapplied until Task 4's display adapter exists.

## Task 4: Add display/work-area contracts and DPI-safe positioning

**Files:**
- Create: `src/YimyWalk.Core/Display/DisplayContracts.cs`, `tests/YimyWalk.Core.Tests/DisplayContractTests.cs`.
- Modify: `src/YimyWalk.Core/Simulation/SimulationContracts.cs`, `PetEngine.cs`, `PetSession.cs`.
- Create: `src/YimyWalk.App/Host/WindowsDisplayAdapter.cs`.
- Modify: `src/YimyWalk.App/Host/DesktopWorldSurface.cs`, `PetWindow.cs`, `NativeMethods.cs`, `DesktopInputSmoke/Program.cs`.

- [x] **Step 1: Write red tests for negative coordinates and display selection.**

Use fake displays with bounds such as `(-1920,0)-(0,1080)` and `(0,0)-(2560,1440)`, different working areas, and a point crossing the boundary. Assert clamping uses the selected display's working area, a missing display relocates to the primary fallback, and no coordinate is rewritten through a positive-only origin assumption.

- [x] **Step 2: Define pure display contracts.**

Add immutable `DisplayId`, `DisplayInfo` (bounds, working area, scale, primary flag), and `IDisplayProvider`. Keep Core unaware of `Screen`, DPI APIs, HWNDs, and WinForms. Extend the surface contract with a deterministic display snapshot or point-to-display lookup used by physics; preserve the single-display implementation as the default.

- [x] **Step 3: Implement the Windows adapter.**

Map `Screen.AllScreens` to `DisplayInfo`, read per-window DPI through `GetDpiForWindow` when a handle exists, and use the system DPI only as a documented fallback. Handle screen enumeration failures by returning the primary working area. Do not change display settings or move user windows.

- [x] **Step 4: Make pet positioning and hit tests use one coordinate transform.**

Use screen coordinates for `PetWindow.Location`, `WM_NCHITTEST` lParam conversion, drag positions, and display working areas. Preserve `WS_EX_NOACTIVATE`, `MA_NOACTIVATE`, and `SWP_NOACTIVATE`. Add logging of display count, bounds, working areas, and DPI to the desktop smoke output.

- [x] **Step 5: Verify what the environment supports.**

Run core display tests and `scripts/build-yimywalk.ps1`. On a machine with a second display, run the negative-coordinate/cross-screen matrix; on the current single-display machine record `DISPLAY1` and leave the desktop matrix marked `Windows 桌面未验证`.

Task 4 evidence: `DisplayContractTests` and the full Core suite pass (56 tests). The current Windows desktop reports one `DISPLAY1`; the publish smoke recorded bounds `1536x864`, working area `1536x816`, system scale `1`, while the input smoke recorded the active interactive session as `1920x1080`, working area `1920x1020`, per-window scale `1.25`. Both real desktop smokes passed the existing fall, movement, hit-test, drag, focus, tray, reload, and cleanup checks. Negative-coordinate and cross-display behavior is covered by Core tests but remains Windows desktop unverified because no second display is exposed.

- [x] **Step 6: Commit.**

```powershell
git add src/YimyWalk.Core src/YimyWalk.App tests tools docs
git commit -m "feat: add display and DPI abstraction"
```

## Task 5: Add conservative window-top landing with safe fallback

**Files:**
- Create: `src/YimyWalk.App/Host/WindowSurfaceResolver.cs`.
- Modify: `DesktopWorldSurface.cs`, `AppHost.cs`, `PetEngine.cs`, `PetWindow.cs`, `NativeMethods.cs`.
- Create: `tests/YimyWalk.Core.Tests/SurfaceSelectionTests.cs` for pure filtering/ordering rules.

- [x] **Step 1: Write pure selection tests.**

Given candidate rectangles and process/window metadata, reject YimyWalk windows, hidden/minimized windows, zero-area rectangles, and stale handles; prefer the nearest visible top edge under the pet's horizontal foot span; fall back to the display working-area floor when no candidate qualifies.

- [x] **Step 2: Implement a low-frequency Windows enumerator.**

Use `EnumWindows`, `IsWindowVisible`, `IsIconic`, `GetWindowRect`, and process-ID filtering only. Never move, minimize, close, or inspect window contents. Refresh candidates on a timer no faster than the simulation surface contract requires, and handle access-denied/stale handles by skipping them.

- [x] **Step 3: Integrate the fallback state.**

When a pet loses its supporting top, enter `Fall`; when enumeration fails or no surface is safe, use the display working-area floor. Exclude the manager and hidden host forms. Keep window-top landing disabled unless the resolver has a valid snapshot, so M1 behavior remains stable while the feature is being validated.

- [x] **Step 4: Verify only with real windows.**

Add a Windows desktop harness that opens a test window, observes a pet land on its top, moves/closes the test window, and observes re-fall or floor fallback. A core simulation test or screenshot alone cannot mark this task passed.

- [x] **Step 5: Commit after real evidence.**

Run the full test/build suite and the desktop input smoke; commit `feat: add conservative window surface landing` only with the actual desktop result recorded.

Task 5 evidence: `SurfaceSelectionTests` and the full Core suite pass (59 tests). `WindowSurfaceResolver` refreshes visible top-level windows no faster than 250 ms, excludes the host/manager and desktop shell or system overlay classes, skips minimized/stale/zero-area and impractically small windows, and never moves or inspects candidate contents. `PetEngine` selects the nearest eligible top while descending and returns to the display working-area floor when the selected window disappears. `scripts/desktop-smoke.ps1 -Landing` passed on the real Windows desktop: the probe at top 300 received the pet, closing it caused re-fall, and cleanup completed. The test session exposed only `DISPLAY1`; second-display/DPI landing remains unverified.

## Task 6: Complete M2 regression evidence and handoff

**Files:**
- Modify: `docs/ARCHITECTURE.md`, `docs/TESTING.md`, `docs/STATUS.md`, `README.md`.
- Use: `scripts/test-yimywalk.ps1`, `scripts/build-yimywalk.ps1`, `scripts/desktop-input-smoke.ps1`, `scripts/desktop-smoke.ps1`, plus the display/landing harnesses.

- [x] **Step 1: Run the core and publish checks.**

Record exact commands, exit codes, test counts, publish path, and role packages. Confirm Core still has no Windows assembly references.

- [x] **Step 2: Run available Windows matrices.**

On the current display, rerun 1/10/50/100 creation/clear, reload success/failure, settings recovery, input/focus/transparent hit testing, and exit cleanup. On a second-display/DPI environment, add negative coordinates, cross-screen drag, taskbar changes, display disconnect/reconnect, and DPI transition. Keep unavailable scenarios explicitly marked unverified.

- [x] **Step 3: Review resource evidence.**

Report active instances, window count, USER/GDI/handles, working set, private bytes, managed heap, and final Dispose metrics. Treat 100 as a pressure tier and do not invent a permanent hard cap or smoothness guarantee.

- [x] **Step 4: Update architecture and status.**

Document who owns decoded PNG/RGBA frames, shared definitions, platform bitmaps, HWNDs, settings files, and display snapshots. Record the second-display deferral and the next executable task. Separate `代码已实现`, `构建通过`, `核心测试通过`, `Windows 桌面已验证`, and `Windows 桌面未验证`.

- [x] **Step 5: Final verification and commit.**

Run `git diff --check`, the full test/build/input/smoke commands, inspect `git status --short`, then commit `feat: establish YimyWalk M2 foundation`. Do not call M2 fully accepted while the second-display/DPI and window-top landing matrices remain unverified.

## Review checklist

- No task adds hunger, bubbles, external app events, real shortcut manipulation, startup registration, or network services.
- No Core file references WinForms, `Screen`, `Bitmap`, `HWND`, or P/Invoke.
- Existing instances retain their old role definition after refresh; old resources are released only after instance and renderer leases end.
- Settings recovery never overwrites unrelated user files and never enables startup by default.
- One host process remains responsible for every pet window.
- Current single-display limitations and all real desktop evidence remain explicit in `docs/STATUS.md`.
