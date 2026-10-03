# YimyWalk

YimyWalk 是面向 Windows 11 x64 的本地桌面 2D 宠物引擎。角色以数据包形式加载：一个角色目录至少包含一张带 Alpha 的 `sprite.png`，按 3×3 格切分为发呆、行走、起跳/滞空/下落帧。

当前仓库处于 M0 基线阶段。完整需求见 [`docs/PROJECT_SPEC.md`](docs/PROJECT_SPEC.md)，真实进度见 [`docs/STATUS.md`](docs/STATUS.md)。上游 eSheep 快照位于 [`vendor/esheep`](vendor/esheep)，审计结论见 [`docs/UPSTREAM_AUDIT.md`](docs/UPSTREAM_AUDIT.md)。

## 开发环境

Windows 11 x64、PowerShell、Git、Visual Studio Desktop MSBuild，以及工作区内 .NET 8 SDK。检查环境：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-environment.ps1
```

若工作区 SDK 尚未安装：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/bootstrap-dotnet.ps1
```

## 上游基线

上游构建入口和限制：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-upstream.ps1
```

该命令不会修改系统或用户桌面。当前机器缺少 .NET Framework 4.7.2 引用程序集，因此原版构建会报告 MSB3644；安装开发者包后可重跑并把结果记录到 `docs/STATUS.md`。

角色添加、构建、测试和 Windows 手工验收约定将在 M1 实现后补充到 [`docs/CHARACTER_FORMAT.md`](docs/CHARACTER_FORMAT.md) 与 [`docs/TESTING.md`](docs/TESTING.md)。
