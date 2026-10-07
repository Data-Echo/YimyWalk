# Window Jump and Climb Frame Corrections Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Use the static Apex frame while climbing and add an independently configurable global/per-pet desire for jumping to reachable visible window tops.

**Architecture:** Keep the existing Core state machine and `JumpTargetPlanner`. Add one bounded Core desire value and one nullable per-instance override, gate only the target-selection branch in `PetEngine`, and leave ordinary jump physics/fallback behavior unchanged. Bind the value through the existing AppHost persistence and Interactions/Pets UI patterns.

**Tech Stack:** C#/.NET 8, Core xUnit tests, WinForms/AntdUI smoke tools, PowerShell release checks.

---

### Task 1: Add failing Core coverage for the two behavior corrections

**Files:**
- Modify: `tests/YimyWalk.Core.Tests/AutonomousMovementTests.cs`
- Modify: `tests/YimyWalk.Core.Tests/AutonomousBoundaryTests.cs`
- Modify: `tests/YimyWalk.Core.Tests/PetPhysicsTests.cs`

- [ ] **Step 1: Write the failing tests**
  - Add a test that constructs a climbing pet and asserts `pet.GetFrameKind()` is `FrameKind.Apex`; assert `TopWalk` still returns the directional walk frame.
  - Add tests with a reachable `JumpCandidates` surface and a deterministic random source: `WindowJumpDesire=0` must not select the candidate, `WindowJumpDesire=1` must select it whenever the target branch is attempted, and an intermediate value must respect the sampled probability.
  - Add a test that no candidate falls back to the existing ordinary jump decision and does not teleport.

- [ ] **Step 2: Run only the new tests to verify RED**

```powershell
.tools/dotnet/dotnet.exe test tests/YimyWalk.Core.Tests -c Release --no-restore --filter "FullyQualifiedName~AutonomousMovement|FullyQualifiedName~AutonomousBoundary|FullyQualifiedName~PetPhysics"
```

Expected: the new frame assertion or missing `WindowJumpDesire` behavior fails while existing tests remain discoverable.

### Task 2: Implement Core state/frame and desire plumbing

**Files:**
- Modify: `src/YimyWalk.Core/Simulation/PetInstance.cs`
- Modify: `src/YimyWalk.Core/Simulation/PetEngine.cs`
- Modify: `src/YimyWalk.Core/Simulation/PetSession.cs`
- Modify: `src/YimyWalk.Core/Simulation/PetBehaviorOverrides.cs`
- Modify: `src/YimyWalk.Core/Settings/SettingsDocument.cs`

- [ ] **Step 1: Implement the minimum frame mapping**
  - Map `PetState.Climb` to `FrameKind.Apex` in `PetInstance.GetFrameKind()`; keep `TopWalk` directional.

- [ ] **Step 2: Implement bounded desire values**
  - Add `DefaultWindowJumpDesire = 0.20f`, validated `[0,1]` `SettingsDocument.WindowJumpDesire`, `PetSession.WindowJumpDesire`, `PetEngine.WindowJumpDesire`, and nullable `PetBehaviorOverrides.WindowJumpDesireOverride` with `ResolveWindowJumpDesire`.
  - Normalize non-finite/out-of-range runtime values with the existing desire rules; preserve old JSON compatibility by applying the default when the field is absent and rejecting invalid documents through the existing restore path.

- [ ] **Step 3: Gate only window-target selection**
  - In `TryAutonomousJump`, resolve the per-pet window desire and sample it only when window landing is enabled and a physically reachable candidate exists.
  - At 0, use the existing ordinary-jump branch; at 1, choose the reachable window target on every eligible attempt; for intermediate values, choose the target only when the new sample succeeds. Preserve the existing jump desire sample, attempt reset, cooldown, pause, drag, Eat, and physics fallback behavior.

- [ ] **Step 4: Run the focused tests to verify GREEN**

```powershell
.tools/dotnet/dotnet.exe test tests/YimyWalk.Core.Tests -c Release --no-restore --filter "FullyQualifiedName~AutonomousMovement|FullyQualifiedName~AutonomousBoundary|FullyQualifiedName~PetPhysics"
```

