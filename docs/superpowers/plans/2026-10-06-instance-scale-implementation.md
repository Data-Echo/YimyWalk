# Per-Instance Pet Scale Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Allow each running pet instance to use its own display-scale multiplier while preserving the global scale for instances that follow it.

**Architecture:** Keep the persistent global `SettingsDocument.Scale` unchanged. Add a nullable runtime-only override to `PetInstance`; the effective multiplier is the instance override when present, otherwise the global value. `PetWindow` resolves that multiplier together with the immutable character scale and per-monitor DPI, while `PetsPage` edits it through `AppHost`.

**Tech Stack:** .NET 8, WinForms, AntdUI, xUnit, existing `PetWindow`/`FrameSurfaceCache` pipeline.

---

### Task 1: Define and test runtime scale inheritance

**Files:**
- Modify: `src/YimyWalk.Core/Simulation/PetInstance.cs`
- Test: `tests/YimyWalk.Core.Tests/PetBehaviorOverrideTests.cs`

- [x] Add a test proving an inherited instance follows the global multiplier, a custom instance remains unchanged when the global value changes, and clearing the override returns to the global value.
- [x] Run the focused test and confirm it fails because the runtime scale API does not exist.
- [x] Add nullable `ScaleOverride`, bounded to 0.1–8.0, plus a resolver that returns override-or-global without multiplying the global value into a custom override.
- [x] Run the focused test and the Core suite.

### Task 2: Apply the override to native pet windows

**Files:**
- Modify: `src/YimyWalk.App/Windows/PetWindow.cs`
- Modify: `src/YimyWalk.App/Host/AppHost.cs`

- [x] Make `PetWindow` use the instance resolver for frame size, anchor placement, and drag coordinates.
- [x] Keep global-scale updates cached for custom instances but refresh their frame only when their effective multiplier changes.
- [x] Add `SetPetScaleOverride` and reset behavior through `AppHost`, refreshing only the targeted window and never writing `SettingsDocument`.

### Task 3: Add the Pets page editor and source text

**Files:**
- Modify: `src/YimyWalk.App/UI/Pages/PetsPage.cs`
- Modify: `src/YimyWalk.App/UI/InstanceSettingState.cs`
- Modify: `src/YimyWalk.App/UI/PetListProjection.cs`

- [x] Add AntdUI `Select` with “跟随全局/自定义” and an `InputNumber` using the existing 0.1–8.0 range.
- [x] Display source and effective multiplier, disable the numeric editor while following global, and apply edits only to the selected stable instance ID.
- [x] Keep the runtime-only note visible and avoid treating refresh or selection changes as user edits.

### Task 4: Extend UI regression coverage and verify

**Files:**
- Modify: `tools/ManagerUiSmoke/Program.cs`
- Modify: `docs/STATUS.md`
- Modify: `RELEASE_NOTES.md`

- [x] Assert the Pets page exposes the scale source selector and numeric editor.
- [x] In the existing mixed-instance smoke, set one instance to a custom multiplier, change the global multiplier, and verify the custom and inherited instances resolve independently; clear the custom override and verify inheritance returns.
- [x] Run Core tests, ManagerUiSmoke, App build, and `git diff --check`.
- [x] Record that per-instance size is runtime-only and list any Windows DPI combinations not executed.
