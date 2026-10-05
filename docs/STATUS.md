# 当前状态（2026-10-05，v0.1.4 EXE 发布包）

## v0.1.4 跨屏行走与最大化窗口修复（2026-10-05）

- **代码实现**：`DisplaySnapshot` 和 `IWorldSurface` 提供所有显示器工作区的全局水平范围；`PetEngine` 在全局左右边界循环并保留当前显示器的落脚高度，因此宠物可从一块水平排列的显示器走到另一块。窗口落脚候选新增 `IsMaximized`，Windows `IsZoomed` 或覆盖工作区的窗口会被排除，普通窗口仍可落脚。
- **构建通过**：Core、App 和 self-contained single-file `YimyWalk.exe` 的 `v0.1.4` 发布构建通过，发布包自检通过（12 个文件、11 个 SHA-256 校验项）。
- **Core 测试通过**：完整 Core 测试 **94/94** 通过，覆盖全局水平范围、左右循环、跨显示器间隙和最大化候选过滤。
- **Windows 桌面已验证**：针对性 smoke 创建真实最大化探针并确认其候选被拒绝；在当前 `DISPLAY1`（0–1536）与 `DISPLAY2`（1920–4480）拓扑下，宠物从第一块屏幕工作区右边缘走入第二块屏幕；原有角色刷新和 1/10/50/100 创建清空检查继续通过。
- **Windows 桌面未验证**：没有重复既有 83/83 双显示器矩阵；垂直排列显示器、负坐标布局、物理拔插、睡眠唤醒、更长时间资源趋势、权限受限会话和真实任务栏变化仍未验证。跨屏 smoke 只覆盖当前可见的水平排列拓扑，不能替代这些场景。
- **远程与归档**：`codex/bootstrap` 实现提交为 `0ba19a6`，`main` 发布提交为 `bea182d`，均已推送；`v0.1.4` 标签已推送。发布压缩包和 `SHA256SUMS.txt` 位于 `D:/Project/Cursor/YimyWalk-main/artifacts/release/v0.1.4/`。

## v0.1.3 应用图标修复（2026-10-05）

- **代码实现**：新增嵌入式 `YimyWalk.App.Resources.yimywalk.ico` 加载器；管理窗口 `Form.Icon` 和托盘 `NotifyIcon.Icon` 现在都使用该资源，和 EXE 文件图标保持一致；托盘图标由宿主显式释放。
- **构建通过**：Core、App 和 self-contained single-file `YimyWalk.exe` 发布均通过，包版本为 `0.1.3`，发布包自检通过（12 个文件、11 个 SHA-256 校验项）。
- **Core 测试通过**：完整 Core 测试 **89/89** 通过；图标回归断言位于 `tools/DesktopSmoke`，不把 WinForms 依赖引入 Core。
- **Windows 桌面已验证**：可见桌面 smoke 已确认嵌入资源存在，管理窗口和托盘图标逐像素匹配同一资源；发布 EXE 直接启动保持运行通过，已有创建/清空、角色刷新和资源释放检查继续通过。
- **Windows 桌面未验证**：本次没有重复双显示器 83/83 矩阵；任务栏通知区域的肉眼截图、负坐标显示器、物理拔插、睡眠唤醒、更长时间资源趋势和权限受限会话仍按原边界标记未验证。输入 smoke 因当前用户已有 `dialogueEnabled=false` 在气泡前置断言处失败，未修改用户设置，也未计为通过。
- **远程与归档**：`codex/bootstrap` 修复提交为 `bc130d5`，`main` 发布提交为 `0c9f106`，均已推送；`v0.1.3` 标签已推送。发布压缩包和 `SHA256SUMS.txt` 位于 `D:/Project/Cursor/YimyWalk-main/artifacts/release/v0.1.3/`。

## 发布分支与 v0.1.2（2026-10-05）

- **开发分支**：`codex/bootstrap` 保留完整源码、Core 测试、Desktop smoke、作者工具、上游审计快照和阶段文档。
- **标准主分支**：`main` 只保留产品源码、角色、作者格式、必要构建/启动脚本、许可证和发布说明；不包含 `tests/`、`tools/`、`vendor/`、本地 SDK、smoke 脚本、测试日志或开发计划。
- **主分支可构建性**：精简后的 `main` 使用只含 `YimyWalk.Core` 与 `YimyWalk.App` 的产品解决方案；在不复制 `.tools/` 且不含测试工程的工作树中，使用外部 .NET 8 SDK 完成 restore/build/publish，脚本会明确跳过不存在的测试项目。
- **Release 产物**：本次发布为 `v0.1.2`，入口为自包含单文件 `YimyWalk.exe`；发布包同时包含 `Characters/`、许可证、版本说明、manifest 和 SHA-256 清单。本地压缩包位于 `artifacts/release/v0.1.2/`，包含源码包、Windows x64 包和 `SHA256SUMS.txt`。
- **远程状态**：已推送 `main`、`codex/bootstrap` 和 `v0.1.2` 标签到 `origin`；没有修改系统设置、开机启动或真实桌面文件，也没有创建在线 Release 页面。

