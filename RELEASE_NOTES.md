# Unreleased

## Per-instance pet size

- The Pets page now lets each running instance follow the global scale or use a custom 0.1–8.0× size.
- Custom instance size is runtime-only. Changing the global scale affects only instances that follow it; clearing an override restores inheritance, and restarting restores the global default.
- Added Core and ManagerUiSmoke regression coverage for mixed-role instances, UI controls, inheritance, and reset behavior.
- Fixed the Pets detail layout so the instance-size source selector and numeric input remain visible after selecting a pet at the normal 125% DPI window size.

# YimyWalk v0.1.5

## Included

- Dragged pets now wrap horizontally across the global display span while airborne. A toss that crosses the right edge re-enters from the left, and the reverse direction behaves the same while preserving flight velocity and vertical motion.
- Surface selection uses the wrapped projected position so a cross-display toss can complete its landing on the destination display floor.
- Core regression tests cover both wrap directions; the targeted Windows smoke validates the behavior against the current two-display topology.

## Requirements and boundaries

- Windows x64. The release executable embeds the .NET 8 runtime.
- No network access, startup registration, administrator privileges, or real desktop-file operations.
- Negative-coordinate monitors, physical monitor hot-plug, sleep/wake, restricted-permission sessions, and long-duration resource trends remain unverified.

# YimyWalk v0.1.4

## Included

- Offline Windows x64 desktop pet application.
- Data-driven 3×3 PNG roles; `cat` and `slime` are included.
- Dragging, throwing, movement, falling, optional window-top landing, hunger, dialogue bubbles, virtual food, and opt-in foreground feedback.
- Startup diagnostics for missing runtimes, incomplete packages, and invalid role directories.
- Application icon derived from `icon_yimy.png`, normalized to 512×512 and packaged as a multi-size Windows icon. The same embedded icon is used by the executable, manager taskbar window, and notification-area tray icon.
- Pets can walk across horizontally arranged displays and wrap from the global virtual-desktop edge to the opposite edge; maximized windows are excluded from window-top landing.
- Release entry is the self-contained single-file `YimyWalk.exe`; double-clicking it starts the app without a separate .NET runtime installation.

## Requirements and boundaries

- Windows x64. The release executable embeds the .NET 8 runtime.
- No network access, startup registration, administrator privileges, or real desktop-file operations.
- The package is self-contained and offline; no separate runtime installation is required.
- Negative-coordinate monitors, physical monitor hot-plug, sleep/wake, restricted-permission sessions, and long-duration resource trends remain unverified.

# YimyWalk v0.1.8

## Included

- AntdUI 2.4.12 unified the WinForms management shell across overview, pets, characters, interactions, and settings.
- Fixed card measurement and page-internal clipping; added content-driven spacing, scrolling, idle1 previews, modern Switch/Select/InputNumber controls, and a default-role plus creation-count overview action.
- Preserved the tray-first lifecycle, stable instance deletion, 200 ms visible manager sampling, global/instance setting semantics, and offline self-contained Windows x64 packaging.

## Verification boundary

- Core tests, ManagerUiSmoke layout assertions, application builds, and the Windows input smoke passed in the current Windows session (DPI 120 / 125%).
- 100%, 150%, 200% DPI switching, minimum-size/maximize matrix, and desktop visual acceptance remain unverified.
