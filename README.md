# YimyWalk

YimyWalk 是一个离线运行的 Windows x64 桌面 2D 宠物引擎。把符合格式的角色目录放入 `Characters/<id>/`，程序即可加载角色，不需要修改代码。

## 使用

1. 安装 .NET 8 `Microsoft.WindowsDesktop.App` x64 runtime。
2. 运行 `Start-YimyWalk.ps1`，或直接运行 `YimyWalk.App.exe`。
3. 角色目录至少需要一张透明的 3×3 `sprite.png`：

```text
Characters/<id>/sprite.png
```

角色格式、可选配置和对白见 [`docs/CHARACTER_FORMAT.md`](docs/CHARACTER_FORMAT.md)。默认包包含 `cat` 和 `slime`。

## 安全边界

- 默认离线运行，不联网、不下载内容、不注册开机启动。
- 不请求管理员权限，不移动、删除、改名或修改真实桌面文件。
- 发布包是 framework-dependent 的 x64 包；运行时由用户单独安装。
- 角色目录只读取图片、配置和对白，不执行脚本、命令、DLL 或远程资源。

## 从源码构建

在包含 .NET 8 SDK 的 Windows 环境中运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-yimywalk.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/publish-yimywalk.ps1
```

发布目录包含启动自检、角色、版本说明、校验清单和许可证。第三方声明见 [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md)。

## 验证边界

当前版本已验证基础单屏桌面行为和双显示器/DPI smoke。负坐标显示器、物理拔插、睡眠唤醒、受限权限会话和长期资源趋势仍需单独验证。
