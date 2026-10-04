# YimyWalk M1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the first real Windows desktop pet milestone with data-driven 3×3 PNG roles, independent physics instances, one transparent window per pet, tray lifecycle, and responsive 1/10/50/100 creation and clear operations.

**Architecture:** `YimyWalk.Core` targets `net8.0` and contains immutable character data, PNG decoding output, simulation contracts, physics, and session/resource lifecycle without WinForms or Windows API references. `YimyWalk.App` targets `net8.0-windows` with WinForms; one host process owns one layered window per `PetInstance`, a tray icon and a manager form. Core decoded RGBA frames are shared by character definition; the app owns platform bitmap/layered-window resources and releases them through definition reference counts.

**Tech Stack:** .NET 8.0.425, C# 12, `net8.0` core, `net8.0-windows` WinForms, SixLabors.ImageSharp 3.1.12 for PNG decode, xUnit 2.9.3 and Microsoft.NET.Test.Sdk 18.10.1 for core tests, Win32 `UpdateLayeredWindow`/`WM_NCHITTEST` for transparent ARGB windows, PowerShell scripts.

**Approved scope:** 3×3 loading; Idle/Walk/Jump/Fall/Dragged; screen working-area ground; drag/toss; transparent display; tray lifecycle; multi-instance creation and clear. Window-top landing, hunger, bubbles, external-application events and real shortcut operations remain outside M1.

**Upstream decision:** M0 fixed `Adrianotiger/desktopPet` at `e2933305157a34818f47d238c74607593d81816d`. The original .NET Framework 4.7.2 project fails at the unused `stdole` COM reference on x86/x64; an isolated project copy without that reference builds and its motion/display/runtime checks pass. Reuse is limited to API/test ideas from `src/dotNet/FormPet.cs`, `FormPet.Motion.cs`, `Animations.cs`, and `tests/*`; do not modify the snapshot, import its XML protocol, or migrate its runtime.

---

## File map

- Create `YimyWalk.sln`.
- Create `src/YimyWalk.Core/YimyWalk.Core.csproj` and files under `src/YimyWalk.Core/Characters`, `Simulation`, and `Resources`.
- Create `tests/YimyWalk.Core.Tests/YimyWalk.Core.Tests.csproj` and focused test files for format, physics, session, and lifecycle.
- Create `src/YimyWalk.App/YimyWalk.App.csproj` and files under `Host`, `Windows`, `Rendering`, and `UI`.
- Create `Characters/cat/sprite.png` and `Characters/slime/sprite.png`, with `character.json` omitted so the minimum package path is exercised.
- Create `tools/GenerateTestCharacters.ps1` to regenerate the two original, clearly marked test sprites without external assets.
- Create `scripts/build-yimywalk.ps1`, `scripts/test-yimywalk.ps1`, and `scripts/run-yimywalk.ps1`; modify `README.md`, `AGENTS.md`, `docs/CHARACTER_FORMAT.md`, `docs/TESTING.md`, and `docs/STATUS.md`.

## Task 1: Create the .NET solution and red test harness

**Files:**
- Create: `YimyWalk.sln`
- Create: `src/YimyWalk.Core/YimyWalk.Core.csproj`
- Create: `tests/YimyWalk.Core.Tests/YimyWalk.Core.Tests.csproj`
- Create: `tests/YimyWalk.Core.Tests/SmokeTests.cs`
- Create: `scripts/test-yimywalk.ps1`

- [ ] **Step 1: Create the projects with exact target frameworks and packages.**

`YimyWalk.Core.csproj` targets `net8.0`, enables nullable and implicit usings, and references `SixLabors.ImageSharp` `3.1.12`. The test project targets `net8.0`, references the core project, and references `Microsoft.NET.Test.Sdk` `18.10.1`, `xunit` `2.9.3`, and `xunit.runner.visualstudio` `2.8.2` with `PrivateAssets=all`.

- [ ] **Step 2: Add a failing smoke test proving the intended assembly boundary.**

```csharp
namespace YimyWalk.Core.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void CoreAssemblyDoesNotReferenceWindowsForms()
    {
        var assembly = typeof(YimyWalk.Core.Characters.CharacterDefinition).Assembly;
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), x =>
            x.Name is "System.Windows.Forms" or "WindowsBase" or "PresentationCore");
    }
}
```