## M4.2 发布包与启动自检实际结果（当前事实来源）

- **代码已实现**：Core 新增 `StartupDiagnostics`，为角色根目录缺失、空目录和全部角色无效提供稳定错误码、原因和恢复方法；AppHost 在启动计时器前执行检查，Program 捕获启动失败并显示诊断，同时把摘要写入用户本地 `YimyWalk/startup-error.log`。`run-yimywalk.ps1` 检查 `YimyWalk.exe`、`Characters` 和至少一个 `sprite.png`；自包含发布包不依赖外部 .NET Runtime，仍保留旧 framework-dependent 开发输出的 runtime 诊断兼容。
- **发布包已实现**：`scripts/build-yimywalk.ps1` 清理目标发布目录后 restore/test/publish，生成 self-contained single-file `YimyWalk.exe`，复制 `Characters/`、许可证和版本说明，并生成 `PACKAGE_README.txt`、`PACKAGE_MANIFEST.json` 与 `PACKAGE_CHECKSUMS.txt`；`scripts/publish-yimywalk.ps1` 使用同一流程输出发布目录。
- **构建通过**：`scripts/build-yimywalk.ps1` 已通过；`YimyWalk.exe` 文件版本为 `0.1.2.0`，包清单标记 `selfContained=true`，发布目录 12 个文件、11 个 SHA-256 校验项，`scripts/test-release-package.ps1` 通过。
- **核心测试通过**：`.tools/dotnet/dotnet.exe test YimyWalk.sln --configuration Release --no-restore` 通过 **89/89**，其中新增 `StartupDiagnosticsTests` 4 项；Core 仍未引用 WinForms 或桌面绘制对象。
- **Windows 桌面已验证**：可见桌面上 `scripts/desktop-smoke.ps1 -Rounds 1 -Role slime` 通过，真实启动发布版宿主并完成 1/10/50/100 创建/清空、角色刷新成功/失败保留和资源计数回落；本轮 100 只清空后 `ACTIVE=0`、`WINDOWS=0`、`CACHE=0`、`USER=29`、`GDI=33`，Host Dispose 后 `USER=10`、`GDI=27`。另用 `run-yimywalk.ps1 -PublishPath artifacts/publish/yimywalk-win-x64` 启动发布包约 2 秒后由测试进程结束，入口预检通过。
- **Windows 桌面未验证**：本任务没有重新执行已通过的双显示器 83/83 矩阵；负坐标显示器、物理拔插、睡眠唤醒、更长时间 CPU/内存曲线、权限受限会话和更高实例压力仍保持未验证。本次输入 smoke 因当前用户已有 `dialogueEnabled=false`，在“新建宠物出现对话气泡”的前置断言处失败，未计为通过，也未修改用户设置。发布/核心/启动预检证据不能替代这些真实桌面验收。

## M4.3 稳定性回归实际结果（2026-10-05）

- **代码已实现**：未改变运行时行为；`tools/DesktopSmoke` 增加 `CPU_S` 累计处理器秒数输出，使长时资源记录同时包含 CPU、工作集、私有字节、托管堆、句柄、USER 和 GDI。输入 smoke 仍使用现有真实刷新、隐藏/恢复、清空和退出入口。
- **构建与核心测试通过**：M4.2 发布脚本在 smoke 前构建 DesktopSmoke 为 0 警告/错误；完整 Core 测试仍为 89/89。M4.3 没有引入新的生产程序集依赖。
- **Windows 桌面已验证**：`scripts/desktop-smoke.ps1 -Rounds 20 -Role slime` 在同一宿主进程完成 20 轮、160 次 CREATE 和 80 次 CLEAR；每次清空均为 `ACTIVE=0 WINDOWS=0 CACHE=0`，USER/GDI 回到 29/33，Host Dispose 后为 10/27。第 1 轮首个 CREATE 为 `CPU_S=0.438 WS=70,504,448 PRIVATE=19,070,976 HANDLES=352`，第 20 轮结束为 `CPU_S=55.234 WS=118,947,840 PRIVATE=63,967,232 HANDLES=364`。输入 smoke 在恢复默认气泡设置后连续两轮通过真实角色刷新、清空、托盘退出、气泡/食物/外部进程反馈和资源释放；原用户设置随后恢复，发布包运行前后清单哈希不变。完整趋势日志保存在 `artifacts/m43-stability-cpu.log`。
- **Windows 桌面未验证**：20 轮显示工作集和私有字节仍有明显进程分配器增长，因此不能宣称长期无泄漏或流畅；更长时间/更高压力、权限受限会话、真实快捷方式、负坐标显示器、拔插显示器和睡眠唤醒仍未验证。第一次输入 smoke 因持久化 `dialogueEnabled=false` 直接断言失败，已记录为测试前置条件问题，未把该次失败改写为通过。

