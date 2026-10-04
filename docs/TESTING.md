# 测试与验收入口

## 自动验证

工作区使用 `.tools/dotnet/dotnet.exe`，不会依赖系统 PATH 中的 SDK。完整自动验证：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-yimywalk.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-yimywalk.ps1
```

`build-yimywalk.ps1` 会 restore、运行整个解决方案的核心测试，并发布 x64 `net8.0-windows` 应用，同时把 `Characters/` 复制到发布目录。`test-yimywalk.ps1` 只运行 Release 核心测试，适合快速回归。

在可见 Windows 桌面会话中，`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-smoke.ps1` 会使用临时 `tools/DesktopSmoke` 宿主真实创建并清空 1、10、50、100 个窗口，并输出工作集、私有字节、句柄、USER/GDI 基线；可追加 `-Rounds 3` 在同一宿主进程重复三轮以观察趋势，或追加 `-Role slime` 验证另一套仅含 `sprite.png` 的角色。它不验证透明点击、拖拽或焦点行为，这些由输入矩阵单独验证。

`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-input-smoke.ps1` 会在真实 Windows 窗口上执行掉落、行走、跳跃、透明/实体命中、鼠标拖拽、释放抛掷、捕获丢失、焦点和托盘生命周期检查。它会先打印生产窗口自身的 `WM_NCHITTEST` 结果；若当前会话由 `LockScreenBackstopFrame` 等安全桌面窗口截获 `WindowFromPoint`，会输出 `RESULT UNVERIFIED INPUT_ROUTING_BLOCKED`，保留未验证状态而不把环境阻塞当作功能通过。

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

100 只是基线档位，不是永久产品上限，也不能在没有测量时承诺流畅。桌面项目若未执行，必须标记为“Windows 桌面未验证”。

## 上游基线

原版 eSheep 测试仍使用：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/run-upstream-tests.ps1 -Platform x64
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/run-upstream-tests.ps1 -Platform x86
```

这些测试属于 M0 上游证据，不会替代 YimyWalk 的桌面验收。

`desktop-smoke.ps1` output also includes `MANAGED` heap and `FINAL` host-Dispose metrics to distinguish active-window cleanup from process allocator retention; these trends are baseline evidence and not a promise of long-term smoothness at 100 instances. The current single-display landing run also verifies the manager-controlled opt-in path indirectly through the host property; the input smoke directly toggles the manager checkbox and verifies close/reopen.

## M2 window-top landing

Run `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-smoke.ps1 -Landing` only in an unlocked, visible Windows desktop session. The harness opens a temporary probe window in an unused region, creates one pet at the probe's horizontal span, verifies that the pet's foot reaches the probe top, closes the probe, and verifies re-fall to the display floor. It does not move, close, or alter existing user windows. A Core selection test or ordinary animation cannot replace this desktop check.