This initially fails because `CharacterDefinition` does not exist; that is the expected red result.

- [ ] **Step 3: Run the red test.**

Run: `& .tools/dotnet/dotnet.exe test tests/YimyWalk.Core.Tests/YimyWalk.Core.Tests.csproj --no-restore`

Expected: compile failure naming the missing `YimyWalk.Core.Characters.CharacterDefinition` type.

- [ ] **Step 4: Add the minimum placeholder type and rerun.**

Create `src/YimyWalk.Core/Characters/CharacterDefinition.cs` with an internal-safe public sealed type containing no platform references, run `dotnet restore YimyWalk.sln` followed by the test command, and record the first green smoke test.

- [ ] **Step 5: Add the local build/test scripts and commit.**

`scripts/test-yimywalk.ps1` must invoke the workspace SDK, run `dotnet test YimyWalk.sln --configuration Release`, and fail on a nonzero exit code. Commit: `build: add YimyWalk core solution and tests`.

## Task 2: Implement the 3×3 character protocol and loader

**Files:**
- Modify: `src/YimyWalk.Core/Characters/CharacterDefinition.cs`
- Create: `src/YimyWalk.Core/Characters/CharacterFormat.cs`
- Create: `src/YimyWalk.Core/Characters/CharacterLoader.cs`
- Create: `src/YimyWalk.Core/Characters/CharacterCatalog.cs`
- Create: `src/YimyWalk.Core/Characters/DialogueSet.cs`
- Create: `tests/YimyWalk.Core.Tests/CharacterFormatTests.cs`
- Create: `tests/YimyWalk.Core.Tests/CharacterLoaderTests.cs`
- Create: `tests/YimyWalk.Core.Tests/TestSpriteFactory.cs`

- [ ] **Step 1: Write red tests for frame indexes, anchor, invalid dimensions, alpha and sprite-only fallback.**

Tests must assert that a 96×96 RGBA PNG produces 32×32 cells; indexes 0–2 are Idle, 3–5 WalkRight, 6 Jump, 7 Apex, 8 Fall; `Anchor=(16,28.8)` for a 32×32 cell; WalkLeft is a horizontal view of WalkRight; a width or height not divisible by 3, a corrupt PNG, all-transparent image, and pixel dimensions above the configured limit are rejected; a directory containing only `sprite.png` produces default movement/behavior/dialogue values.

- [ ] **Step 2: Run the focused tests and confirm they fail for missing loader behavior.**

Run: `& .tools/dotnet/dotnet.exe test tests/YimyWalk.Core.Tests/YimyWalk.Core.Tests.csproj --filter FullyQualifiedName~Character`

Expected: failures or compile errors naming the missing `CharacterLoader`, `FrameKind`, and validation behavior.

- [ ] **Step 3: Implement immutable character data.**

Use these public shapes:

```csharp
public enum FrameKind { Idle, WalkRight, WalkLeft, Jump, Apex, Fall }
public readonly record struct Anchor(float X, float Y);
public sealed record CharacterFrame(int Index, int Width, int Height, ReadOnlyMemory<byte> RgbaPixels);
public sealed record CharacterMovement(float WalkSpeed, float JumpSpeed, float Gravity, float MaxFallSpeed);
public sealed record CharacterDefaults(float IdleSecondsMin, float IdleSecondsMax, float SpeechCooldownSeconds);
public sealed class CharacterDefinition : IDisposable
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required int CellWidth { get; init; }
    public required int CellHeight { get; init; }
    public required Anchor Anchor { get; init; }
    public required IReadOnlyList<CharacterFrame> Frames { get; init; }
    public required CharacterMovement Movement { get; init; }
    public required CharacterDefaults Defaults { get; init; }
    public required DialogueSet Dialogue { get; init; }
    public void Dispose() { /* release only owned immutable buffers once no instance references remain */ }
}
```

The frame list stores the decoded RGBA bytes once. It never stores `Bitmap`, `Form`, `HWND`, or a mutable instance field.

- [ ] **Step 4: Implement `CharacterLoader.LoadDirectory`.**