## M4.2 双显示器/DPI 实际结果

- **代码已实现**：`WindowsDisplayAdapter` 为每块屏幕读取独立 DPI；应用启动声明 `PerMonitorV2`；`DesktopWorldSurface` 按宠物坐标解析显示器缩放；`PetWindow` 在目标显示器上创建/调整尺寸并使用 `AutoScaleMode.Dpi`，跨 DPI 渲染尺寸和透明命中保持一致。新增 `tools/DesktopDisplaySmoke` 与 `scripts/desktop-display-smoke.ps1`，仍沿用一个宿主进程、一个宠物一个窗口的结构。
- **构建通过**：`scripts/build-yimywalk.ps1` 通过；双屏 smoke 工具 x64 Release 构建 0 警告、0 错误。
- **核心测试通过**：`scripts/test-yimywalk.ps1` 通过 **85/85**；Core 未引用 WinForms 或桌面绘制对象。
- **Windows 桌面已验证**：在 Windows 11 Home 10.0.26200、x64、16 logical CPUs、Intel Core i5-12500H 的已解锁桌面上，`scripts/desktop-display-smoke.ps1 -Rounds 3` 返回 `RESULT PASS CHECKS=83 FAILURES=0`。`DISPLAY1` 为 1920×1080、工作区 1920×1020、DPI 120（125%）；右侧 `DISPLAY2` 为 2560×1440、工作区 2560×1392、DPI 96（100%）。cat/slime 在两块屏幕上的生成、掉落、落地、行走、跳跃、透明区域穿透、实体命中、拖拽释放、焦点保持、global scale=2，以及双向跨屏拖拽均通过；3 轮双屏创建/清空后活动实例、窗口和 cache 均归零。
- **资源趋势**：最终三轮清空 GDI 为 41、46、51，USER 为 15，句柄为 323、324、324；工作集/私有字节保留进程分配器趋势。`AutoScaleMode.Dpi` 将跨 DPI 创建/销毁的 GDI 增长从早期约每轮 +40 降到本轮约 +5，但仍不能据此宣称无泄漏或 100 只长期流畅。完整日志：`artifacts/dual-display-final.log`。
- **Windows 桌面未验证**：本次拓扑没有负坐标屏幕，因此负坐标布局未验证；物理断开/重连、睡眠/唤醒、任务栏布局变化、更长时间 CPU/内存曲线和更高实例压力未执行。它们继续保持未验证，不能用 Core 或普通窗口动画替代。

## 下一条可执行任务

继续处理 M4.3 残余验证债务：获取更长时段 CPU/内存/句柄曲线和权限受限会话证据；若再次取得负坐标或拓扑变化环境，再运行对应真实桌面矩阵。真实快捷方式读写、自启动和系统设置修改仍不在当前范围。

## M4.1 实际结果

- **代码已实现**：新增 `tools/CharacterValidator`（net8.0），直接引用 `YimyWalk.Core.CharacterLoader`；提供角色诊断、3×3 预览 PNG 和网格/默认脚底锚点参考 PNG。Core、作者工具和绘制适配层都可使用 ImageSharp 解码/转换像素；Core 仍不持有窗口、Bitmap 或其他平台绘制对象。
- **作者材料已实现**：`docs/CHARACTER_FORMAT.md` 已按当前实现区分 supported/planned；`examples/character-template` 提供配置和 UTF-8 对白示例；`Characters/cat`、`Characters/slime` 保持 sprite-only，可直接加载；`docs/assets/` 保存可重复生成的 cat/slime 预览和网格参考图。
- **入口已实现**：`scripts/validate-character.ps1` 支持空格/中文路径、可选预览和参考图，验证失败返回非零；不会修改输入角色文件。未知 JSON 属性当前会忽略，`actions.png` 尚未实现，文档已明确说明。
- **构建通过**：`.tools/dotnet/dotnet.exe test YimyWalk.sln --configuration Release --no-restore` 通过 85/85；`scripts/build-yimywalk.ps1` 通过并生成 x64 framework-dependent 发布目录。
- **Core/工具验证通过**：已运行 cat、slime、template、含空格和中文路径的角色；坏 PNG 返回 1；预览/参考图尺寸、参考图透明区域与锚点断言通过；输入 sprite 的 SHA-256 前后不变。
- **Windows 桌面状态**：本任务没有新增桌面行为，M4.1 的作者工具不需要 GUI 验收。既有单显示器桌面证据仍按历史交接保留；第二显示器/DPI 矩阵继续标记为 **Windows 桌面未验证（用户已暂缓）**。核心测试或普通窗口预览不能替代该验收。

