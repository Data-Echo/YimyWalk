# YimyWalk 主菜单 UI Shell Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** 在保持现有桌宠 Core、透明窗口和角色资源生命周期的前提下，交付普通任务栏主菜单窗口、五页 WinForms 导航、全局默认/单实例覆盖语义和可验证的最小 UI。

**Architecture:** 继续使用 WinForms。ManagerForm 负责任务栏窗口、侧栏导航、页面缓存和关闭/退出生命周期；每个页面是独立 UserControl，通过 AppHost 的业务门面读写状态。Core 增加可测试的实例行为覆盖与统一的生效值解析；全局设置继续由 SettingsDocument/SettingsStore 校验和原子保存。UI 不直接操作文件、HWND、Bitmap 或 Core 私有字段。

**Tech Stack:** .NET 8, C# WinForms, YimyWalk.Core, xUnit, PowerShell smoke scripts, existing FrameSurfaceCache and desktop adapters.

## 当前执行记录（2026-10-05）

- Task 1–2：已实现并提交；Core 行为覆盖和 AppHost 保存门面已通过 101 项 Core 测试及设置契约 smoke。
- Task 3：已实现普通任务栏主窗体、200px 侧栏、五页缓存、启动总览、关闭转最小化和显式退出。
- Task 4–5：已实现实例列表的 InstanceId 选择、三态/数值覆盖、idle1/index 0 预览、角色显式设为下次创建、互动全局默认、设置缩放/显示器和真实保存状态。
- Task 6：本轮以现有 WinForms DPI 布局和增量列表实现完成最小回归；更长时间资源曲线、负坐标显示器、系统结束会话和物理拔插仍需独立桌面回归。
- 设置页不显示 `WarningLimit`：该字段继续由 `SettingsDocument` 校验并保留兼容性，但当前没有对应运行时警告行为，避免提供无效控件。

每个任务的完成证据仍按代码实现、构建通过、逻辑测试通过和 Windows 桌面验证分别记录；`desktop-input-smoke.ps1` 若遇到桌面遮罩阻塞透明穿透，会返回未验证状态，不以 Core 测试替代。

---

## 范围和执行顺序

本计划只覆盖 M4 UI 最小实现：设置语义与行为闭环、任务栏主窗口、五页导航、单帧预览、队列/选择维护、真实保存状态和桌面回归。WPF、WinUI、WebView、通知中心、主题编辑器、完整角色编辑器和真实快捷方式操作不进入任务。

每个任务先写失败测试或可复现 smoke 断言，再实现最小代码，随后运行针对性验证并提交。任务之间不共享未提交的半成品。

## 文件映射

- Core 行为与设置：src/YimyWalk.Core/Settings/SettingsDocument.cs、src/YimyWalk.Core/Simulation/PetInstance.cs、src/YimyWalk.Core/Simulation/PetEngine.cs、src/YimyWalk.Core/Simulation/PetSession.cs、src/YimyWalk.Core/Simulation/PetBehaviorOverrides.cs。
- Core 测试：tests/YimyWalk.Core.Tests/SettingsDocumentTests.cs、tests/YimyWalk.Core.Tests/PetBehaviorOverrideTests.cs、tests/YimyWalk.Core.Tests/PetPhysicsTests.cs。
- App 设置门面：src/YimyWalk.App/Host/AppHost.cs、src/YimyWalk.App/Settings/SettingsSaveCoordinator.cs。
- App UI 壳体：src/YimyWalk.App/UI/ManagerForm.cs、src/YimyWalk.App/UI/UiTheme.cs、src/YimyWalk.App/UI/ManagerPage.cs、src/YimyWalk.App/UI/Pages/OverviewPage.cs、src/YimyWalk.App/UI/Pages/PetsPage.cs、src/YimyWalk.App/UI/Pages/CharactersPage.cs、src/YimyWalk.App/UI/Pages/InteractionsPage.cs、src/YimyWalk.App/UI/Pages/SettingsPage.cs。
- App UI 状态与预览：src/YimyWalk.App/UI/PetListProjection.cs、src/YimyWalk.App/UI/InstanceSettingState.cs、src/YimyWalk.App/UI/Controls/CharacterFramePreview.cs、src/YimyWalk.App/Rendering/FrameSurfaceCache.cs。
- Windows smoke：tools/DesktopInputSmoke/Program.cs、scripts/desktop-input-smoke.ps1、tools/DesktopSmoke/Program.cs。
- 文档：docs/superpowers/specs/2026-10-05-ui-shell-design.md、docs/STATUS.md。