`CharacterLoader.LoadDirectory(string directory)` accepts only one directory level, resolves `sprite.png` beneath it, decodes with ImageSharp into RGBA bytes, validates limits, reads optional `character.json` with strict finite-number/default checks, and reads optional UTF-8 `dialogue.txt` with built-in fallback. Unknown optional files are ignored. An invalid package throws `CharacterLoadException` containing the role path and actionable reason.

- [ ] **Step 5: Implement catalog replacement semantics.**

`CharacterCatalog.Scan(string root)` returns a new immutable dictionary. A failed package is reported through a result object and does not replace the previous entry. `TryGet(string id, out CharacterDefinition definition)` exposes only the currently indexed definition. M1 does not add a visible reload button, but the catalog API must support M2 refresh without forcing existing instances to switch definitions.

- [ ] **Step 6: Run the focused tests green and commit.**

Run the Character filter and then the full core test project. Commit: `feat: load shared 3x3 character definitions`.

## Task 3: Implement the core simulation and exact frame selection

**Files:**
- Create: `src/YimyWalk.Core/Simulation/PetState.cs`
- Create: `src/YimyWalk.Core/Simulation/PetInstance.cs`
- Create: `src/YimyWalk.Core/Simulation/SimulationContracts.cs`
- Create: `src/YimyWalk.Core/Simulation/PetEngine.cs`
- Create: `tests/YimyWalk.Core.Tests/PetPhysicsTests.cs`

- [ ] **Step 1: Write red tests for state transitions and bounded time.**

Use a fake world rectangle `Left=0, Top=0, Right=1280, Bottom=720`, a deterministic random source, and a fake clock. Cover spawn-above-screen → Fall → ground → Idle; Walk direction and screen-boundary reversal; Jump frame 6 → Apex frame 7 only near zero vertical velocity → Fall frame 8 → ground; `deltaSeconds` clamps to 0.1; a dragged pet ignores gravity and autonomous movement; release applies capped toss velocity; two instances never share position, velocity or animation time; and missing/removed surfaces relocate safely.

- [ ] **Step 2: Run `PetPhysicsTests` and confirm red failures.**

Run: `& .tools/dotnet/dotnet.exe test tests/YimyWalk.Core.Tests/YimyWalk.Core.Tests.csproj --filter FullyQualifiedName~PetPhysics`

Expected: missing type/member failures or assertion failures before the engine exists.

- [ ] **Step 3: Add simulation contracts.**

```csharp
public readonly record struct WorldRect(float Left, float Top, float Right, float Bottom);
public interface IWorldSurface { WorldRect WorkingArea { get; } bool IsValid { get; } }
public interface IRandomSource { double NextDouble(); int Next(int minInclusive, int maxExclusive); }
public enum PetState { Idle, Walk, Jump, Fall, Dragged }
```

`PetInstance` owns `InstanceId`, its `CharacterDefinition` reference, foot position, velocity, facing, state, state time, animation time, and drag/toss fields. Its constructor receives a definition and initial foot position; no state is static or stored on the definition.

- [ ] **Step 4: Implement `PetEngine.Tick`.**

Use screen-Y-down coordinates, feet at `anchor`, a maximum step of 0.1 seconds, gravity only in Jump/Fall, capped fall speed, ground clamp at `WorkingArea.Bottom`, deterministic decisions no more often than once per state decision interval, and clamped walk speed. Dragged state accepts `SetDraggedFootPosition`; `ReleaseDrag(Vector2 tossVelocity)` clamps both components and enters Fall. `GetFrameKind` maps the third row by motion state rather than looping it.

- [ ] **Step 5: Run the focused tests green, then all core tests.**

Run both the filter and `& .tools/dotnet/dotnet.exe test tests/YimyWalk.Core.Tests/YimyWalk.Core.Tests.csproj --configuration Release`. Commit: `feat: add deterministic pet physics`.

## Task 4: Add shared definitions, instance/session cleanup and real test roles

**Files:**
- Create: `src/YimyWalk.Core/Simulation/PetSession.cs`
- Create: `src/YimyWalk.Core/Resources/DefinitionLease.cs`
- Create: `Characters/cat/sprite.png`
- Create: `Characters/slime/sprite.png`
- Create: `tools/GenerateTestCharacters.ps1`
- Create: `tests/YimyWalk.Core.Tests/PetSessionTests.cs`
- Create: `tests/YimyWalk.Core.Tests/ResourceLifecycleTests.cs`

