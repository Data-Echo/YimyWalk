# eSheep 上游审计

## 固定基线

| 项目 | 结果 |
|---|---|
| 地址 | <https://github.com/Adrianotiger/desktopPet> |
| 远程 | `upstream`，推送 URL 被设为 `DISABLED` |
| 分支/提交 | `master` / `e2933305157a34818f47d238c74607593d81816d` |
| 获取日期 | 2026-10-04 |
| 许可 | 上游 `Readme.md:150-151` 声明 MIT；素材仍需逐项审计 |
| 构建入口 | `src/DesktopPet_Portable.sln`、`src/DesktopPet_Portable.csproj` |
| 目标框架 | .NET Framework 4.7.2（Portable）；主工程为 .NET Framework 4.8 |
| 依赖 | `src/packages.config` 中的 JSON、NAudio 和 System.* NuGet 包；部分 DLL 随快照存在 |

## 实际构建结果

使用：

```powershell
& 'D:\Program Files\Visual\product\MSBuild\Current\Bin\MSBuild.exe' vendor\esheep\src\DesktopPet_Portable.csproj /nologo /v:minimal /p:Configuration=Release /p:Platform=x64 /p:GenerateManifests=false
```

结果：失败，MSBuild `MSB3644`，当前机器没有 `.NETFramework,Version=v4.7.2` 引用程序集。没有把该失败包装成构建通过；安装对应开发者包后才可继续原版构建和交互测试。机器已有 Visual Studio 18.5.2 Build Tools、桌面 MSBuild 和 .NET 运行时；工作区另安装了 .NET SDK 8.0.425，供 YimyWalk 新代码使用。

安装引用程序集和 NuGet 包后，原版项目继续在 `stdole` COMReference 处失败（MSB4181/MSB3283），x86/x64 一致。源码检索未发现 `stdole` 的实际使用，因此脚本提供 `-SkipUnusedStdoleComReference`，只在同目录临时项目副本中移除该引用，不修改上游快照；这属于构建环境适配验证，不代表上游原版项目文件本身已修复。

使用该隔离选项后，x64 和 x86 都成功编译，并分别运行 `MotionTests`、`DisplayTopologyTests` 和 `RuntimeMotionTests`；测试输出全部为 PASS。测试运行的是现有桌面会话中的显示器 0、scale 1，未覆盖 1/10/50/100 实例性能或所有 DPI/多屏组合。

## 代码路径审计

- **宠物窗口/透明交互**：`src/dotNet/FormPet.cs:20` 定义 WinForms 宠物窗口；`FormPet.cs:1094-1233` 处理按下、拖拽、抛掷和释放；`FormPet.cs:1312-1452` 集中声明 Win32 窗口 API。上游使用逐宠物窗体，透明和命中区域需要在 Windows 桌面逐项验证。
- **渲染/动画**：`src/dotNet/FormPet.cs:387-505` 由 WinForms Timer 驱动动画；`src/dotNet/Animations.cs:454-524` 使用字典保存动画、生成、子动画和音效；`src/dotNet/FormPet.Motion.cs:19` 对镜像精灵做按实例缓存。
- **计时/清理**：`src/dotNet/StartUp.cs:47,136-168,293-347` 使用全局 Timer，并在退出时释放宠物和托盘；`FormPet.Motion.cs:130-139` 释放镜像缓存。需要在多实例删除和异常关闭场景继续验证。
- **桌面与多显示器**：`FormPet.cs:129,249,276,1215` 读取 `Screen.AllScreens`；`FormPet.cs:870-1083` 枚举窗口、读取窗口矩形并尝试窗口顶部落脚。负坐标、DPI、显示器断开和遮挡处理需用现行 Windows 验收矩阵复测。
- **配置/资源加载**：`src/LocalData/AnimationXML.cs:402-437` 用 `XmlSerializer` 解析 XML；`src/dotNet/Xml.cs` 和 `Animations.cs` 将表达式、帧序列和行为转换为运行时对象。该 XML 模型与 YimyWalk 的 3×3 PNG 格式不同，不能直接当作角色格式。
- **联网/自启动边界**：`src/LocalData/Class1.cs:45-54,421-433` 含 GitHub raw 下载地址；设置/安装相关代码存在单独入口。YimyWalk 默认不启用这些上游联网下载和自启动路径。

## 复用判断

保留上游作为参考和后续局部复用候选，优先复用桌面窗口 API 经验和已覆盖运动边界的测试思路。角色加载、数据模型、实例调度和生命周期将使用 YimyWalk 自己的分层接口，避免把 XML、共享定义和 Windows 窗体状态混在一起。M1 之前不修改上游快照。
