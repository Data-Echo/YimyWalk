# YimyWalk 状态交接

更新时间：2026-10-05
当前分支：`codex/bootstrap`
Current target: M3.1 preparation. M2 foundation code and the available single-display Windows matrix are recorded below; second-display/DPI remains explicitly deferred.

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

## M2 hardening handoff (2026-10-05)

本轮补齐并验证了此前审查指出的 M2 缺口。Core 的 `PetEngine` 现在用单调时间戳计算受上限保护的 delta；窗口表面选择使用投影后的下一步位置；从实际窗口顶边走出会保持行走水平速度进入坠落；显示表面失效时会按当前可用工作区安全迁移。`WindowSurfaceResolver` 仍只读枚举窗口矩形，保守排除被更高窗口遮挡的顶边，不移动或关闭用户窗口。

管理器新增“允许宠物落到可见窗口顶部”真实开关，默认关闭并写入 `SettingsDocument.WindowLandingEnabled`；关闭管理器时隐藏而不销毁，托盘可重新打开，退出时由宿主统一释放。绘制缓存按角色定义引用计数，最后一个窗口释放后清理该定义的 Bitmap 和缓存项。窗口物理刷新使用单调时钟而不是固定 16ms 假定。

## M2 当前证据

**代码已实现**：角色刷新保留旧实例定义；共享定义、RGBA 与平台 Bitmap 生命周期分层；显示工作区/窗口顶部选择、管理器开关、设置恢复、实例清空和退出清理路径已实现。

**构建通过**：`scripts/build-yimywalk.ps1` 于 2026-10-05 通过，Core 测试、App x64 Release 编译和 publish 均为 0 警告/错误，发布目录为 `src/YimyWalk.App/bin/x64/Release/net8.0-windows/win-x64/publish/`。

**核心测试通过**：`dotnet test YimyWalk.sln --configuration Release --no-restore`，67/67 通过。新增覆盖上方生成不跳顶、投影坠落、走出窗口支撑、显示表面失效迁移、遮挡候选过滤和设置开关默认值。

**Windows 桌面已验证**：

- `scripts/desktop-input-smoke.ps1` 在解锁的 Windows 11 Home 10.0.26200、x64、DPI 120、Intel Core i5-12500H、单 `DISPLAY1`（1920×1080，工作区 1920×1020，窗口 DPI scale 1.25）通过真实掉落、Walk、Jump/Apex/Fall、透明穿透、实体命中、拖拽/抛掷、捕获丢失、焦点保持、托盘隐藏/显示/清空/退出、角色刷新成功/失败保留、管理器刷新按钮、落脚开关和关闭/重新打开管理器。
- `scripts/desktop-smoke.ps1 -Landing -Rounds 1 -Role slime` 在另一实际 DPI 会话（单 `DISPLAY1` 1536×864，工作区 1536×816，scale 1）通过真实 probe 窗口顶部 Y=300 落脚，关闭 probe 后继续坠落（Y=322.2），并通过 slime sprite-only 角色以及 1/10/50/100 创建/清空。100 只 CREATE 记录 WS 80,084,992、PRIVATE 23,703,552、HANDLES 375、USER 430、GDI 630；CLEAR 后 CACHE=0、USER 31、GDI 30；Host Dispose 后 WS 79,552,512、PRIVATE 23,613,440、HANDLES 365、USER 12、GDI 24。

**Windows 桌面未验证**：第二显示器、负坐标跨屏拖动、每显示器 DPI 元数据、显示器断开/重连和更长时间 CPU/内存曲线仍未验证。`WindowsDisplayAdapter` 当前的 `DisplayInfo.Scale` 是宿主窗口/系统 DPI 快照，不能解释为每个显示器已实测；单窗口 `GetDpiForWindow` 只用于窗口渲染缩放。100 只是压力基线，不是永久上限或流畅性承诺。

## 下一条可执行任务（M3）

执行 `docs/superpowers/plans/2026-10-05-m2-hardening-and-m3.md` 的 M3.1：在不引入窗口或桌面 API 的 Core 中实现 hunger/boredom 独立实例数值、增长率和人工喂食闭环；随后再做受限气泡和虚拟食物。外部应用反馈、真实快捷方式操作和第二显示器继续保持后续/暂缓状态。

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

## M2 Task 5 handoff (2026-10-04)

Task 5 is implemented. Core now has `WindowSurfaceCandidate`, `SurfaceSelection`, and `SurfaceSelector`; the selector chooses the nearest visible top edge crossed during descent and falls back to the display working-area floor. The App layer owns `WindowSurfaceResolver`, which refreshes top-level window rectangles at 250 ms or slower, excludes the host and manager windows, ignores desktop shell/system overlays and impractically small helpers, and never moves or closes user windows. Landing is opt-in and remains disabled for the normal M1 path.

