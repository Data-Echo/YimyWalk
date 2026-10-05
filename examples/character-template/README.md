# 角色作者模板

将自己的 3×3 `sprite.png` 放在本目录，或把整个目录复制到 `Characters/<id>/`。模板中的配置和对白都是可选示例；删除它们后，只有 `sprite.png` 也能运行。

用工作区校验器检查目录：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/validate-character.ps1 `
  -Path .\examples\character-template `
  -PreviewPath .\artifacts\template-preview.png
```

参考线和锚点示意位于 `docs/assets/character-grid-reference.png`。不要把参考线绘进提交给 YimyWalk 的运行时 sprite。
