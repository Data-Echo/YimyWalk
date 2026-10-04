# YimyWalk 状态交接

更新时间：2026-10-04
当前分支：`codex/bootstrap`
Current target: M2 foundation. M1 desktop baseline remains the verified scope; M2 starts with atomic role refresh, settings recovery, display/DPI abstractions, and conservative window-surface work.

## M0 上游决策证据

- 上游仓库：`https://github.com/Adrianotiger/desktopPet`
- 固定提交：`e2933305157a34818f47d238c74607593d81816d`
- 快照路径：`vendor/esheep/`
- 原版 `DesktopPet_Portable.csproj` 在未适配环境时先因缺少 .NET Framework 4.7.2 引用程序集失败；补齐工作区引用后，原版仍因未使用的 `stdole` COMReference 报 MSB4181/MSB3283。隔离构建脚本只在临时副本移除这条未使用引用，x64/x86 构建和已有运动、显示器、WinForms runtime 检查通过。
- 复用依据：参考 `src/dotNet/FormPet.cs`、`FormPet.Motion.cs`、`Animations.cs` 的窗口/运动/缓存思路，以及 `tests/` 的运行时检查；没有直接采用上游 XML 角色协议，也没有改写快照。
- YimyWalk 新代码选择 `net8.0` Core 与 `net8.0-windows` App，原因是隔离可测试物理和 Windows API，避开上游 .NET Framework 目标包限制；没有把上游整体迁移到新运行时。

## 已完成证据

### 代码已实现

- `YimyWalk.Core`：PNG 解码、3×3 帧定义、默认配置、共享定义目录索引、独立实例物理、工作区地面、跳跃/坠落、拖拽/抛掷、定义和渲染引用计数。
- M2 Task 1 is implemented in Core: `PetSession.RemoveDefinition` lets a refresh stop new instances for a removed role while existing instances retain their leased definition; the new refresh regression test brings the core suite to 42 passing tests.
- M2 Task 2 is implemented: `AppHost.ReloadCharacters()` atomically rescans roles, keeps old definitions leased by existing pets, removes deleted roles from new-instance creation, and reports failures; the manager exposes a working `????` button and status count. Real smoke covered valid reload, invalid reload retention, and the button entry point.
- `YimyWalk.App`：一个实例一个透明分层窗口；同一宿主进程管理所有窗口；托盘菜单、管理器、1/10/50/100 按钮和分批创建队列；窗口透明区域和实体命中分别走 `WM_NCHITTEST`；窗口使用 `WS_EX_NOACTIVATE`。
- 角色包：`Characters/cat/sprite.png`、`Characters/slime/sprite.png`，均只包含 `sprite.png`。

### 构建通过

- `dotnet restore YimyWalk.sln`：通过。
- `dotnet test YimyWalk.sln --configuration Release --no-restore`：通过，41 个核心测试。
- `dotnet build src/YimyWalk.App/YimyWalk.App.csproj --configuration Release -p:Platform=x64`：通过。
- `scripts/build-yimywalk.ps1`：通过，发布到 `src/YimyWalk.App/bin/x64/Release/net8.0-windows/win-x64/publish/`，并复制角色目录。

### 核心测试通过

- 角色格式、加载校验、物理状态和边界、共享资源生命周期、实例创建/清空均通过。
- `YimyWalk.Core` 程序集没有引用 WinForms、WindowsBase 或 PresentationCore。

### Windows 桌面已验证

- 在本机 Windows 11 Home 10.0.26200、x64、DPI 120、12th Gen Intel Core i5-12500H、约 15.7 GB 可见内存环境中，发布版启动后保持运行至少 3 秒，随后由测试进程结束；启动时进程约 61.1 MB 工作集、333 个句柄、无主窗口句柄（应用以托盘/宠物窗口生命周期运行）。
- 通过 `scripts/desktop-smoke.ps1` 在已解锁桌面实际创建并清空 1、10、50、100 个分层窗口；每档输出的活动实例/窗口数分别为 1/1、10/10、50/50、100/100，清空后每档均为 0/0。`cat` 和 `slime` 两套只含 `sprite.png` 的角色均完成真实启动/创建 smoke。本轮单轮 `cat` 日志 CREATE 工作集/句柄约为 1=57.8 MB/336、10=59.5 MB/337、50=64.7 MB/338、100=70.3 MB/338；CLEAR 后约为 58.4 MB/337、60.7 MB/337、65.4 MB/338、71.4 MB/338。该结果是当前基线和资源趋势记录，不是流畅性承诺；绝对值会受会话和加载状态影响。
- `scripts/desktop-input-smoke.ps1` 在已解锁桌面通过：真实屏幕上方掉落、落地、Walk、Jump/Apex/Fall；透明像素 `WindowFromPoint` 穿透到探针；实体像素命中宠物；拖拽移动、释放抛掷、捕获丢失；鼠标点击/拖拽不改变前台窗口；隐藏/恢复不抢焦点；清空和退出释放托盘图标。期间修复了 `WM_MOUSEACTIVATE -> MA_NOACTIVATE` 和无激活恢复窗口路径。 Direct real-mouse clicks on `NotifyIcon.ContextMenuStrip` items (open manager, hide, show, clear, exit) also passed.
- 同一进程五轮 1/10/50/100 创建/清空并在清空后强制 GC：活动实例/窗口始终归零，USER/GDI 回到约 23/24，句柄约 333—348；本次 cat 运行工作集约从 58.1 MB 增至 75.0 MB，私有字节约从 16.5 MB 增至 28.0 MB。原生窗口资源没有随轮次持续增长，但托管/进程分配趋势仍是风险，不能据此承诺长期流畅。
- 20-round same-process 1/10/50/100 baseline also exited 0: round 1 first CREATE was about WS 61.6 MB and PRIVATE 18.0 MB; round 20 final CLEAR was about WS 107.5 MB and PRIVATE 58.5 MB. Active instances/windows returned to zero every round, USER/GDI returned to 23/24, and handles stayed around 333 to 343. Shared images, window disposal, and handle cleanup are in place; 100 is a pressure tier, not a daily-count or long-term smoothness promise.

