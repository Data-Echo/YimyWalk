# YimyWalk 状态交接

更新时间：2026-10-04
当前分支：`codex/bootstrap`
当前目标：M1 桌面基线，范围保持为 3×3 角色、Idle/Walk/Jump/Fall、工作区地面、拖拽/抛掷、透明宠物显示、托盘生命周期、多实例创建与清空。

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
- `YimyWalk.App`：一个实例一个透明分层窗口；同一宿主进程管理所有窗口；托盘菜单、管理器、1/10/50/100 按钮和分批创建队列；窗口透明区域和实体命中分别走 `WM_NCHITTEST`；窗口使用 `WS_EX_NOACTIVATE`。
- 角色包：`Characters/cat/sprite.png`、`Characters/slime/sprite.png`，均只包含 `sprite.png`。

### 构建通过

- `dotnet restore YimyWalk.sln`：通过。
- `dotnet test YimyWalk.sln --configuration Release --no-restore`：通过，21 个核心测试。
- `dotnet build src/YimyWalk.App/YimyWalk.App.csproj --configuration Release -p:Platform=x64`：通过。
- `scripts/build-yimywalk.ps1`：通过，发布到 `src/YimyWalk.App/bin/x64/Release/net8.0-windows/win-x64/publish/`，并复制角色目录。

### 核心测试通过

- 角色格式、加载校验、物理状态和边界、共享资源生命周期、实例创建/清空均通过。
- `YimyWalk.Core` 程序集没有引用 WinForms、WindowsBase 或 PresentationCore。

### Windows 桌面已验证

- 在本机 Windows 11 Home 10.0.26200、x64、DPI 120、12th Gen Intel Core i5-12500H、约 15.7 GB 可见内存环境中，发布版启动后保持运行至少 3 秒，随后由测试进程结束；启动时进程约 57.7 MB 工作集、313 个句柄、无主窗口句柄（应用以托盘/宠物窗口生命周期运行）。

### Windows 桌面未验证

以下项目仍未完成可见桌面逐项验收，不能据此宣布 M1 全部验收通过：

- 透明背景点击穿透与可见实体命中分别通过真实测试窗口验证。
- 拖拽、释放抛掷、焦点不抢占、托盘隐藏/恢复、退出后的窗口/计时器/绘制资源清理。
- 屏幕上方掉落、实际行走/跳跃/下落和 1、10、50、100 实例真实创建与一键清空。
- 重复创建/清空后的 CPU、内存、USER/GDI/句柄趋势，多显示器和 DPI 适配。

本环境可以构建并启动 Windows GUI，但本轮没有执行上述交互矩阵；这些条目保持未验证状态。

## 下一条可执行任务

在可见 Windows 桌面会话中启动发布版，按 `docs/TESTING.md` 的矩阵完成 1、10、50、100 创建/清空、透明命中、拖拽抛掷、焦点和资源趋势记录；发现问题后先写失败复现，再修复并重新运行核心测试和发布构建。