### Task 1: 定义全局默认、运行级限制和实例覆盖

**Files:**
- Create: src/YimyWalk.Core/Simulation/PetBehaviorOverrides.cs
- Modify: src/YimyWalk.Core/Settings/SettingsDocument.cs
- Modify: src/YimyWalk.Core/Simulation/PetInstance.cs
- Modify: src/YimyWalk.Core/Simulation/PetEngine.cs
- Modify: src/YimyWalk.Core/Simulation/PetSession.cs
- Test: tests/YimyWalk.Core.Tests/PetBehaviorOverrideTests.cs
- Test: tests/YimyWalk.Core.Tests/SettingsDocumentTests.cs
- Test: tests/YimyWalk.Core.Tests/PetPhysicsTests.cs

- [ ] **Step 1: 写失败的设置解析测试**

新增测试必须先锁定以下行为：

    [Fact]
    public void BooleanOverridesResolveAgainstGlobalDefault()
    {
        Assert.True(BooleanOverride.Inherit.Resolve(true));
        Assert.True(BooleanOverride.Enabled.Resolve(false));
        Assert.False(BooleanOverride.Disabled.Resolve(true));
    }

    [Fact]
    public void ActivityOverrideUsesGlobalWhenInheritedAndClampsCustomValue()
    {
        Assert.Equal(1.5f, PetBehaviorOverrides.Inherit.ResolveActivityRate(1.5f));
        Assert.Equal(4f, new PetBehaviorOverrides(ActivityRate: 9f).ResolveActivityRate(1.5f));
    }

    [Fact]
    public void DisabledWalkingDoesNotDisableGravityOrDragging()
    {
        // create an existing test pet, set WalkOverride.Disabled,
        // advance from Idle, then assert it remains out of Walk while
        // BeginDrag/ReleaseDrag and Fall still change position/state.
    }

    SettingsDocumentTests must assert DefaultWalkEnabled and DefaultDialogueEnabled default to true, ActivityRate and HungerRate stay in 0.1–4.0, and invalid finite/range values are rejected.

- [ ] **Step 2: 运行针对性测试确认失败**

运行：

    .tools/dotnet/dotnet.exe test tests/YimyWalk.Core.Tests/YimyWalk.Core.Tests.csproj --configuration Release --filter FullyQualifiedName~PetBehaviorOverrideTests

Expected: FAIL because the override types and new settings fields do not exist.

- [ ] **Step 3: 实现最小 Core 合约**

新增 BooleanOverride（Inherit/Enabled/Disabled）、PetBehaviorOverrides 和统一 Resolve 方法。数值覆盖使用 null 表示跟随全局，非 null 只接受 0.1–4.0；不要把数值覆盖编码成布尔三态。

SettingsDocument 增加 DefaultWalkEnabled 和 DefaultDialogueEnabled，缺失 JSON 字段按默认值补齐，保留现有 DialogueEnabled 作为运行级总限制。PetInstance 持有可变 PetBehaviorOverrides；覆盖属于实例，不放入 CharacterDefinition。

PetEngine/PetSession 暴露 DefaultWalkEnabled 和 ActivityRate。ActivityRate 只调整自主行为决策频率/倾向：以角色 IdleSecondsMin/Max 为基础，按有效倍率计算下一次自主决策时机；不改变 WalkSpeed、Gravity、MaxFallSpeed 或动画 FPS。WalkOverride.Disabled 只阻止自主进入 Walk，不阻止拖拽、重力、坠落、落地和手动交互。

- [ ] **Step 4: 运行 Core 测试确认通过**

