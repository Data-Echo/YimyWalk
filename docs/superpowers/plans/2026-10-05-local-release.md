# YimyWalk local release implementation plan

**Goal:** Preserve full development on `codex/bootstrap`, create a minimal buildable `main`, and package local `v0.1.0` source and Windows x64 binaries.

**Approved design:** The user approved the branch/package proposal in this session. `main` keeps product source, character resources, product documentation, licenses and build/run scripts. Development tools, tests, upstream snapshots and engineering records stay on `codex/bootstrap`. No push, remote default-branch change, online Release, system change or desktop-file operation is authorized.

**Architecture:** Keep shared product/build files identical on both branches. Export tracked files using an explicit allowlist and generate a two-project product solution. Preserve Git history. Build and package the exact tagged main source; release archives are ignored artifacts, not Git contents.

**Tech stack:** .NET 8, PowerShell 5.1, Git worktrees, ZIP and SHA-256.

- [ ] Save existing M4.2/M4.3 work on the development branch after Core/build verification.
- [ ] Add version metadata, runtime/license/readme payload and SDK discovery to shared build/publish entry points. Publish must work in a clean source export with no tests/tools/local SDK bundled.
- [ ] Add a development-only explicit export manifest and documented main promotion steps; verify exported files and reject development/build artifacts.
- [ ] Create local `main` in an ignored worktree, replace its tracked tree with the approved product export and run restore/build/publish with an explicit SDK path. Review before committing.
- [ ] Create annotated `v0.1.0` on main. Produce source ZIP from that tag and binary ZIP from a clean extraction; verify per-file checksums, version, payload and repeat publish content.
- [ ] Run the complete development Core suite and available non-dual-display smoke; perform package startup precheck. Preserve earlier desktop evidence and unverified scenarios accurately.
- [ ] Record final commits, artifacts and commands in development STATUS/RELEASING. Keep both branches/worktrees and avoid remote mutations.

Validation: `dotnet test YimyWalk.sln -c Release`, `scripts/build-yimywalk.ps1`, `scripts/publish-yimywalk.ps1`, a clean exported-source build using `-DotnetPath`, source/payload path inventories, SHA-256 comparison after two publishes and after ZIP extraction, `scripts/desktop-smoke.ps1 -Rounds 1 -Role slime`, `git diff --check`. No repeat dual-display test.