- [ ] **Step 1: Write red tests for independent instances and definition lifetime.**

Assert that 100 cat instances reference the same `CharacterDefinition`; mutating one instance does not change another; creation of 1/10/50/100 returns exactly the requested active count; clear removes every instance; a failed replacement keeps the previous definition; a successful replacement causes new instances to reference the new definition while existing instances retain the old one; old definitions dispose only after the last instance and renderer lease release; clear and dispose detach events/timers and release all leases.

- [ ] **Step 2: Implement reference-counted definition leases.**

`DefinitionLease` exposes `Definition`, increments on `Acquire`, decrements on `Dispose`, and invokes the definition disposer exactly once when both instance and renderer lease counts reach zero. `PetSession` owns the instance dictionary and emits `InstanceAdded`/`InstanceRemoved` events. The session has `CreateMany`, `Remove`, `Clear`, `Pause`, `Resume`, `Dispose`, and a configurable soft warning/hard limit; the hard limit is not fixed at 100.

- [ ] **Step 3: Generate and inspect two original role packages.**

`GenerateTestCharacters.ps1` writes transparent 96×96 PNGs with 32×32 cells: `cat` uses a warm cat silhouette and visibly different poses in the three rows; `slime` uses a green slime silhouette and different poses. The script uses only standard PowerShell/.NET PNG generation or a checked-in helper and never copies third-party art. Run it, load both directories, and visually inspect the generated PNGs before committing.

- [ ] **Step 4: Run lifecycle tests and commit.**

Run the session/resource filters and full core suite. Commit: `feat: share role resources across pet instances`.

## Task 5: Create the Windows host and one-window-per-pet renderer

**Files:**
- Create: `src/YimyWalk.App/YimyWalk.App.csproj`
- Create: `src/YimyWalk.App/Program.cs`
- Create: `src/YimyWalk.App/Host/AppHost.cs`
- Create: `src/YimyWalk.App/Host/PetWindowRegistry.cs`
- Create: `src/YimyWalk.App/Windows/PetWindow.cs`
- Create: `src/YimyWalk.App/Windows/NativeMethods.cs`
- Create: `src/YimyWalk.App/Rendering/FrameSurfaceCache.cs`
- Create: `src/YimyWalk.App/Rendering/LayeredWindowRenderer.cs`
- Create: `tests/YimyWalk.Core.Tests/RenderingContractTests.cs`

- [ ] **Step 1: Write red platform-contract tests without creating a window in Core.**

Test that the app project references Core while Core references no Windows assemblies, and that a renderer lease is acquired per definition rather than per instance. Keep actual Win32 tests in a separately marked Windows desktop test entry; do not pretend a Core test proves transparency.

- [ ] **Step 2: Add the WinForms app project.**

Set `<TargetFramework>net8.0-windows</TargetFramework>`, `<UseWindowsForms>true</UseWindowsForms>`, `<OutputType>WinExe</OutputType>`, and `<PlatformTarget>x64</PlatformTarget>` for the primary build. Add a project reference to Core. The app owns all `System.Drawing`, `System.Windows.Forms`, P/Invoke and `HWND` types.

- [ ] **Step 3: Implement layered window creation.**

`PetWindow` is borderless, hidden from task switcher, `WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`, and uses `UpdateLayeredWindow` with a premultiplied ARGB DIB. It maps one `PetInstance` to one window and never starts a process. `SetWindowPos` always uses `SWP_NOACTIVATE`.

- [ ] **Step 4: Implement shared frame surfaces and hit testing.**

`FrameSurfaceCache` keys immutable role/frame/scale combinations and returns a disposable renderer lease. It converts Core RGBA bytes to premultiplied platform surfaces once per definition/scale, tracks leases, and disposes only after the final instance and paint lease release. `PetWindow.WndProc` handles `WM_NCHITTEST`: alpha below the configured hit threshold returns `HTTRANSPARENT`; visible pixels return `HTCLIENT`. This is separately tested for background click-through and entity hit.

- [ ] **Step 5: Implement drag and release wiring.**

Mouse down on an entity calls `PetSession.BeginDrag`, captures the mouse and pauses autonomous motion. Mouse move updates the Core foot position immediately. Mouse up, capture loss and deactivation call the same idempotent release path; recent cursor samples become a capped toss velocity. The window never activates on drag or repaint.

