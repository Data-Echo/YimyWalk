# 主动运动与预览实施计划

> **For agentic workers:** Use subagent-driven-development for the independent App/UI task; execute the coupled physics and integration work in this session. 用户已授权连续实施，不再设设计确认关卡。

**Goal:** 交付经过可重复测试的跳跃/攀爬欲望、物理可达窗口跳跃、屏幕顶部运动、slime-only 内置包与 idle1 自适应预览。

**Architecture:** Core 保存调度与解析物理，Windows 适配提供窗口/显示器快照。App 持久化全局值并在互动页和桌宠页编辑；预览只在 UI 层绘制。

**Tech Stack:** C# .NET 8 / WinForms / AntdUI / xUnit / PowerShell。

## 1. Core 设置与运动

- [ ] 在 `tests/YimyWalk.Core.Tests/AutonomousMovementTests.cs`、`JumpTargetTests.cs` 和 `SettingsDocumentTests.cs` 增加失败用例：旧配置默认、范围、继承重置、失败等待、暂停无补发、关闭走动、水平不可达、下降交会/终端速度、目标失效、两侧攀爬/顶部释放/窗口承接、拓扑变化。
- [ ] 运行 `.tools/dotnet/dotnet.exe test tests/YimyWalk.Core.Tests -c Release --filter "FullyQualifiedName~Autonomous|FullyQualifiedName~JumpTarget|FullyQualifiedName~SettingsDocument"`，确认缺失行为导致失败。
- [ ] 修改 `SettingsDocument`、`PetBehaviorOverrides`、`PetInstance`、`PetSession`、`PetEngine`、`SimulationContracts`；新增 `BallisticMotion`、`JumpTargetPlanner`、`AutonomousMovement`。协议：`JumpDesire/ClimbDesire` 是 0–1；覆盖为可空值；`IWorldSurface.JumpCandidates` 默认空；渲染缩放通过 `PetSession.GlobalScale` 与 DisplayInfo.Scale 解析。
- [ ] 解析位移使用 `y += v*t + g*t*t/2`，超出终端速度时分段积分；水平位移保持 `vx*t`。规划器按下降根时间求有效 X 区间与限速的交集，成功后仅设置初速度。
- [ ] 新攀附状态绑定 display ID/工作区/缩放；不满足原快照时解除并 Fall。顶部边界、释放时间和安全边距按设计补充规则实现。
- [ ] 运行 Core 全套；保留已有重力/跨屏断言，仅把依赖旧欧拉误差的数值改为解析积分期望，并新增细分步长一致性证明。

## 2. App/UI（独立任务）

- [ ] `AppHost` 暴露并持久化 JumpDesire/ClimbDesire，初始化和更新 Session.GlobalScale；`DesktopWorldSurface` 暴露刷新过且受窗口开关限制的 JumpCandidates。
- [ ] `InteractionsPage` 增加百分比控件；`PetsPage` 增加继承/自定义值和实际来源，不破坏实例大小可见性。
- [ ] `CharacterFramePreview` 使用原尺寸 idle1 缓存，自绘等比 fit，不影响实际宠物大小；先用 `tools/ManagerUiSmoke` 复现高分辨率四角裁切，再修复。
- [ ] App smoke 检验不同帧比例和控件/DPI尺寸下真实像素可见性、百分比/继承/重置、删除/刷新/托盘和缓存释放。

## 3. 角色与发布

- [ ] 用 `scripts/validate-character.ps1` 校验用户指定优化 PNG；校验失败不得替换。复制后 SHA-256 一致；内置 cat 移到 `tests/Fixtures/Characters/cat`；发布仅复制显式内置 slime。
- [ ] smoke 需要第二角色时在临时目录复制 fixture，不能污染发布目录；保留多角色覆盖。
- [ ] 构建输出新目录 `artifacts/publish/autonomous-movement`，保护用户在旧 publish 中添加的角色。
- [ ] `.tools/dotnet/dotnet.exe test YimyWalk.sln -c Release --no-restore`、App/UI smoke 构建/运行、`scripts/build-yimywalk.ps1 -Version 0.1.9 -PublishPath artifacts/publish/autonomous-movement`、`scripts/test-release-package.ps1`。

## 4. 审查与交接

- [ ] 独立审查需求符合性与代码质量，修复重要问题；`git diff --check`。
- [ ] 更新设计、`docs/STATUS.md`、资源说明和测试说明，分别记录 Core、UI 像素绘制、真实 Windows 桌面验证；未运行的桌面行为不得记为通过。
