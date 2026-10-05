# Airborne Cross-Display Toss Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make a released, airborne pet wrap across the global horizontal display span while retaining its flight and landing behavior.

**Architecture:** `PetEngine.Tick` projects airborne motion through a shared horizontal wrapping helper before surface selection. `IntegrateAirborne` applies the same helper for the committed position and keeps the wrapped X coordinate when landing; `IWorldSurface.HorizontalTravelBounds` remains the single source of the global span.

**Tech Stack:** C#/.NET 8, xUnit Core tests, PowerShell build and Windows smoke scripts.

---

### Task 1: Add regression coverage

**Files:**
- Modify: `tests/YimyWalk.Core.Tests/PetPhysicsTests.cs`

- [x] **Step 1: Write right-edge and left-edge airborne wrap tests**

  Use a display working area of `0..100` or `400..500`, a global travel span of `0..500`, release at `1200` or `-1200` pixels/second, and tick for `0.1` seconds. Assert the wrapped X, gravity-adjusted Y, preserved horizontal velocity, and `Fall` state.

- [x] **Step 2: Run the focused tests and verify the expected failure**

  Run `\.tools\dotnet\dotnet.exe test tests\YimyWalk.Core.Tests\YimyWalk.Core.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~AirborneTossWraps`.

  Expected result before the implementation: both tests fail because `IntegrateAirborne` clamps X to the current area.

### Task 2: Implement shared airborne wrapping

**Files:**
- Modify: `src/YimyWalk.Core/Simulation/PetEngine.cs`

- [ ] **Step 1: Pass the global travel span through projection and integration**

  Read `Surface.HorizontalTravelBounds` once in `Tick`, pass it to `GetNextFootPosition`, and pass it to every airborne integration branch.

- [ ] **Step 2: Add a bounded horizontal wrap helper**

  For a positive span width, add or subtract the width until the X coordinate is inside `[Left, Right]`. For a non-positive width, clamp to the current area as the fallback.

- [ ] **Step 3: Use wrapped projected and committed X coordinates**

  Apply the helper to airborne projected positions before `SurfaceForPet`, then use it in `IntegrateAirborne`. Preserve horizontal velocity and use the selected area bottom when landing.

- [ ] **Step 4: Run focused tests and the full Core suite**

  Run the focused filter, then `\.tools\dotnet\dotnet.exe test YimyWalk.sln --configuration Release --no-restore`. Expected result: all tests pass, including the existing window-surface and walking-wrap tests.

### Task 3: Rebuild and verify release behavior

**Files:**
- Modify: `docs/STATUS.md`
- Create: release artifacts under `artifacts/` only

- [ ] **Step 1: Run the release build and publish/package validation**

  Run `scripts/build-yimywalk.ps1`, then the existing release package validation against a fresh publish directory. Confirm the executable, roles, licenses, and manifest remain present.

- [ ] **Step 2: Run available Windows targeted smoke**

  Run the targeted smoke once on the available unlocked desktop. Record whether the drag path crosses a real display seam; leave negative-coordinate, physical disconnect, sleep/resume, and long-running resource scenarios marked as unverified unless directly observed.

- [ ] **Step 3: Update status and inspect the diff**

  Record code, Core, build/package, and Windows smoke status separately in `docs/STATUS.md`, then run `git diff --check` and review the changed files.
