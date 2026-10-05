# 测试与验收入口

## 自动验证

工作区使用 `.tools/dotnet/dotnet.exe`，不会依赖系统 PATH 中的 SDK。完整自动验证：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-yimywalk.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-yimywalk.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/publish-yimywalk.ps1
```

`build-yimywalk.ps1` 会 restore、运行整个解决方案的核心测试，清理并发布 x64 `net8.0-windows` 应用，同时把 `Characters/`、`THIRD_PARTY_NOTICES.md` 和发布 manifest 复制到发布目录。`test-yimywalk.ps1` 只运行 Release 核心测试，适合快速回归。`publish-yimywalk.ps1` 使用同一套流程把可复制包写到 `artifacts/publish/yimywalk-win-x64`；每次运行都会清理目标目录并生成 `PACKAGE_CHECKSUMS.txt`，两次相同输入应得到相同清单。

发布包是 self-contained x64 单文件应用，直接双击 `YimyWalk.exe` 即可运行，不需要单独安装 .NET Runtime。`run-yimywalk.ps1` 会检查发布目录、`YimyWalk.exe`、`Characters` 目录和至少一个 `sprite.png`；缺失时返回 `PUBLISH_INCOMPLETE` 或 `CHARACTERS_*` 诊断而不启动应用。脚本仍兼容旧的 framework-dependent 开发输出并保留 `RUNTIME_MISSING` 诊断。它不会修改安装目录之外的桌面文件、注册开机启动或请求管理员权限。应用启动后若角色目录全部无效，会显示 `CHARACTERS_INVALID` 并把摘要写入用户本地 `YimyWalk/startup-error.log`。

在可见 Windows 桌面会话中，`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-smoke.ps1` 会使用临时 `tools/DesktopSmoke` 宿主真实创建并清空 1、10、50、100 个窗口，并输出累计处理器秒数 `CPU_S`、工作集、私有字节、托管堆、句柄、USER/GDI 基线；可追加 `-Rounds 3`（上限 20）在同一宿主进程重复多轮以观察趋势，或追加 `-Role slime` 验证另一套仅含 `sprite.png` 的角色。它不验证透明点击、拖拽或焦点行为，这些由输入矩阵单独验证。

`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-input-smoke.ps1` 会在真实 Windows 窗口上执行掉落、行走、跳跃、透明/实体命中、鼠标拖拽、释放抛掷、捕获丢失、焦点和托盘生命周期检查。它会先打印生产窗口自身的 `WM_NCHITTEST` 结果；若当前会话由 `LockScreenBackstopFrame` 等安全桌面窗口截获 `WindowFromPoint`，会输出 `RESULT UNVERIFIED INPUT_ROUTING_BLOCKED`，保留未验证状态而不把环境阻塞当作功能通过。

`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-display-smoke.ps1 -Rounds 3` 要求可见且已解锁的 Windows 桌面至少有两块扩展显示器。它使用生产启动入口、PerMonitorV2、真实分层宠物窗口和真实鼠标，在每块显示器上检查显示器 bounds/工作区/DPI、cat/slime 的生成和落地、行走/跳跃、透明区域穿透、实体命中、拖拽释放、全局缩放，以及两个方向的跨屏拖拽；随后在同一宿主进程中重复创建和清空并记录 CPU、工作集、私有字节、句柄、USER/GDI。`-Rounds` 可设为 1–100，`-InventoryOnly` 只记录当前显示器元数据。该 smoke 不会改变显示器配置、关闭或移动现有用户窗口；物理拔插、睡眠/唤醒、任务栏布局变化和负坐标拓扑必须另行执行，未执行时仍标记为未验证。

当前核心测试覆盖：3×3 切帧和 0—8 协议、左向镜像、默认脚底锚点、sprite-only 默认值、坏 PNG/透明图/越界图、受限 delta、坠落和落地、行走边界、跳跃 apex、拖拽和抛掷、实例状态隔离、1/10/50/100 创建与清空、失败刷新保留旧定义、共享资源引用计数和幂等释放，以及 hunger/boredom 独立数值、配置增长率、人工喂食边界、饥饿开关、台词限流和虚拟食物认领/消费/超时。

## Windows 桌面验收矩阵

桌面验收必须在可见 Windows 桌面会话中进行，不能用普通窗口动画或纯逻辑模拟代替。每次记录 OS、进程架构、DPI、显示器工作区、CPU、内存以及进程的适用 USER/GDI/句柄指标。

| 项目 | 证据要求 |
| --- | --- |
| 角色加载 | 只放 `sprite.png` 的 cat/slime 都能启动，换目录不改代码 |
| 运动 | 屏幕上方掉落、落地、Idle/Walk、Jump、Fall 都在真实桌面可见 |
| 交互 | 透明区域穿透、实体命中、拖拽、释放抛掷分别验证 |
| 焦点 | 点击、拖拽和刷新不激活宠物窗口 |
| 生命周期 | 托盘隐藏/恢复、清空、退出后窗口、计时器、托盘和绘制资源都释放 |
| 批量 | 1、10、50、100 真实创建与一键清空，重复运行观察 CPU、内存和句柄趋势 |
| 多显示器/DPI | 每块屏幕真实生成、落地、渲染尺寸、透明/实体命中、拖拽跨屏和释放；记录每屏 DPI 与工作区 |

100 只是基线档位，不是永久产品上限，也不能在没有测量时承诺流畅。桌面项目若未执行，必须标记为“Windows 桌面未验证”。

## 上游基线

原版 eSheep 测试仍使用：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/run-upstream-tests.ps1 -Platform x64
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/run-upstream-tests.ps1 -Platform x86
```

