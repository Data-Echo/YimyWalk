# eSheep 上游审计

## 固定基线

| 项目 | 结果 |
|---|---|
| 地址 | <https://github.com/Adrianotiger/desktopPet> |
| 分支/提交 | `master` / `e2933305157a34818f47d238c74607593d81816d` |
| 获取日期 | 2026-10-04 |
| 许可 | 上游 `Readme.md:150-151` 声明 MIT；素材仍需逐项审计 |
| 构建入口 | `src/DesktopPet_Portable.sln`、`src/DesktopPet_Portable.csproj` |
| 目标框架 | .NET Framework 4.7.2（Portable）；主工程为 .NET Framework 4.8 |
| 依赖 | `src/packages.config` 中的 JSON、NAudio 和 System.* NuGet 包；部分 DLL 随上游仓库存在 |
| 当前仓库状态 | 不保留上游快照、素材、DLL 或构建产物；需要复核时在仓库外临时获取该提交 |

## 实际构建结果

历史审计先尝试直接构建，MSBuild 报 `MSB3644`，因为机器没有 `.NETFramework,Version=v4.7.2` 引用程序集。补齐引用程序集和 NuGet 包后，原版项目又在 `stdole` COMReference 处报 `MSB4181/MSB3283`，x86/x64 一致。源码检索未发现 `stdole` 的实际使用，因此审计脚本曾在临时项目副本中移除该引用进行环境适配验证，不修改上游仓库。

隔离适配后，x64 和 x86 均编译成功，并运行 `MotionTests`、`DisplayTopologyTests` 和 `RuntimeMotionTests`；测试使用当时桌面会话中的显示器 0、scale 1，未覆盖 1/10/50/100 实例性能或全部 DPI/多屏组合。该结果属于历史审计记录，不表示 YimyWalk 当前仓库含有或依赖上游源码。

## 代码路径审计

以下路径均指向上游提交中的文件，当前 YimyWalk 仓库不包含这些路径：

- **宠物窗口/透明交互**：`src/dotNet/FormPet.cs:20` 定义 WinForms 宠物窗口；`FormPet.cs:1094-1233` 处理按下、拖拽、抛掷和释放；`FormPet.cs:1312-1452` 集中声明 Win32 窗口 API。上游使用逐宠物窗体，透明和命中区域需要在 Windows 桌面逐项验证。
- **渲染/动画**：`src/dotNet/FormPet.cs:387-505` 由 WinForms Timer 驱动动画；`src/dotNet/Animations.cs:454-524` 使用字典保存动画、生成、子动画和音效；`src/dotNet/FormPet.Motion.cs:19` 对镜像精灵做按实例缓存。
- **计时/清理**：`src/dotNet/StartUp.cs:47,136-168,293-347` 使用全局 Timer，并在退出时释放宠物和托盘；`FormPet.Motion.cs:130-139` 释放镜像缓存。需要在多实例删除和异常关闭场景继续验证。
- **桌面与多显示器**：`FormPet.cs:129,249,276,1215` 读取 `Screen.AllScreens`；`FormPet.cs:870-1083` 枚举窗口、读取窗口矩形并尝试窗口顶部落脚。负坐标、DPI、显示器断开和遮挡处理需用现行 Windows 验收矩阵复测。
- **配置/资源加载**：`src/LocalData/AnimationXML.cs:402-437` 用 `XmlSerializer` 解析 XML；`src/dotNet/Xml.cs` 和 `Animations.cs` 将表达式、帧序列和行为转换为运行时对象。该 XML 模型与 YimyWalk 的 3×3 PNG 格式不同，不能直接当作角色格式。
- **联网/自启动边界**：`src/LocalData/Class1.cs:45-54,421-433` 含 GitHub raw 下载地址；设置/安装相关代码存在单独入口。YimyWalk 默认不启用这些上游联网下载和自启动路径。

## 复用判断

YimyWalk 已采用自己的角色加载、数据模型、实例调度和生命周期分层，只保留审计结论与代码路径记录。桌面窗口 API 经验和运动边界测试思路可在后续开发中参考，但不通过复制上游源码、素材或二进制实现复用。
