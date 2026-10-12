# YimyWalk 工程约束

先阅读 [`docs/PROJECT_SPEC.md`](docs/PROJECT_SPEC.md) 和 [`docs/STATUS.md`](docs/STATUS.md)，再查看 Git 状态。需求按 M0 到 M4 分阶段交付；未验证的桌面行为必须明确标记，不能用文档或模拟测试冒充通过。

## 当前工作区

- eSheep 只作为外部审计来源，不再把完整上游快照提交到本仓库；审计摘要见 `docs/UPSTREAM_AUDIT.md`。
- `.tools/dotnet/` 是本地开发 SDK，不提交二进制；缺失时运行 `scripts/bootstrap-dotnet.ps1`。
- `docs/` 保存规格、审计、架构、角色格式、测试和状态交接文档。
- `scripts/` 保存可复现的环境检查和构建入口。

## 命令

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-environment.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-yimywalk.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-yimywalk.ps1
```

当前目标代码使用本地 .NET 8 SDK，Windows API 适配必须与可测试的核心逻辑分层。需要重新审计上游时，在仓库外的临时目录获取指定提交，不要把上游完整源码、素材或二进制复制回仓库。

不要提交密钥、真实桌面路径、用户文件内容或未经审计的第三方素材。不要默认启用联网下载、自启动、真实快捷方式移动或管理员权限。
