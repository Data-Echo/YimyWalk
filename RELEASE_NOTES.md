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
