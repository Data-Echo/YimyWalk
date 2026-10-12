# YimyWalk

YimyWalk 是一个面向 Windows x64 的离线桌面 2D 宠物引擎。它采用数据驱动的角色格式：把一张符合约定的 3×3 透明 PNG 放进角色目录，就可以让桌宠发呆、行走、跳跃、坠落、攀爬、被拖拽和互动，不需要修改程序代码。

项目由个人业余开发，是一个非盈利、为爱发电的产品。欢迎下载、体验、学习、修改和提出新的想法。

## 主要功能

- 内置 `slime` 角色，发布包默认只带史莱姆；角色资源与引擎逻辑分离。
- 支持 Idle、Walk、Jump、Fall、Dragged 等基础状态，以及随机蓄力的主动跳跃。
- 支持跳到可见窗口顶部、屏幕边缘攀爬、顶部行走和普通下落。
- 支持拖拽、抛掷、窗口顶部落脚、饥饿、虚拟食物、对白气泡和可选的前台应用反馈。
- 支持多个独立桌宠实例、全局设置、单只桌宠覆盖、DPI 和多显示器适配。
- 管理界面提供角色预览、实例管理、互动参数、缩放和运行状态查看。
- 默认离线运行，不需要账号、API Key、联网服务或管理员权限。

## 下载与运行

优先从 GitHub 仓库的 [Releases](https://github.com/Data-Echo/YimyWalk/releases) 下载 Windows x64 便携包。发布包是 self-contained 单文件应用，通常不需要另行安装 .NET 运行时。如果 Releases 暂时还没有对应版本，可以按下方步骤从源码构建。

解压后双击 `YimyWalk.exe` 即可启动。角色目录应与程序放在同一发布目录中：

```text
YimyWalk.exe
Characters/
  slime/
    sprite.png
```

程序不会自动联网、注册开机启动、申请管理员权限，也不会移动、删除、改名或修改真实桌面文件。

## 添加自己的角色

角色的最小格式只有一张 PNG：

```text
Characters/<role-id>/sprite.png
```

图片必须满足以下条件：

- PNG，带 Alpha 透明通道；
- 宽度和高度都能被 3 整除；
- 按 3 列 × 3 行排列九个等大的格子；
- 单边不超过 2048 像素，总像素不超过 16,777,216；
- 不允许损坏、全透明或没有 Alpha 的图片。

九格的索引和用途如下：

| 索引 | 内容 | 说明 |
| ---: | --- | --- |
| 0–2 | 发呆 | 按配置循环播放 |
| 3–5 | 朝右行走 | 朝左时由程序水平翻转 |
| 6 | 起跳 | 上升阶段使用 |
| 7 | 滞空 / 最高点 | 蓄力和最高点附近使用 |
| 8 | 下落 | 下降阶段使用 |

可选的 `character.json` 和 `dialogue.txt` 用于配置速度、重力、锚点和对白。完整格式见 [`docs/CHARACTER_FORMAT.md`](docs/CHARACTER_FORMAT.md)，模板见 [`examples/character-template`](examples/character-template)。

校验角色包：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/validate-character.ps1 `
  -Path .\Characters\slime `
  -PreviewPath .\artifacts\slime-preview.png
```

## 从源码构建

开发环境需要 Windows x64 和 .NET 8 SDK。仓库提供可复现的 PowerShell 入口：

缺少 SDK 时，可显式运行 `scripts/bootstrap-dotnet.ps1` 在 `.tools/dotnet/` 安装本地开发 SDK。eSheep 仅作为外部审计参考，YimyWalk 的构建和测试不依赖它；来源及固定提交见 [`docs/UPSTREAM_AUDIT.md`](docs/UPSTREAM_AUDIT.md)。本地 SDK、构建产物和临时文件均不提交到仓库。

```powershell
# 恢复依赖、运行测试并生成 Windows x64 self-contained 发布包
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-yimywalk.ps1 `
  -Version 0.1.13 `
  -PublishPath artifacts/publish/yimywalk-win-x64

# 检查发布包内容、角色清单和 SHA-256 校验清单
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-release-package.ps1 `
  -Path artifacts/publish/yimywalk-win-x64 `
  -Version 0.1.13
```

只运行 Core 测试：

```powershell
.tools/dotnet/dotnet.exe test YimyWalk.sln -c Release --no-restore
```

发布包还会包含 `PACKAGE_MANIFEST.json`、`PACKAGE_CHECKSUMS.txt`、第三方声明和许可证文件。依赖与上游代码说明见 [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md)。

## 贡献代码和想法

欢迎提交：

- [Issue](https://github.com/Data-Echo/YimyWalk/issues)：报告问题、提出功能建议或分享桌宠玩法；
- [Pull Request](https://github.com/Data-Echo/YimyWalk/pulls)：修复问题、改进文档、增加测试或贡献功能；
- 角色、动画、UI 和行为方面的新想法。

提交 Issue 时，尽量附上 Windows 版本、DPI/显示器布局、复现步骤、相关日志和是否使用发布包。提交 PR 前请运行 Core 测试和与改动相关的 smoke 检查，并在描述中说明未验证的桌面行为。

项目遵循“先验证再宣称”的原则。核心测试、模拟测试和截图不能替代真实 Windows 桌面验收；物理拔插显示器、睡眠/唤醒、负坐标显示器、长期资源趋势等项目需要单独记录。

## 使用与分发边界

YimyWalk 是非盈利项目，允许个人免费使用、下载、学习、修改、fork 和进行非商业二次开发。欢迎把改进提交回本仓库，也欢迎基于项目提出新的实现思路。

未经作者明确书面许可，禁止：

- 出售 YimyWalk 本体、修改版或重新打包版；
- 提供收费下载、付费解锁或以 YimyWalk 为主要内容的收费分发；
- 将本项目或其发布包捆绑进商业产品后收费提供；
- 删除项目署名、许可证和第三方声明后再发布。

本项目当前采用项目专用的非商业使用说明，**不是** MIT、Apache-2.0 等允许商业再分发的标准开源许可证。第三方依赖、代码和素材的授权以各自文件及 [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md) 为准；它们不因 YimyWalk 的项目条款而改变。

## 当前边界

- 目标平台是 Windows x64；其他平台没有发行承诺。
- 角色目录不会执行脚本、DLL、Shell 命令或远程资源。
- 当前发布包默认只包含 `slime`；测试所需的其他角色 fixture 不代表会随正式包发布。
- 真实桌面上的主动跳跃、侧边攀爬、显示器物理拔插、睡眠恢复和所有 DPI 组合仍需按 [`docs/STATUS.md`](docs/STATUS.md) 中的验证记录理解。

## 文档

- [`docs/PROJECT_SPEC.md`](docs/PROJECT_SPEC.md)：完整产品规格和工程边界
- [`docs/CHARACTER_FORMAT.md`](docs/CHARACTER_FORMAT.md)：角色制作与校验格式
- [`docs/TESTING.md`](docs/TESTING.md)：自动测试和 Windows 桌面验收方法
- [`docs/STATUS.md`](docs/STATUS.md)：当前实现、验证证据和未验证项
- [`RELEASE_NOTES.md`](RELEASE_NOTES.md)：版本变更记录
- [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md)：第三方代码、依赖和素材声明

如果你喜欢这个项目，欢迎试用、反馈和贡献代码。YimyWalk 仍在持续开发中，任何关于角色、行为和桌面互动的想法都值得讨论。