### Windows 桌面未验证

以下项目仍未完成可见桌面逐项验收，不能据此宣布 M1 全部验收通过：

- Tray menu item display and real mouse clicks are verified. The smoke directly displays `NotifyIcon.ContextMenuStrip` and clicks its items; invoking the menu from the system notification-area icon itself, and double-clicking that icon to open the manager, remain unverified.
- DEFERRED: the second-display/DPI desktop matrix is postponed because this session exposes only `DISPLAY1` (bounds 1536x864, working area 1536x816). It remains `Windows desktop unverified`, not a product failure or a reason to block M2 core work.
- 更长时间/更多轮次的资源趋势、CPU 曲线和 100 只流畅性仍未验证；当前数据只作为 M1 基线，不是性能承诺。

此前安全桌面遮罩导致的 `LockScreenBackstopFrame` 阻塞已消失；本轮输入矩阵通过的项目已移到“Windows 桌面已验证”。剩余项目仍保持未验证，不通过模拟或文档替代。

## 下一条可执行任务

Previous next-task note: implement M2 Task 3, validated user settings persistence and atomic recovery; that task is now complete. Keep the second-display/DPI matrix explicitly deferred.

## M2 Task 3 handoff (2026-10-04)

Task 3 is implemented in the working tree. `SettingsDocument` is a validated schema-v1 Core record; `SettingsStore` writes in the user-local `YimyWalk` directory through `settings.json.tmp`, flushes before replacement, keeps `settings.json.bak`, and reports whether defaults or the backup were used. The store never writes to the install directory and startup registration remains disabled.

The host loads settings before role selection, persists the selected role on manager changes and clean exit, applies the validated global scale to new pet windows, and shows settings recovery/save status in the manager. Preferred display is retained as validated data and is intentionally not applied until the display adapter task.

Evidence: `dotnet test YimyWalk.sln --configuration Release --no-restore` passed 53/53 Core tests; `scripts/build-yimywalk.ps1` passed with a publish output; `scripts/desktop-input-smoke.ps1` passed transparent hit-through, entity hit, drag/toss, focus, tray lifecycle, and manager reload entry; `scripts/desktop-smoke.ps1 -Rounds 1 -Role slime` passed settings default/save/backup recovery, reload success/failure retention, and real 1/10/50/100 create/clear. The current single-display Windows baseline reported final active instances/windows at zero and USER/GDI back at 10/24 after host disposal.

Status distinctions: code implemented; build passed; Core tests passed; Windows desktop settings and lifecycle smoke passed. The second-display/DPI matrix remains deferred/unverified (`DISPLAY1` only), and window-top landing is still unimplemented. Next executable task: M2 Task 4 display/work-area contracts and DPI-safe positioning.

## M2 Task 4 handoff (2026-10-04)

Task 4 display contracts and the Windows adapter are implemented. Core now owns immutable `DisplayId`, `DisplayInfo`, `IDisplayProvider`, and `DisplaySnapshot` values plus point-to-display resolution; `IWorldSurface` keeps a single-display fallback while allowing physics to use the working area for the display containing each pet. Negative coordinates remain signed throughout the Core geometry and App window positioning path.

The App adapter reads `Screen.AllScreens`, bounds, working areas, and device scale. It uses `GetDpiForWindow` when a host window handle exists, falls back to `GetDpiForSystem`, and returns a deterministic 1280x720 primary fallback if monitor enumeration fails. Snapshots are refreshed at most once per second during normal simulation. Desktop smoke output now records display ID, bounds, working area, scale, and primary status.

Evidence: `DisplayContractTests` and the full Core suite pass 56/56; `scripts/build-yimywalk.ps1` passes; `scripts/desktop-smoke.ps1 -Rounds 1 -Role cat` and `scripts/desktop-input-smoke.ps1` pass with display snapshots, fall/walk/jump, transparent/entity hit tests, drag/toss, focus, tray lifecycle, role reload, and final cleanup. The publish smoke saw one `DISPLAY1` at 1536x864 with a 1536x816 work area and scale 1; the input smoke saw one active `DISPLAY1` at 1920x1080 with a 1920x1020 work area and per-window scale 1.25. These are separate real sessions, so both values are recorded rather than merged.

Status distinctions: display contracts and adapter code are implemented; build and Core tests pass; single-display Windows desktop behavior is verified. Negative-coordinate and cross-display behavior is Core-tested but Windows desktop unverified because this session exposes no second display. Window-top landing remains unimplemented. Next executable task: M2 Task 5 conservative window-surface landing with floor fallback.
