# YimyWalk M4 稳定化与可选扩展计划

依据 `PROJECT_SPEC.md` 第 18 节和 M3.5 真实交接。M4 先稳定发布材料和验收边界，再考虑轻量社交；真实快捷方式继续隔离设计，不进入默认路径。

## M4.1 角色作者材料与校验入口

| 修改范围 | 依赖 | 验证与完成条件 |
|---|---|---|
| `docs/CHARACTER_FORMAT.md`、`Characters` 示例、`scripts/validate-character.ps1`、README | M3 角色加载器 | cat/slime 仍只需 sprite.png；提供带网格参考图、json/dialogue 示例和明确错误；脚本能报告尺寸、Alpha、帧和配置问题，不改源文件 |

## M4.2 发布包与启动自检

| 修改范围 | 依赖 | 验证与完成条件 |
|---|---|---|
| build/publish 脚本、第三方声明、运行入口 | M4.1 | clean workspace 可重复 restore/build/publish；发布目录含角色和许可证；不启用联网、自启动或管理员权限；启动失败给出可理解诊断 |

## M4.3 稳定性回归

| 修改范围 | 依赖 | 验证与完成条件 |
|---|---|---|
| Core/App 生命周期、smoke、STATUS/TESTING | M4.2 | 重复刷新、清空、退出、气泡/食物/事件资源无残留；CPU/内存/句柄趋势重新记录；所有未验证桌面场景继续明确标注 |

## M4.4 可选轻量社交（单独切片）

| 修改范围 | 依赖 | 验证与完成条件 |
|---|---|---|
| Core 邻近检测和短时互动，App 反馈 | M4.3 | 不共享可变需求状态；100 只时有全局节流；拖拽、坠落、喂食优先级不被破坏；没有繁殖或真实文件操作 |

## M4.5 真实快捷方式沙盒设计（不默认实现）

| 修改范围 | 依赖 | 验证与完成条件 |
|---|---|---|
| 独立设计文档和隔离测试程序 | M4.3、额外用户启用流程 | 先只读、可恢复、无真实桌面副作用；未达到恢复标准前保持虚拟图标模式，不接入主程序 |

每个任务先写失败或边界测试，完成后运行完整 Core/build/可用 Windows smoke，再更新 `docs/STATUS.md`。不自动推送远程，不移动用户桌面文件，不设置开机启动。


### M4.1 逐任务实施计划（2026-10-05）

1. **Author-tool contract and failing tests**
   - 修改范围：新增 `tools/CharacterValidator` net8.0 console project，引用 `YimyWalk.Core`；在 Core 测试工程中加入对工具验证 API 的测试引用。
   - 依赖：现有 `CharacterLoader` 的 PNG、配置和对白限制；不引入 Windows Forms、System.Drawing 或桌面 API。
   - 验证方法：先运行新增测试，确认缺少验证 API 时按预期失败；随后覆盖 sprite-only、有效 JSON/对白、损坏 PNG、非法配置、无效 UTF-8 和带空格/中文路径。
   - 完成条件：验证器返回稳定的成功/失败结果和可读诊断，退出码区分参数错误、角色无效和验证成功。
2. **Preview and reference material**
   - 修改范围：工具支持生成实际角色的 3×3 预览和独立的网格/锚点参考 PNG；新增 `examples/character-template`（含 JSON、对白和说明）与 `docs/assets/character-grid-reference.png`，不改写 `Characters/cat` 或 `Characters/slime`。
   - 依赖：验证器先完成并通过测试；预览从 `CharacterDefinition.Frames` 的 RGBA 快照生成，运行时 sprite 不嵌入参考线。
   - 验证方法：生成两类 PNG 后重新读取尺寸、Alpha 和 SHA-256；对输入角色文件计算前后哈希，确认验证和预览只读输入。
   - 完成条件：预览命令可重复运行、输出目录可控，失败不会留下宣称有效的预览。
3. **Author documentation and wrapper**
   - 修改范围：重写 `docs/CHARACTER_FORMAT.md` 的 supported/planned 边界，新增 `scripts/validate-character.ps1` 和 README 使用说明；明确不支持 `actions.png`、未知 JSON 字段当前会忽略、发布包依赖 .NET 8 Windows Desktop runtime。
   - 依赖：工具命令行和示例目录可运行。
   - 验证方法：在仓库根目录、含空格路径和中文路径分别运行脚本；检查坏角色非零退出，sprite-only 角色零退出。
   - 完成条件：作者无需修改代码即可替换角色目录；文档不把模拟/核心测试写成 Windows 桌面验收。
4. **交接与审查**
   - 修改范围：更新 `docs/STATUS.md`，记录 M4.1 实际结果和仍未完成的 M3/M2 桌面矩阵；请求代码审查并修复 Critical/Important 问题。
   - 依赖：前述测试、构建、脚本验证全部有新鲜输出。
   - 验证方法：`.tools/dotnet/dotnet.exe test YimyWalk.sln -c Release`、`scripts/build-yimywalk.ps1`、`scripts/validate-character.ps1` 和 `git diff --check`。
   - 完成条件：只报告代码/构建/Core/Windows 桌面各自真实状态；M4.2 作为下一条可执行任务。