- [ ] **Step 6: Run app build and targeted Windows checks.**

Run: `& .tools/dotnet/dotnet.exe build src/YimyWalk.App/YimyWalk.App.csproj --configuration Release -p:Platform=x64`.

Run `scripts/desktop-smoke.ps1` for real 1/10/50/100 window creation and clearing. Run `scripts/desktop-input-smoke.ps1` in an unlocked visible session to verify separately: transparent region passes a click to a test window; visible pet pixels receive a click; dragging moves the pet; releasing creates a fall/toss; clicking/dragging does not activate the pet window. The harness records the production `WM_NCHITTEST` values and reports a `LockScreenBackstopFrame`/secure-desktop interception as unverified rather than treating it as a pass. Commit: `feat: add one-window-per-pet layered host`.

## Task 6: Add tray lifecycle, manager controls and responsive batch creation

**Files:**
- Create: `src/YimyWalk.App/UI/ManagerForm.cs`
- Create: `src/YimyWalk.App/UI/TrayIconHost.cs`
- Create: `src/YimyWalk.App/Host/BatchCreationQueue.cs`
- Modify: `src/YimyWalk.App/Host/AppHost.cs`
- Create: `tests/YimyWalk.Core.Tests/BatchCreationTests.cs`

- [ ] **Step 1: Write red queue tests.**

Use a fake window factory and assert that requests for 1, 10, 50 and 100 eventually create exactly that many instances in bounded batches; the request method returns without synchronously creating all 100; `Clear` cancels pending work and disposes already-created windows; a second request cannot create beyond the configurable hard limit.

- [ ] **Step 2: Implement the batch queue.**

`BatchCreationQueue.Enqueue(count)` stores remaining work and uses a WinForms timer or `BeginInvoke` batches of at most eight windows per UI turn. It exposes `PendingCount`, `ActiveCount`, `Cancel`, and `Dispose`. Every created window is registered in `PetWindowRegistry`; every removal unregisters and disposes it.

- [ ] **Step 3: Implement manager controls.**

`ManagerForm` scans `Characters` on startup, displays role ID/name and active count, and exposes buttons labelled `创建 1`, `创建 10`, `创建 50`, `创建 100`, and `一键清空`. It must not show unimplemented M2/M3 actions. Controls remain responsive while the queue drains.

- [ ] **Step 4: Implement tray lifecycle.**

`TrayIconHost` owns one `NotifyIcon` and a context menu for opening the manager, clearing all pets, hiding/restoring pet windows, and exiting. Exit cancels queue work, stops simulation/render timers, disposes every pet window, releases renderer and definition leases, disposes the tray icon and exits the message loop. No startup registration, admin elevation, network service or external file operation is added.

- [ ] **Step 5: Run queue tests, build the app and commit.**

Run the queue filter, full core tests, and Release app build. Commit: `feat: add tray and responsive multi-instance controls`.

## Task 7: Add reproducible role generation, build/run scripts and documentation

**Files:**
- Create: `scripts/build-yimywalk.ps1`
- Create: `scripts/run-yimywalk.ps1`
- Modify: `README.md`
- Modify: `AGENTS.md`
- Modify: `docs/CHARACTER_FORMAT.md`
- Modify: `docs/TESTING.md`
- Modify: `docs/ARCHITECTURE.md`
- Modify: `docs/STATUS.md`
- Modify: `THIRD_PARTY_NOTICES.md`

- [ ] **Step 1: Add build and run scripts.**

`build-yimywalk.ps1` invokes `.tools/dotnet/dotnet.exe restore`, `dotnet test`, and `dotnet publish src/YimyWalk.App/YimyWalk.App.csproj -c Release -r win-x64 --self-contained false`; it fails if the SDK or project is missing. `run-yimywalk.ps1` launches the verified Release output from the workspace `Characters` directory and never enables startup registration.

- [ ] **Step 2: Document the exact role protocol and startup path.**

State frame mapping exactly: 0–2 Idle, 3–5 WalkRight mirrored for left, 6 Jump, 7 short Apex near zero vertical velocity, 8 Fall; third row is state-selected, not a loop; default foot anchor is `(0.5 * cellWidth, 0.9 * cellHeight)`. Document sprite-only fallback, two sample roles, build/run commands, and the current Windows-only limitations.

