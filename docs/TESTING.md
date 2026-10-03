# 测试与验收入口

## 自动验证

工作区使用 `.tools/dotnet/dotnet.exe`，不会依赖系统 PATH 中的 SDK。完整自动验证：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-yimywalk.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-yimywalk.ps1
```

`build-yimywalk.ps1` 会 restore、运行整个解决方案的核心测试，并发布 x64 `net8.0-windows` 应用，同时把 `Characters/` 复制到发布目录。`test-yimywalk.ps1` 只运行 Release 核心测试，适合快速回归。\n\n在可见 Windows 桌面会话中，powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-smoke.ps1 会使用临时 	ools/DesktopSmoke 宿主真实创建并清空 1、10、50、100 个窗口，并输出工作集和句柄基线。它不验证透明点击、拖拽或焦点行为，这些仍需手工输入矩阵。

当前核心测试覆盖：3×3 切帧和 0—8 协议、左向镜像、默认脚底锚点、sprite-only 默认值、坏 PNG/透明图/越界图、受限 delta、坠落和落地、行走边界、跳跃 apex、拖拽和抛掷、实例状态隔离、1/10/50/100 创建与清空、失败刷新保留旧定义、共享资源引用计数和幂等释放。

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
