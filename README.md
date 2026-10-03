# YimyWalk

YimyWalk 是一个离线运行的 Windows 11 x64 桌面 2D 宠物引擎。M1 使用数据驱动的 3×3 PNG 角色包：`Characters/<id>/sprite.png` 即可加载角色，不需要修改代码。

当前 M1 已实现角色加载、Idle/Walk/Jump/Fall、屏幕工作区地面、拖拽/抛掷、一个实例一个透明分层窗口、同一宿主进程的托盘生命周期，以及 1/10/50/100 分批创建和清空入口。窗口顶部落脚、饥饿、气泡、外部应用事件和真实快捷方式操作留到后续阶段。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-yimywalk.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/run-yimywalk.ps1
```

核心测试和发布构建可在当前工作区运行；真实透明命中、拖拽、焦点、托盘和批量桌面性能矩阵见 [`docs/TESTING.md`](docs/TESTING.md)，当前状态见 [`docs/STATUS.md`](docs/STATUS.md)。