运行同一 filter，然后运行：

    .tools/dotnet/dotnet.exe test YimyWalk.sln --configuration Release --no-restore

Expected: 新增测试和原有测试全部通过，原有物理行为没有回归。

- [ ] **Step 5: 提交**

    git add src/YimyWalk.Core tests/YimyWalk.Core.Tests
    git commit -m "feat: add scoped pet behavior settings"

### Task 2: 接入 AppHost 业务门面和真实保存状态

**Files:**
- Create: src/YimyWalk.App/Settings/SettingsSaveCoordinator.cs
- Modify: src/YimyWalk.App/Host/AppHost.cs
- Modify: src/YimyWalk.App/Settings/SettingsStore.cs only if save-status plumbing requires it
- Test: tools/DesktopInputSmoke/Program.cs
- Test: tools/DesktopSmoke/Program.cs

- [ ] **Step 1: 先增加失败的业务断言**

在 DesktopInputSmoke 中增加反射断言：

    Require(Property(host, "DefaultWalkEnabled") is bool, "global default walk setting exists");
    Require(Property(host, "DefaultDialogueEnabled") is bool, "global default dialogue setting exists");
    Require(Property(host, "DialogueEnabled") is bool, "global dialogue gate exists");
    Require(Property(host, "SettingsSaveState") is not null, "global save status exists");

在 DesktopSmoke 的清空路径加入断言：清空后 ACTIVE=0、WINDOWS=0，且等待旧的 batch interval 后仍然为 0。

- [ ] **Step 2: 运行 smoke 确认失败**

运行：

    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-input-smoke.ps1

Expected: 新增属性/状态断言失败，原有输出保留，失败不计为桌面通过。

- [ ] **Step 3: 实现统一 AppHost 业务入口**

AppHost 增加 DefaultWalkEnabled、DefaultDialogueEnabled、ActivityRate、DialogueEnabled、ExternalEventsEnabled 的统一读写入口；每个入口先更新运行时，再将需要持久化的全局设置交给 SettingsSaveCoordinator。

SettingsSaveCoordinator 维护 Applied、Saving、Saved、Failed 状态；离散开关立即保存，ActivityRate/HungerRate/Scale 等连续值使用 250–400ms 合并写入。保存失败不回滚用户输入，返回“已应用于本次运行，保存失败”和可再次触发的重试方法。

PetSession/PetEngine 在创建实例时应用全局默认，既有实例只在对应覆盖为 Inherit 时读取默认。PublishDialogue 的顺序固定为：全局 DialogueEnabled gate → 实例 DialogueOverride/DefaultDialogueEnabled → DialogueScheduler；全局 gate 为 false 时强制开启不能绕过。ExternalEventsEnabled 为 false 时停止 PollForegroundApplication 的下一次轮询。

ClearPets 先取消 BatchCreationQueue，再清 Session 和虚拟食物窗口；所有 UI 和内部命令继续调用同一方法。

- [ ] **Step 4: 运行已有和新增 smoke**

运行：

    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-smoke.ps1 -Rounds 1 -Role slime
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-input-smoke.ps1

Expected: 清空不会在旧队列完成后重新生成；外部反馈关闭后专用轮询停止；保存状态可区分已应用、已保存和失败。

- [ ] **Step 5: 提交**

    git add src/YimyWalk.App tools/DesktopSmoke tools/DesktopInputSmoke
    git commit -m "feat: expose scoped settings through app host"

### Task 3: 建立普通任务栏 WinForms 导航壳体

**Files:**
- Create: src/YimyWalk.App/UI/UiTheme.cs
- Create: src/YimyWalk.App/UI/ManagerPage.cs
- Create: src/YimyWalk.App/UI/Pages/OverviewPage.cs
- Create: src/YimyWalk.App/UI/Pages/PetsPage.cs
- Create: src/YimyWalk.App/UI/Pages/CharactersPage.cs
- Create: src/YimyWalk.App/UI/Pages/InteractionsPage.cs
- Create: src/YimyWalk.App/UI/Pages/SettingsPage.cs
- Modify: src/YimyWalk.App/UI/ManagerForm.cs
- Modify: src/YimyWalk.App/Host/AppHost.cs
- Test: tools/DesktopInputSmoke/Program.cs