Evidence: `SurfaceSelectionTests` and the full Core suite pass 59/59; `scripts/build-yimywalk.ps1` passes; `scripts/desktop-smoke.ps1 -Landing` passed on the unlocked Windows desktop with a real probe window (pet landed at top 300, then re-fell after probe close); `scripts/desktop-smoke.ps1 -Rounds 1 -Role cat` passed real 1/10/50/100 creation and clear with active windows returning to zero; `scripts/desktop-input-smoke.ps1` passed fall, walk, jump, transparent/entity hit testing, drag/toss, focus, tray, reload, and cleanup. The landing harness was placed at an unused right-side region and did not move or close existing user windows.

Status distinctions: Task 5 code is implemented, the publish build passes, Core tests pass, and single-display Windows landing is verified. The second-display/DPI matrix remains deferred and Windows-unverified because this session exposes only `DISPLAY1`; no M2 claim treats that matrix as passed. Next executable task: maintain the M2 baseline and add a second-display/DPI desktop run when such hardware is available.

说明：文档中较早的 M2 Task 3–5 handoff 保留为历史交接记录；本节的 2026-10-05 hardening handoff 是当前事实来源，已覆盖其中“窗口顶部落脚未实现”等过时描述。

## M3.1 handoff (2026-10-05)

**代码已实现**：`CharacterDefaults.HungerIncreasePerMinute` 现在真正保存在角色定义中，sprite-only 角色使用 0.01/分钟默认值；`PetInstance` 为每只宠物独立保存 Hunger、Boredom 和短暂 Eat 状态；`PetEngine` 使用受限 delta 更新数值，`PetSession` 提供 hunger 开关、0–4 倍增长率和 `FeedAll`。拖拽、跳跃、坠落或已有 Eat 状态时拒绝喂食，缺少专用动作图时继续使用基础帧。管理器提供真实的“启用饥饿增长”和“喂食全部”入口，设置会保存。

**构建与核心测试通过**：`scripts/build-yimywalk.ps1` 通过；`dotnet test YimyWalk.sln --configuration Release --no-restore` 通过 71/71。`PetNeedsTests` 覆盖默认值、配置加载、实例隔离、边界和开关。

**Windows 桌面已验证**：输入 smoke 真实点击并验证饥饿开关、喂食全部进入 Eat、管理器关闭/重开；批量 smoke 以 slime sprite-only 角色重新完成 1/10/50/100 创建和清空，100 CREATE 约 WS 78,209,024、PRIVATE 23,691,264、HANDLES 350、USER 428、GDI 632，CLEAR 后 ACTIVE/WINDOWS=0、CACHE=0、USER 29、GDI 32。

**范围边界（当时交接）**：气泡限流、虚拟食物目标和外部应用事件尚未实现；后续 M3.2/M3.3 已分别补齐前两项，第二显示器/DPI 真实矩阵仍暂缓。

## M3.2 handoff (2026-10-05)

**代码已实现**：Core `DialogueScheduler` 处理角色台词组回退、单宠物冷却、全局同时显示上限、过期和清空；App 的 `BubbleWindow` 使用非激活、工具窗口和 `HTTRANSPARENT`，由宿主随创建/点击/拖拽/喂食事件发布并在宠物删除、清空、关闭气泡或退出时释放。管理器新增“启用自动气泡”开关，默认开启并持久化。

**构建与核心测试通过**：`scripts/build-yimywalk.ps1` 通过；Core 74/74 通过。`DialogueSchedulerTests` 覆盖缺失组回退、冷却、全局上限、过期和清空。

**Windows 桌面已验证**：输入 smoke 真实观察创建气泡，切换自动气泡开关，且透明区域/实体命中、拖拽、焦点、托盘和管理器生命周期仍通过；批量 smoke 在加入气泡后重新完成 1/10/50/100 创建/清空。100 只 CREATE 约 WS 79,818,752、PRIVATE 24,182,784、HANDLES 350、USER 443、GDI 651；CLEAR 后 ACTIVE/WINDOWS=0、CACHE=0、USER 29、GDI 33。

**范围边界**：外部应用事件尚未实现；第二显示器/DPI 真实矩阵仍暂缓，不能标记为 Windows 桌面通过。

## M3.3 handoff (2026-10-05)

**代码已实现**：Core `VirtualFoodSystem` 只管理程序生成目标，限制目标数、单宠认领和 8 秒过期；中断移动会释放认领，接近后进入 Consumed、缩小并喂食实例。App 的 `FoodWindow` 只绘制小型非激活目标，管理器提供“生成虚拟食物”，一键清空和退出同时清理目标与窗口。

**构建与核心测试通过**：`scripts/build-yimywalk.ps1` 通过；Core 77/77 通过。`VirtualFoodTests` 覆盖单一认领、消费缩小、打断释放、目标上限和无使用者超时。

**Windows 桌面已验证**：输入 smoke 真实生成虚拟食物，创建宠物后完成认领/消费并进入 Eat，随后清空目标；透明穿透、实体命中、拖拽、焦点、气泡和托盘生命周期仍通过。批量 smoke 在虚拟食物代码加入后重新完成 slime 1/10/50/100 创建/清空。

**范围边界**：真实快捷方式操作仍属于后续阶段，外部应用事件尚未实现；第二显示器/DPI 真实矩阵仍暂缓。
