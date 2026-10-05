# YimyWalk

YimyWalk 是一个离线运行的 Windows 11 x64 桌面 2D 宠物引擎。M1 使用数据驱动的 3×3 PNG 角色包：`Characters/<id>/sprite.png` 即可加载角色，不需要修改代码。

当前 M1 已实现角色加载、Idle/Walk/Jump/Fall、屏幕工作区地面、拖拽/抛掷、一个实例一个透明分层窗口、同一宿主进程的托盘生命周期，以及 1/10/50/100 分批创建和清空入口。窗口顶部落脚、饥饿、气泡、外部应用事件和真实快捷方式操作留到后续阶段。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-yimywalk.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/run-yimywalk.ps1
```

核心测试和发布构建可在当前工作区运行；真实透明命中、拖拽、焦点、托盘和批量桌面性能矩阵见 [`docs/TESTING.md`](docs/TESTING.md)，当前状态见 [`docs/STATUS.md`](docs/STATUS.md)。

M2 已加入可回滚角色刷新、用户设置恢复、显示器/DPI 抽象和可选窗口顶部落脚。窗口顶部落脚通过 `scripts/desktop-smoke.ps1 -Landing` 在真实 Windows 桌面验证；第二显示器/DPI 矩阵仍按当前环境标记为未验证。

M3.1 已加入每实例 hunger/boredom 数值、可配置增长率、饥饿开关、短暂 Eat 状态和管理器“喂食全部”入口；气泡、虚拟食物和外部应用事件仍按计划分阶段实现。

M3.2 已加入受全局上限和单宠冷却控制的非激活台词气泡；虚拟食物和外部应用事件仍未实现。

M3.3 已加入程序生成的虚拟食物：管理器可生成目标，宠物会认领、接近、消费并进入 Eat，目标视觉随后缩小消失；不会接触真实桌面文件或快捷方式。

M3.4 已加入默认关闭的应用切换反馈开关；开启后只低频读取前台进程标识，实际外部程序事件矩阵仍需单独验证。

M4.1 增加了角色作者校验器和静态预览：它直接复用 `YimyWalk.Core.CharacterLoader`，报告尺寸、Alpha、九帧映射、配置/对白来源和错误原因，并可生成预览图与独立的 3×3 锚点参考图。作者入口为：

首次使用请先运行一次 `scripts/build-yimywalk.ps1`（或用工作区 SDK 还原 `YimyWalk.sln`）；校验脚本随后使用 `--no-restore`，不会在每次检查角色时隐式联网。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/validate-character.ps1 `
  -Path .\Characters\cat `
  -PreviewPath .\artifacts\cat-preview.png
```

角色格式和当前支持边界见 [`docs/CHARACTER_FORMAT.md`](docs/CHARACTER_FORMAT.md)，带配置/对白的模板见 [`examples/character-template`](examples/character-template)。`actions.png` 和脚本型扩展仍未实现。发布输出是依赖 .NET 8 Windows Desktop runtime 的 framework-dependent 包；构建和核心测试通过不等于 Windows 桌面验收完成。
