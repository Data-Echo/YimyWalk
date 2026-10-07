# Charged Jump Trajectories Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add randomized ordinary charged jumps and physically reachable charged trajectories for visible-window jumps without changing Core platform boundaries or existing behavior priorities.

**Architecture:** Keep all charge state, launch-speed selection, trajectory planning, and ballistic integration in `YimyWalk.Core`. `PetInstance` exposes the new state/frame mapping and per-attempt charge data; `PetEngine` owns timing, random sampling, pause/drag priority, and target fallback. Existing App/UI rendering consumes the state through current frame selection and receives no trajectory policy.

**Tech Stack:** .NET 8, C#, xUnit, existing deterministic random/clock test seams, Core simulation tests.

---

### Task 1: Add failing charge-state and launch tests

**Files:**
- Modify: `tests/YimyWalk.Core.Tests/AutonomousMovementTests.cs`
- Modify: `tests/YimyWalk.Core.Tests/AutonomousBoundaryTests.cs`

- [ ] **Step 1: Write tests for charge state and ordinary launch modes.** Add tests that create a grounded pet with deterministic random input and assert: a charged attempt enters `PetState.JumpCharge`; `GetFrameKind()` is `FrameKind.Apex`; charge updates preserve X and horizontal velocity; an uncharged attempt enters `Jump` immediately; charge duration changes launch vertical speed; and launch speed never exceeds `1.75 * Movement.JumpSpeed`.

- [ ] **Step 2: Write tests for desire, pause, and timer behavior.** Assert ordinary jump desire `0` starts neither charge nor jump, a charge timer is frozen while paused, and one failed probability attempt arms the next retry without drawing again on the next tick.

- [ ] **Step 3: Run the focused tests and verify RED.** Run `dotnet test tests/YimyWalk.Core.Tests/YimyWalk.Core.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~ChargedJump`. Expected: compilation/test failures identifying missing `JumpCharge`/launch behavior, not setup errors.

### Task 2: Add failing window-trajectory planner tests

**Files:**
- Modify: `tests/YimyWalk.Core.Tests/AutonomousMovementTests.cs`
- Modify: `tests/YimyWalk.Core.Tests/AutonomousBoundaryTests.cs`

- [ ] **Step 1: Write a reachable high-window test.** Build a candidate whose top is above the base jump apex and assert a selected window jump uses a charged launch and reaches a descending intersection inside the candidate’s valid landing interval.

- [ ] **Step 2: Write boundary and invalidation tests.** Assert horizontal launch speed is clamped to the existing walk-speed cap, scaled-frame margins are included in the landing interval, unreachable horizontal/vertical targets fall back to an ordinary jump, and invalidating the target during flight leaves the current ballistic state unchanged without snapping or replanning.

- [ ] **Step 3: Run the focused planner tests and verify RED.** Run `dotnet test tests/YimyWalk.Core.Tests/YimyWalk.Core.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~WindowJumpTrajectory`. Expected: failures caused by absent planner/trajectory behavior.

### Task 3: Implement charge state and ordinary launch

**Files:**
- Modify: `src/YimyWalk.Core/Simulation/SimulationContracts.cs`
- Modify: `src/YimyWalk.Core/Simulation/PetInstance.cs`
- Modify: `src/YimyWalk.Core/Simulation/PetEngine.cs`

- [ ] **Step 1: Add the `JumpCharge` state and per-attempt fields.** Extend `PetState`; add finite charge elapsed/duration and sampled launch-speed fields to `PetInstance`; reset them whenever a charge ends, is cancelled by drag, or the pet lands.

- [ ] **Step 2: Map charge rendering to `FrameKind.Apex`.** Update `PetInstance.GetFrameKind()` so `JumpCharge` always resolves to the existing seventh frame while all other frame mappings remain unchanged.

- [ ] **Step 3: Start ordinary attempts with one random mode sample.** In the existing autonomous jump gate, preserve desire/activity/priority checks, sample charged versus uncharged once, sample duration in `[0.15, 0.45]` seconds when charged, and use the existing base jump speed for uncharged attempts. A zero desire returns before either sample.

- [ ] **Step 4: Integrate charge without movement and launch at the sampled speed.** While charging, advance only the charge timer when not paused/dragged; keep position and horizontal velocity fixed; on completion transition to `Jump` with `-min(baseJumpSpeed * 1.75, derivedSpeed)` and preserve the existing horizontal speed cap.

- [ ] **Step 5: Run the Task 1 focused tests and then the full Core suite.** Expected: charge tests pass and existing tests remain green.

### Task 4: Implement reachable charged window trajectories

**Files:**
- Modify: `src/YimyWalk.Core/Simulation/PetEngine.cs`
- Modify: `src/YimyWalk.Core/Display/SurfaceSelection.cs` (only if current landing interval helper needs a Core-neutral extension)

- [ ] **Step 1: Add a Core-neutral trajectory planner helper.** Given current position, frame width/anchor/scale, horizontal speed cap, gravity, base jump speed, and a candidate surface, calculate a descending intersection time and valid X interval; reject non-finite, out-of-range, or unreachable candidates.

- [ ] **Step 2: Select a reachable target only on the existing independent window-desire branch.** If a candidate passes the planner, store the planned launch speed/horizontal velocity for this attempt and enter `JumpCharge` for a bounded duration sufficient to reach the planned vertical speed. If no candidate passes, use Task 3 ordinary random mode selection.

- [ ] **Step 3: Integrate the planned ballistic arc continuously.** Use the existing fixed-step physics path so each tick updates position from velocity and gravity. Do not attach to the target, teleport, replan after movement, or exceed horizontal/vertical caps; if the target disappears or moves, continue current physics and let normal surface selection handle landing.

- [ ] **Step 4: Run the focused trajectory tests and full Core suite.** Expected: all new planner tests and prior regressions pass.

### Task 5: Verify App integration and update status

**Files:**
- Modify: `docs/STATUS.md`

- [ ] **Step 1: Build and run the complete verification set.** Run `dotnet test YimyWalk.sln -c Release --no-restore`; build the x64 App Release; run `ManagerUiSmoke`, `ManagerUiSmoke --preview-pixels`, `scripts/desktop-input-smoke.ps1`, and `scripts/desktop-display-smoke.ps1 -Rounds 1` as available.

- [ ] **Step 2: Review the diff for scope and platform boundaries.** Confirm no App/UI trajectory policy was added, no user assets were removed, and existing tray, drag, state-refresh, resource-release, and fixture regressions remain present.

- [ ] **Step 3: Update `docs/STATUS.md`.** Record the charge and reachable-window implementation, exact test/build evidence, and explicitly mark real desktop timing/visual observations as verified only if actually observed; leave unobserved matrix cases marked unverified.

- [ ] **Step 4: Commit the plan and implementation changes only after fresh verification.** Use a focused commit message describing charged jump trajectories.
