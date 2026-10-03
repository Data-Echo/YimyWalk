# YimyWalk 角色格式（M0 约定）

## 最小包

```text
Characters/<id>/
  sprite.png
```

`id` 使用目录名，限制为单层相对目录名，不允许路径分隔符、`.`、`..` 或控制字符。`sprite.png` 必须是可读取的带 Alpha PNG，宽高都能被 3 整除，切成 9 个相同大小的格。总像素和文件体积上限由加载器集中校验；全透明、损坏或越界输入拒绝而不影响其他角色。

帧索引从左到右、从上到下：0-2 发呆，3-5 向右行走，6 起跳，7 滞空，8 下落。向左由程序绕脚底锚点水平镜像。默认锚点为 `(0.5 * cellWidth, 0.9 * cellHeight)`，逻辑速度单位为像素/秒，物理位置和命中区域使用同一缩放换算。

## 可选配置

`character.json` 的 `schemaVersion` 为 `1`。缺失字段回退内置默认值；非法数字、负尺寸、NaN、无穷大和未知动作引用产生可读错误并保留上一份角色。

```json
{
  "schemaVersion": 1,
  "displayName": "示例小猫",
  "scale": 1.0,
  "anchor": { "x": 0.5, "y": 0.9 },
  "animation": { "idleFps": 3, "walkFps": 8 },
  "movement": { "walkSpeed": 60, "jumpSpeed": 450, "gravity": 1500, "maxFallSpeed": 1500 },
  "behavior": { "idleSecondsMin": 2, "idleSecondsMax": 8, "hungerIncreasePerMinute": 0.01, "speechCooldownSeconds": 45 }
}
```

配置合并顺序为内置默认 → 角色配置 → 用户覆盖；临时实例状态不写回作者文件。`dialogue.txt` 使用 UTF-8 分组格式，缺失分组回退内置中文台词。`actions.png` 只有在配置声明单格尺寸、帧映射、速度、循环和锚点后才加载；高级动作缺失时使用基础帧和程序化效果。
