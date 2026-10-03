# YimyWalk 状态交接

更新时间：2026-10-04

## 当前里程碑：M0 可信基线（已完成，含上游构建阻塞）

已完成：

- 检查确认当前目录原先只有启动文档，不在外层 Git 仓库内；初始化本地 Git，分支为 `codex/bootstrap`。
- 保存完整需求到 `docs/PROJECT_SPEC.md`。
- 获取并固定 eSheep 上游快照 `e2933305157a34818f47d238c74607593d81816d` 到 `vendor/esheep/`，配置 `upstream` 只读远程。
- 核对上游 README 的 MIT 声明、工程入口、依赖和关键窗口/动画/桌面代码路径。
- 通过工作区脚本安装 .NET SDK 8.0.425 到 `.tools/dotnet/`，未修改系统 SDK 或安全设置。
- 写入 AGENTS、README、架构、角色格式、测试和第三方通知材料。

验证和阻塞：

- `git fetch upstream`：通过，远程 master 可用。
- `git rev-parse upstream/master`：得到固定提交 `e2933305157a34818f47d238c74607593d81816d`。
- 上游 `DesktopPet_Portable.csproj` 初次 MSBuild：错误 `MSB3644`，缺少 .NET Framework 4.7.2 引用程序集；随后已安装 4.8.1 Developer Pack、NuGet 7.9，并恢复精确 4.7.2 引用程序集到工作区。
- 上游在引用程序集齐备后继续失败于未使用的 `stdole` COMReference（MSB4181/MSB3283，x86/x64 一致）；可用显式 `-SkipUnusedStdoleComReference` 做不修改上游文件的隔离构建验证。
- 使用隔离项目副本构建后，x64 和 x86 均生成 `DesktopPet.exe`；两种架构的运动、显示器拓扑和 WinForms 运行时测试均通过。x64 运行时记录 39 次窗口位置变化，x86 记录 37 次；两者都通过拖拽、窗口跟随、窗口落脚、镜像缓存和并发释放检查。
- Windows GUI、透明命中、托盘、DPI、多显示器和 1/10/50/100 实例性能：未执行。

## 下一条可执行任务

先由用户审阅 `docs/ARCHITECTURE.md` 和 `docs/CHARACTER_FORMAT.md` 的 M1 设计，再创建核心模拟与角色加载项目；不把上游 XML 直接当作 YimyWalk 角色格式。
