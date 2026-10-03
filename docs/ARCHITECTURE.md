# YimyWalk 架构决策（M0）

## 当前决策

1. 先建立可独立测试的核心模拟，再接入 Windows 窗口；核心不引用 `System.Windows.Forms`。
2. 第一版采用 .NET 8 Windows 桌面应用作为新代码基线，Windows API 集中在 `DesktopIntegration`/`AppHost`；这样避开上游 .NET Framework 目标包缺失，同时保留 Win32 能力。
3. 角色定义不可变且共享图片/台词；`PetInstance` 只保存位置、速度、朝向、状态、动画计时和需求数值。
4. 基础角色包只要求 `sprite.png`，固定 3×3 格和一致锚点；`character.json`、`dialogue.txt`、`actions.png` 都是可选扩展。
5. M1 只交付屏幕工作区地面和基础拖拽/抛掷；窗口顶部落脚、多显示器 DPI 和外部应用事件在有真实适配层后验收。
6. 默认所有“偷吃图标”都是虚拟分身动画。真实快捷方式暂存属于隔离的后续阶段，不能进入基础路径。

## 逻辑边界

```text
CharacterLoader -> CharacterDefinition (共享只读资源)
                       |
PetEngine --------> PetInstance[] ----> PetRenderer
                       |                     |
InteractionSystem ----+                     +--> DesktopIntegration
AppHost/Settings ----------------------------+       (Windows only)
```

时钟、随机源、工作区表面和输入由接口注入；测试使用假的实现，Windows 适配层只负责窗口、显示器、托盘和系统事件。所有推进使用单调时间，`delta` 限制在安全上限，渲染频率不决定物理结果。

## 资源生命周期

角色扫描产生新的不可变定义；只有校验成功的定义才替换目录索引。现存实例持有旧定义的安全引用，等实例释放后旧图片才可回收。坏包刷新时保留上一份可用定义并记录错误。相同角色的 100 个实例不得重复解码同一图片。

## 暂不决定

上游的逐宠物 WinForms 窗口、单一覆盖层或其他渲染方式将在 M1 的透明命中、焦点、DPI 和实例基线后选择；现在不以未经测试的架构假设替代证据。