- [ ] **Step 1: 增加页面和生命周期的失败 smoke 断言**

断言 ManagerForm 启动时可见、ShowInTaskbar 为 true、默认标题为总览；导航标签存在；右上角关闭后窗口最小化而不是宿主退出；显式退出仍使 host 结束。

- [ ] **Step 2: 运行 smoke 确认失败**

运行：

    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-input-smoke.ps1

Expected: 当前 ManagerForm 启动不显示总览或没有新页面标签，新增断言失败。

- [ ] **Step 3: 实现固定侧栏和页面缓存**

ManagerForm 使用 AutoScaleMode.Dpi、默认 ClientSize 1120×720、MinimumSize 900×580、200px 固定侧栏和右侧页面宿主。侧栏只负责切换已创建页面，不在按钮事件中直接写设置。

UiTheme 统一提供深色文字、奶油内容区、黄色侧栏、珊瑚选中态、青绿色主操作、8/12/16/24px 间距和标题/分组/正文/辅助字号。状态文字不能只靠颜色表达。

AppHost.Start 在角色校验与业务初始化后显示 ManagerForm 并选择 OverviewPage。页面切换只改变当前页，不重置其他页面选择和滚动状态。

FormClosing 在 !IsExiting 时取消关闭并将 WindowState 设为 Minimized；提供固定的“退出 YimyWalk”按钮调用 ExitApplication。显式退出和系统结束会话设置 IsExiting 后继续执行资源清理。保留 TrayIconHost 兼容资源但不依赖其作为主入口。

- [ ] **Step 4: 运行桌面 smoke 和构建**

运行：

    .tools/dotnet/dotnet.exe build src/YimyWalk.App/YimyWalk.App.csproj --configuration Release -p:Platform=x64
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-input-smoke.ps1

Expected: 编译无警告/错误；真实桌面可见任务栏按钮、页面切换、最小化/恢复和显式退出路径。

- [ ] **Step 5: 提交**

    git add src/YimyWalk.App tools/DesktopInputSmoke
    git commit -m "feat: add taskbar manager navigation shell"

### Task 4: 实现桌宠实例页、稳定选择和单帧预览

**Files:**
- Create: src/YimyWalk.App/UI/PetListProjection.cs
- Create: src/YimyWalk.App/UI/InstanceSettingState.cs
- Create: src/YimyWalk.App/UI/Controls/CharacterFramePreview.cs
- Modify: src/YimyWalk.App/UI/Pages/PetsPage.cs
- Modify: src/YimyWalk.App/Host/AppHost.cs
- Modify: src/YimyWalk.App/Rendering/FrameSurfaceCache.cs
- Test: tools/DesktopInputSmoke/Program.cs
- Test: tests/YimyWalk.Core.Tests/PetBehaviorOverrideTests.cs

- [ ] **Step 1: 增加实例选择/覆盖的失败断言**

断言创建两只实例后选择由 InstanceId 维护；删除当前项后选择下一只或空状态；清空后详情操作禁用；全局默认变化只更新 Inherit 项；强制值保持；恢复跟随清除当前覆盖。

- [ ] **Step 2: 实现增量 PetListProjection**

PetListProjection 以 Guid 为主键保存行模型和 selectedInstanceId。Session InstanceAdded/Removed 只更新受影响行；低频状态摘要使用 100–250ms 节流，不在 Tick 中逐帧重建控件。刷新时 BeginUpdate/EndUpdate 只包围必要行变化，恢复选中 ID 和滚动索引，不依赖行号。

- [ ] **Step 3: 实现桌宠详情和三态控件**

PetsPage 显示 56:44 的列表/详情布局、空状态、错误状态和运行中状态。WalkOverride/DialogueOverride 使用明确的 Inherit/Enabled/Disabled 控件；ActivityRateOverride 使用 Inherit 或自定义 0.1–4.0 数值。每个控件同时显示来源和生效值，显示“单只设置仅在本次运行中有效，重启后恢复默认。”

