# 测试与验收入口

## 已有基线

上游提供 `vendor/esheep/tests/run-motion-tests.ps1`，覆盖运动插值、拖拽速度、显示器索引恢复、窗口移动、Timer/Hook 清理和交互运行时检查。它依赖 .NET Framework 4.7.2 目标包、NuGet 和可用桌面会话；当前机器的原版编译先因 MSB3644 阻塞，未宣称这些测试通过。

当前工作区用以下入口恢复包、使用隔离的 `stdole` 适配构建并运行同一套测试：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/run-upstream-tests.ps1 -Platform x64
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/run-upstream-tests.ps1 -Platform x86
```

## YimyWalk 自动测试目标

M1 开始后，核心测试必须覆盖 3×3 切图/索引、非法图像、配置默认值、锚点/镜像、状态迁移、跳跃/坠落/落地、拖拽中断、delta 上限、随机源隔离、共享定义、角色刷新失败、命令参数和数量上限。测试使用假的时钟、随机源、桌面表面和输入。

## Windows 手工验收

在 Windows 11 x64 桌面会话中记录 OS、架构、DPI、显示器数量、构建配置和实际实例数。逐项测试透明区域点击穿透、实体拖拽/抛掷、不抢焦点、托盘恢复、负坐标副屏、跨屏拖动、任务栏变化、窗口移动/关闭、休眠恢复、全屏/勿扰、批量清空和退出后无遗留活动。截图只作为画面证据，不能替代输入和资源稳定性验证。

性能档位为 1、10、50、100 只；报告 CPU 口径、内存、句柄、生成/清空释放趋势和测试时长。没有执行的档位标记“未执行”。