这些测试属于 M0 上游证据，不会替代 YimyWalk 的桌面验收。

`desktop-smoke.ps1` output also includes `MANAGED` heap and `FINAL` host-Dispose metrics to distinguish active-window cleanup from process allocator retention; these trends are baseline evidence and not a promise of long-term smoothness at 100 instances. The current single-display landing run also verifies the manager-controlled opt-in path indirectly through the host property; the input smoke directly toggles the manager checkbox and verifies close/reopen.

## 双显示器实测记录（2026-10-05）

在 Windows 11 Home 10.0.26200、x64、16 logical CPUs、Intel Core i5-12500H 的可见桌面上，`desktop-display-smoke.ps1 -Rounds 3` 返回 `RESULT PASS CHECKS=83 FAILURES=0`。主屏 `\\.\DISPLAY1` 为 1920×1080、工作区 1920×1020、DPI 120（125%）；副屏 `\\.\DISPLAY2` 位于主屏右侧，为 2560×1440、工作区 2560×1392、DPI 96（100%）。两套 sprite-only 角色均在两块屏幕上真实生成、落地、行走、跳跃和清空；透明像素穿透、实体像素命中、焦点保持、主屏→副屏及副屏→主屏拖拽释放均通过。主屏单格实际渲染为 40×40，副屏为 32×32；global scale=2 时分别为 80×80 和 64×64。

三轮资源记录的清空值为：GDI 41、46、51；USER 均为 15；句柄为 323、324、324；活动实例和窗口均回到 0，渲染 cache 回到 0。`AutoScaleMode.Dpi` 已用于宠物窗口，以避免 WinForms 默认字体自动缩放在跨 DPI 创建/销毁时产生更大的 GDI 增长；剩余 GDI 小幅上升仍作为平台/WinForms 趋势风险保留，不能写成“无泄漏”或长期流畅性承诺。完整日志见 `artifacts/dual-display-final.log`。

## M2 window-top landing

Run `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-smoke.ps1 -Landing` only in an unlocked, visible Windows desktop session. The harness opens a temporary probe window in an unused region, creates one pet at the probe's horizontal span, verifies that the pet's foot reaches the probe top, closes the probe, and verifies re-fall to the display floor. It does not move, close, or alter existing user windows. A Core selection test or ordinary animation cannot replace this desktop check.