## 仍需单独处理的验证债务

历史 M3 交接中的“代码已实现”不等于所有需求已验收：无聊状态尚未驱动完整行为决策，走路自然结束/高级动作仍有限；外部应用 smoke 的台词断言不能证明真实事件；气泡点击穿透、虚拟食物窗口层级/目标过期与路径约束仍需独立回归；长时 CPU/内存/句柄趋势和权限受限场景未完成。后续稳定性任务必须针对这些证据补强，不能用文档或模拟测试改写为通过。

## 下一条可执行任务

执行 M4.2：完善可重复 publish/package、第三方许可和启动自检诊断；保持不联网、不自启动、不修改真实桌面文件，并在完成后再次区分代码、构建、Core 测试和 Windows 桌面验证状态。

# YimyWalk 状态交接

更新时间：2026-10-05
当前分支：`codex/bootstrap`
Current target: M4 stabilization planning. M2 foundation and M3 core fun loop are implemented; available single-display Windows evidence is recorded below, while second-display/DPI remains explicitly deferred.

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

**范围边界（M3.3 交接时）**：真实快捷方式操作仍属于后续阶段，外部应用事件尚未实现；随后已补上 M3.4 的可选轮询代码，但真实事件矩阵仍未验证；第二显示器/DPI 真实矩阵仍暂缓。

## M3.4 handoff (2026-10-05)

**代码已实现**：应用切换反馈设置默认关闭；启用后 AppHost 每 500ms 只读取前台窗口所属进程 ID，排除自身进程并在进程切换时复用 `program_opened` 台词组。没有读取窗口标题、路径、文档正文、键盘输入或浏览历史；关闭开关后停止轮询。

**构建与核心测试通过**：`scripts/build-yimywalk.ps1` 通过；Core 77/77 通过，设置 round-trip 覆盖默认关闭和启用保存。

**Windows 桌面已验证**：输入 smoke 真实切换“启用应用切换反馈”开关，并启动可关闭的 Notepad 临时进程验证前台进程切换产生台词气泡；测试只使用进程 ID，结束后关闭该进程，未读取标题、路径或文件内容。其他 M3 输入、气泡、虚拟食物和生命周期检查通过。

**Windows 桌面未验证**：更长时间、多进程快速切换和权限受限会话仍未验证；第二显示器/DPI 仍暂缓。

## 下一条可执行任务

执行 M3.5 回归交接：重复完整核心/构建/输入/批量 smoke，整理资源和权限边界；外部事件的基础单进程切换 smoke 已通过，更长时间与权限矩阵继续保留未验证。

## M3.5 regression handoff (2026-10-05)

**代码已实现**：M3.1 饥饿/无聊/人工喂食、M3.2 限流气泡、M3.3 虚拟食物、M3.4 可选前台进程反馈均已保留在 Core/App 分层；清空、角色刷新和退出路径覆盖了新增事件、食物和气泡资源。

**构建与核心测试通过**：`dotnet test YimyWalk.sln -c Release --no-restore` 77/77；`scripts/build-yimywalk.ps1` 通过并生成 x64 self-contained publish；`git diff --check` 无内容错误（仅换行格式提示）。

**Windows 桌面已验证**：在本轮解锁桌面上，`scripts/desktop-input-smoke.ps1` 通过真实掉落/行走/跳跃/坠落、透明穿透/实体命中、拖拽抛掷、焦点、托盘、角色刷新、管理器开关、创建气泡、虚拟食物认领消费进入 Eat、Notepad 进程切换反馈和退出；`scripts/desktop-smoke.ps1 -Rounds 1 -Role slime` 通过 sprite-only slime、1/10/50/100 创建清空、CACHE=0 和资源计数回落。最近一轮 100 只 CREATE 为 WS 80,900,096、PRIVATE 25,075,712、HANDLES 350、USER 443、GDI 651；CLEAR 为 ACTIVE/WINDOWS=0、CACHE=0、USER 29、GDI 33；Host Dispose 后 USER 10、GDI 27。

**Windows 桌面未验证**：第二显示器/负坐标跨屏/DPI 切换/断开重连、长时间 CPU/内存曲线、权限受限外部事件和更高实例压力仍未验证；100 仍只是压力档位，不是永久上限或流畅性承诺。真实快捷方式读写、移动、隐藏和恢复没有实现，也没有触碰用户桌面文件。

## 下一阶段

已写入 `docs/superpowers/plans/2026-10-05-m4-stabilization.md`。下一条可执行任务是 M4.1：整理角色作者材料、校验入口和发布包自检；随后再处理真实验收中发现的稳定性问题。第二显示器和真实快捷方式继续作为单独的未验证/后续项目。