- [ ] **Step 3: Add ImageSharp attribution.**

Record SixLabors.ImageSharp 3.1.12 and its Apache 2.0 license in `THIRD_PARTY_NOTICES.md`; keep upstream MIT and bundled-media caveats unchanged.

- [ ] **Step 4: Update status with evidence categories.**

`docs/STATUS.md` must separately list code implemented, builds passing, core tests passing, Windows desktop checks passing, and Windows desktop checks unverified. Include actual instance count, OS/build architecture, display/DPI, CPU/memory/handle measurements for every run that was executed; do not claim 100-pet smoothness without a measurement.

## Task 8: Execute the M1 acceptance matrix and close the milestone honestly

**Files:**
- Modify: `docs/STATUS.md`
- Modify: `docs/TESTING.md`
- Use: `scripts/desktop-smoke.ps1` and `scripts/desktop-input-smoke.ps1`
- Create: `artifacts/m1/<timestamp>/` test logs and measurements (ignored by Git)

- [ ] **Step 1: Run automated verification.**

Run `scripts/build-yimywalk.ps1`, `scripts/test-yimywalk.ps1`, and the Release publish command. Expected results: restore/build/test exit 0 and all core tests pass. Preserve command output under the ignored artifacts directory.

- [ ] **Step 2: Run the real Windows desktop matrix.**

On the available Windows desktop, record OS version, x64/x86 process architecture, DPI scale, display count/work areas, CPU model, memory, and applicable process/USER/GDI handles. Test role loading from only `sprite.png`, role switching without code changes, actual Fall → ground → Walk/Idle, Jump, drag, release toss, transparent hit-through, visible entity hit, no focus activation, tray exit, and create/clear at 1/10/50/100. Repeat create/clear and record resource trends. `desktop-input-smoke.ps1` must print the direct `WM_NCHITTEST` values and actual `WindowFromPoint` targets; a `LockScreenBackstopFrame` or other secure-desktop interception leaves input routing unverified. Do not move desktop files or enable startup/security changes.

- [ ] **Step 3: Mark each result with its evidence level.**

Use only these labels: `代码已实现`, `构建通过`, `核心测试通过`, `Windows 桌面已验证`, `Windows 桌面未验证`. A blocked or unavailable GUI session leaves the desktop labels unverified and keeps M1 open.

- [ ] **Step 4: Commit the verified milestone and push the feature branch.**

Run `git diff --check`, `git status --short`, and the complete verification commands again. Commit: `feat: deliver YimyWalk M1 desktop baseline`; push `codex/bootstrap` to `origin`. If any desktop check remains unverified, keep that explicit in the commit status and do not call the full M1 acceptance complete.

## 执行状态（2026-10-04）

- [x] Task 1：Core solution、Release test harness 和脚本。
- [x] Task 2：3×3 角色协议、PNG 解码、默认配置、目录索引。
- [x] Task 3：Core 物理、状态机、delta 上限、拖拽/抛掷和帧选择。
- [x] Task 4：共享定义、实例会话、引用计数、清空路径和两套 sprite-only 角色。
- [x] Task 5：`net8.0-windows`、一个实例一个分层窗口、透明/实体命中分流和不抢焦点窗口样式；代码构建已通过，交互桌面验收未完成。
- [x] Task 6：托盘菜单、管理器、1/10/50/100 按钮和每 UI turn 最多 8 只的批量队列；代码构建已通过，真实批量桌面验收未完成。
- [x] Task 7：构建/运行脚本、角色生成脚本、文档和第三方通知。
- [ ] Task 8：自动化、已解锁桌面的 1/10/50/100 批量 smoke、cat/slime 加载和输入矩阵（透明路由、实体点击、拖拽/抛掷、焦点、捕获丢失、隐藏/恢复、清空和退出）已通过；真实托盘菜单逐项点击、多显示器/DPI 和更长期资源趋势仍保持 Windows 桌面未验证。

完成判定仍按五类证据分别记录：代码已实现、构建通过、核心测试通过、Windows 桌面已验证、Windows 桌面未验证。未完成的桌面项不会因为核心测试或启动 smoke test 而自动变为已验收。
