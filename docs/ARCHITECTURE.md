# YimyWalk 架构决策（M0/M1/M2）

## 当前决策

1. 先建立可独立测试的核心模拟，再接入 Windows 窗口；核心不引用 `System.Windows.Forms`。
2. 第一版采用 .NET 8 Windows 桌面应用作为新代码基线，Windows API 集中在 `DesktopIntegration`/`AppHost`；这样避开上游 .NET Framework 目标包缺失，同时保留 Win32 能力。
3. 角色定义不可变且共享图片/台词；`PetInstance` 只保存位置、速度、朝向、状态、动画计时和需求数值。
4. 基础角色包只要求 `sprite.png`，固定 3×3 格和一致锚点；`character.json`、`dialogue.txt`、`actions.png` 都是可选扩展。
5. M1 只交付屏幕工作区地面和基础拖拽/抛掷；M2 通过显式管理器开关提供可选窗口顶部落脚，多显示器 DPI 和外部应用事件仍分别验收。
6. 默认所有“偷吃图标”都是虚拟分身动画。真实快捷方式暂存属于隔离的后续阶段，不能进入基础路径。
7. M1 采用“一个宠物实例对应一个透明分层窗口”。所有窗口由同一个 `YimyWalk.App` 宿主进程管理；没有为每只宠物启动进程，也没有预先改成全屏覆盖层。
8. `CharacterLoader` 只负责 PNG 解码、校验、RGBA 帧和帧元数据；`YimyWalk.Core` 不创建窗口、不调用 Win32、不持有 `Bitmap`。`FrameSurfaceCache` 和 `LayeredWindowRenderer` 在 App 层把共享 RGBA 转成平台绘制资源。

## 逻辑边界

```text
CharacterLoader -> CharacterDefinition (共享只读资源)
                       |
PetEngine --------> PetInstance[] ----> PetRenderer
                       |                     |
InteractionSystem ----+                     +--> DesktopIntegration
AppHost/Settings ----------------------------+       (Windows only)
```

实例、角色定义和绘制资源的边界如下：同一角色的实例共享不可变 `CharacterDefinition` 和 RGBA 帧；位置、速度、状态、动画计时和拖拽数据只属于 `PetInstance`。定义替换通过 `DefinitionLease` 保持实例引用和在途渲染引用，最后一个引用释放后才释放旧资源。透明背景命中测试和实体命中测试分别在 `PetWindow.WndProc` 中处理，焦点控制使用 `WS_EX_NOACTIVATE` 和 `SWP_NOACTIVATE`；这些行为仍需 Windows 桌面矩阵逐项验证。

时钟、随机源、工作区表面和输入由接口注入；测试使用假的实现，Windows 适配层只负责窗口、显示器、托盘和系统事件。所有推进使用单调时间，`delta` 限制在安全上限，渲染频率不决定物理结果。

## 资源生命周期

角色扫描产生新的不可变定义；只有校验成功的定义才替换目录索引。现存实例持有旧定义的安全引用，等实例释放后旧图片才可回收。坏包刷新时保留上一份可用定义并记录错误。相同角色的 100 个实例不得重复解码同一图片。

## 当前验证边界

M1 已选择并实现上游已验证的“一个宠物实例对应一个透明分层窗口”方案；没有为每只宠物启动进程，也没有预先改成全屏覆盖层。批量 smoke 已在真实 Windows 桌面创建和清空 1、10、50、100 个窗口。透明区域穿透、实体命中、拖拽、焦点、托盘退出、长期资源趋势和多显示器/DPI 仍需在没有安全桌面遮罩的可见会话中逐项验收；未完成前不把普通窗口或核心模拟结果记为桌面通过。
## M2 settings boundary

`YimyWalk.Core.Settings.SettingsDocument` owns only validated, immutable data: schema version, selected role, scale, activity rate, dialogue flag, window-landing preference, preferred display identifier, and the optional warning limit. `YimyWalk.App.Settings.SettingsStore` owns the user-local JSON file, temporary file, atomic replacement, backup, and recovery diagnostics. It never places a `Form`, HWND, `Bitmap`, decoded PNG, or startup registration field in Core. AppHost loads this document before role selection, ManagerForm persists explicit selection and landing changes, and clean host disposal saves it. The preferred display value is retained until a real multi-display run is available.

## M2 window-surface boundary

`YimyWalk.Core.Display.SurfaceSelector` receives immutable rectangles and metadata only. It rejects hidden, minimized, stale, excluded, invalid, and non-overlapping candidates, then selects the nearest top edge crossed while a pet is descending. If no candidate qualifies, `IWorldSurface` returns the display working-area floor. Core has no HWND, WinForms, `Bitmap`, `EnumWindows`, or desktop shell knowledge.

`YimyWalk.App.Host.WindowSurfaceResolver` owns the low-frequency `EnumWindows` snapshot and the platform filters for desktop shell, DWM/input, UWP child, and tiny helper windows. `DesktopWorldSurface` owns the fallback display snapshot and enables landing only when the host explicitly opts in. The existing M1 mapping remains one pet instance to one layered window, with every window managed by one `AppHost` process. Closing or losing a supporting window therefore changes only the affected pet's surface selection; it never moves or closes the user's window.

The manager's “允许宠物落到可见窗口顶部” checkbox is the user entry point. It is off by default, persisted in validated settings, and can be changed without restarting the host. Closing the manager hides it and cancels the close so the tray can reopen the same subscribed form; host exit disposes it.

M3.1 adds `Hunger`, `Boredom`, and the short `Eat` state to each `PetInstance`. `CharacterDefaults.HungerIncreasePerMinute` is immutable role data; the mutable values stay on the instance. `PetEngine` applies bounded need deltas with a session-level enable/rate setting, and `FeedAll` is exposed by the manager without adding any desktop API to Core. Feeding is rejected while dragging, jumping, falling, or already eating, so movement/input priority remains explicit.