Expected: all focused tests pass.

### Task 3: Persist and expose the new setting in App/UI

**Files:**
- Modify: `src/YimyWalk.App/Host/AppHost.cs`
- Modify: `src/YimyWalk.App/UI/Pages/InteractionsPage.cs`
- Modify: `src/YimyWalk.App/UI/Pages/PetsPage.cs`
- Modify: `src/YimyWalk.App/UI/InstanceSettingState.cs`
- Modify: `tools/ManagerUiSmoke/Program.cs`
- Modify: `tests/YimyWalk.Core.Tests/SettingsDocumentTests.cs`
- Modify: `tests/YimyWalk.Core.Tests/PetBehaviorOverrideTests.cs`

- [ ] **Step 1: Add settings/override round-trip tests**
  - Assert default/serialized/parsed `WindowJumpDesire` is 0.20, invalid values are rejected through the existing settings path, and a nullable per-pet override inherits, clamps, and resets like the other desires.

- [ ] **Step 2: Wire AppHost persistence**
  - Load, expose, clamp, and save `WindowJumpDesire` beside `JumpDesire` and `ClimbDesire`; keep the selected role and user settings untouched.

- [ ] **Step 3: Add the global UI control**
  - Add a 0–100% InputNumber on InteractionsPage, map it to Core `/100`, label its source as global, and keep the existing window-top switch independent.

- [ ] **Step 4: Add per-pet inherit/override/reset UI**
  - Add mode/value/source controls to PetsPage, include them in selection synchronization, apply/reset logic, and enable only for an explicit override.

- [ ] **Step 5: Extend ManagerUiSmoke**
  - Assert the global and per-pet 0–100 controls, set global and per-pet values, verify Core values 0.35/0.70, then reset and verify inheritance. Keep deletion, state refresh, tray, drag, and resource-release checks.

### Task 4: Update design/status and run full verification

**Files:**
- Modify: `docs/superpowers/specs/2026-10-07-yimywalk-autonomous-movement-design.md`
- Modify: `docs/STATUS.md`
- Modify: `tools/ManagerUiSmoke/Program.cs` only if smoke diagnostics need the new assertions.

- [ ] **Step 1: Run the complete Core and App checks**

```powershell
.tools/dotnet/dotnet.exe test YimyWalk.sln -c Release --no-restore
.tools/dotnet/dotnet.exe build src/YimyWalk.App/YimyWalk.App.csproj -c Release -p:Platform=x64 --no-restore
.tools/dotnet/dotnet.exe build tools/ManagerUiSmoke/ManagerUiSmoke.csproj -c Release -p:Platform=x64 --no-restore
```

Expected: Core tests pass with no failures; App and smoke builds report 0 warnings and 0 errors.

- [ ] **Step 2: Run UI/resource verification**

```powershell
.tools/dotnet/dotnet.exe tools/ManagerUiSmoke/bin/x64/Release/net8.0-windows/ManagerUiSmoke.dll
.tools/dotnet/dotnet.exe tools/ManagerUiSmoke/bin/x64/Release/net8.0-windows/ManagerUiSmoke.dll --preview-pixels
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/validate-character.ps1 -Path Characters/slime
```

Expected: full UI, preview pixel, and optimized slime validation pass.

- [ ] **Step 3: Refresh and validate the isolated release package**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-yimywalk.ps1 -Version 0.1.9 -PublishPath artifacts/publish/window-jump-corrections
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-release-package.ps1 -Path artifacts/publish/window-jump-corrections -Version 0.1.9
```

Expected: package roles contain only `slime`; checksum inventory validates all files.

- [ ] **Step 4: Run desktop regression smoke and record boundaries**
  - Run `scripts/desktop-input-smoke.ps1` and `scripts/desktop-display-smoke.ps1 -Rounds 1` when the interactive desktop is available.
  - Record ordinary jump/render/drag/tray/resource results separately from the still-unverified autonomous window-jump and climb paths.

- [ ] **Step 5: Run final hygiene checks**

```powershell
git diff --check
git status --short --branch
```

Expected: no whitespace errors, no debug markers, and no user-role deletion outside the built-in sample.