# YimyWalk v0.1.1

## Included

- Offline Windows x64 desktop pet application.
- Data-driven 3×3 PNG roles; `cat` and `slime` are included.
- Dragging, throwing, movement, falling, optional window-top landing, hunger, dialogue bubbles, virtual food, and opt-in foreground feedback.
- Startup diagnostics for missing runtimes, incomplete packages, and invalid role directories.
- Application icon derived from `icon_yimy.png`, normalized to 512×512 and packaged as a multi-size Windows icon.

## Requirements and boundaries

- Windows x64 with .NET 8 `Microsoft.WindowsDesktop.App` runtime.
- No network access, startup registration, administrator privileges, or real desktop-file operations.
- The package is framework-dependent; the .NET runtime is installed separately.
- Negative-coordinate monitors, physical monitor hot-plug, sleep/wake, restricted-permission sessions, and long-duration resource trends remain unverified.