“关闭主动走动”只写 WalkOverride，不调用 Pause、Hide 或影响 Drag/Fall。删除、喂食、清空和创建调用 AppHost 统一入口。

- [ ] **Step 4: 实现 idle1 单帧预览**

CharacterFramePreview 接收 CharacterDefinition 和索引 0 的 CharacterFrame，使用 FrameSurfaceCache.Get(definition, frame, false, scale)；选中改变时释放旧定义引用，控件 Dispose 时释放当前引用。不得重新解码 PNG，不得把整张 sprite.png 缩小显示。桌宠页使用实例当前 Definition，角色刷新失败时保留旧实例 Definition。

- [ ] **Step 5: 运行逻辑和桌面验证**

运行：

    .tools/dotnet/dotnet.exe test YimyWalk.sln --configuration Release --no-restore
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-input-smoke.ps1
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-smoke.ps1 -Rounds 1 -Role slime

Expected: 覆盖测试通过；真实桌面可选中实例并看到 idle1，刷新失败不破坏旧实例，100 实例创建/清空后列表和资源回落。

- [ ] **Step 6: 提交**

    git add src/YimyWalk.App tests/YimyWalk.Core.Tests tools/DesktopInputSmoke
    git commit -m "feat: add pet instance settings and idle preview"

### Task 5: 实现总览、角色、互动和设置页面

**Files:**
- Modify: src/YimyWalk.App/UI/Pages/OverviewPage.cs
- Modify: src/YimyWalk.App/UI/Pages/CharactersPage.cs
- Modify: src/YimyWalk.App/UI/Pages/InteractionsPage.cs
- Modify: src/YimyWalk.App/UI/Pages/SettingsPage.cs
- Modify: src/YimyWalk.App/UI/ManagerForm.cs
- Modify: src/YimyWalk.App/Host/AppHost.cs
- Test: tools/DesktopInputSmoke/Program.cs

- [ ] **Step 1: 增加页面真实绑定的失败断言**

断言总览分别显示 ActiveCount 和 Batch.PendingCount；角色页只有点击“设为下次创建角色”才改变 SelectedRoleId（下次创建默认角色），浏览/预览不会改变当前运行实例选择；互动页的默认气泡、全局气泡 gate、应用切换反馈、默认走动和活跃度分别显示来源/限制语义；设置页显示保存状态。

- [ ] **Step 2: 实现总览**

总览使用状态摘要卡和快捷操作：创建、暂停/继续、隐藏/恢复、清空、退出。创建按钮只使用 AppHost 当前 SelectedRoleId；查看桌宠实例不能隐式修改 SelectedRoleId。底部状态明确写“全局设置保存状态”。

- [ ] **Step 3: 实现角色页**

角色页列出有效 CharacterCatalog 定义和 LastFailures。选择角色必须通过明确的“设为下次创建角色”动作保存；角色预览使用当前目录有效定义的索引 0。刷新成功后更新角色列表；失败时保留旧定义、旧选择、旧滚动和旧实例预览。

- [ ] **Step 4: 实现互动和设置页**

互动页只显示存在真实业务闭环的控件：默认主动走动、全局活跃度倍率、默认自动气泡、全局允许自动气泡、应用切换反馈、饥饿增长及已有虚拟食物入口。没有新增独立静音/勿扰字段就不显示静音/勿扰控件。

设置页绑定 Scale、WindowLandingEnabled、PreferredDisplayId 和保存/恢复诊断。`WarningLimit` 没有真实运行时行为时只保留在校验文档，不渲染为无效控件。连续数值使用 SettingsSaveCoordinator 合并写入。每个修改显示已应用、保存中、已保存或保存失败。

- [ ] **Step 5: 运行 UI smoke**

运行：

    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-input-smoke.ps1

Expected: 五页可切换；长角色名、中文文案、最小窗口和系统缩放下主要按钮可访问；全局 gate、默认值、局部覆盖和应用切换轮询语义可见且真实生效。

- [ ] **Step 6: 提交**

    git add src/YimyWalk.App tools/DesktopInputSmoke
    git commit -m "feat: bind overview role interaction and settings pages"

### Task 6: 完成布局、键盘焦点、高 DPI 和资源生命周期回归

**Files:**
- Modify: src/YimyWalk.App/UI/UiTheme.cs
- Modify: src/YimyWalk.App/UI/ManagerForm.cs
- Modify: src/YimyWalk.App/UI/Pages/*.cs
- Modify: src/YimyWalk.App/UI/Controls/CharacterFramePreview.cs
- Modify: tools/DesktopInputSmoke/Program.cs
- Modify: scripts/desktop-input-smoke.ps1

- [ ] **Step 1: 增加布局和焦点 smoke 断言**

断言 1120×720 默认窗口、900×580 最小窗口、侧栏约 200px；Tab 键能到达侧栏、主要操作、三态控件和退出；长角色名不会使主操作越界；最小化/恢复后页面、选择和滚动位置保持。

- [ ] **Step 2: 实现布局保护**

使用 TableLayoutPanel/Dock/Anchor 和独立滚动区；详情区域在没有选择时显示空状态；保存失败和角色刷新失败显示可读文本；禁用操作设置 Enabled=false 并保留原因；DPI 变化后重新布局而不改变 Core 坐标。

- [ ] **Step 3: 运行双重构建与桌面 smoke**

运行：

    .tools/dotnet/dotnet.exe test YimyWalk.sln --configuration Release --no-restore
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-yimywalk.ps1 -Version 0.1.6
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-input-smoke.ps1
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-smoke.ps1 -Rounds 1 -Role slime

Expected: Core/构建/smoke 的失败数为 0；输出分别记录代码、构建、逻辑和 Windows 桌面状态，不把截图或 Core 测试当作任务栏焦点/最小化恢复证据。

- [ ] **Step 4: 提交**

    git add src/YimyWalk.App tools/DesktopInputSmoke scripts
    git commit -m "test: harden taskbar UI layout and lifecycle"

### Task 7: 更新状态交接并保留未验证边界

**Files:**
- Modify: docs/STATUS.md
- Modify: docs/TESTING.md only if new commands or UI smoke semantics need documenting

- [ ] **Step 1: 汇总新鲜证据**

记录完整 Core 测试、App build/publish、desktop-input-smoke、desktop-smoke 的实际命令和关键输出。分别列出代码已实现、构建通过、逻辑测试通过、Windows 桌面已验证和 Windows 桌面未验证。

- [ ] **Step 2: 明确未验证项**

至少保留负坐标显示器、物理拔插、睡眠唤醒、长期资源曲线、权限受限外部事件、任务栏布局变化和托盘最终删除决策为未验证或后续项，不能用 UI 线框、Core 测试或截图替代。

- [ ] **Step 3: 运行最终静态检查并提交**

运行：

    git diff --check
    .tools/dotnet/dotnet.exe test YimyWalk.sln --configuration Release --no-restore

Expected: diff check 无内容错误，测试退出码为 0；随后提交文档和交接记录。

    git add docs/STATUS.md docs/TESTING.md
    git commit -m "docs: record taskbar UI shell verification"

## 计划自检

- 五页、WinForms 壳体、任务栏入口、单帧 idle1、全局/实例分层分别在 Tasks 3–5 覆盖。
- 默认值和运行级限制在 Task 1–2 分离，DialogueEnabled 不会被实例强制开启绕过。
- WalkOverride 有 DefaultWalkEnabled 继承来源；ActivityRate 明确是自主决策倍率，角色参数作为基础值。
- 布尔覆盖是 Inherit/Enabled/Disabled；数值覆盖是 Inherit/自定义数值。
- 全局保存失败、实例临时覆盖、清空队列顺序、稳定 InstanceId 选择和 100 实例增量刷新均有任务和验证。
- 未新增真实勿扰、静音、快捷方式或通知中心，避免没有业务闭环的假控件。
