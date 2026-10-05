# Cross-display walking and maximized-window landing design

## Goal

Make walking pets cross the virtual desktop horizontally, wrap from the global left edge to the global right edge (and vice versa), and prevent maximized application windows from becoming landing surfaces while preserving ordinary window-top landing.

## Chosen behavior

- A walking pet uses one horizontal travel span derived from every valid display working area. The span is `[minimum WorkingArea.Left, maximum WorkingArea.Right]`, so a pet can leave one display, pass through any coordinate gap, and enter another display.
- Crossing the global span wraps the pet to the opposite global edge. The wrap keeps the current walking direction and carries the floor selection from the display resolved at the new coordinate.
- The pet's vertical floor remains display-specific. While the pet is on a display, it uses that display's working-area bottom. If two displays have different vertical positions, entering the new display may produce a short fall or upward snap according to the existing surface physics.
- A maximized top-level window is never a landing candidate. Ordinary visible, non-minimized windows remain eligible subject to the existing overlap, occlusion, stale, and host-exclusion checks.
- A maximized candidate is identified by the Windows `IsZoomed` state and by a rectangle that covers its resolved display working area. The second check protects against window-manager states where the style flag is not reliable.

## Boundaries

- The change does not move, resize, minimize, or close user windows.
- It does not change display settings, startup behavior, network behavior, permission requirements, or desktop-file operations.
- The global horizontal span is intentionally the bounding span of valid working areas, including coordinate gaps. Routing through arbitrary display layouts is out of scope; the existing display resolver remains responsible for selecting the nearest display and its floor.

## Components

- `YimyWalk.Core.Display.DisplaySnapshot` exposes the global horizontal travel bounds.
- `YimyWalk.Core.Simulation.IWorldSurface` exposes an optional horizontal travel span with a single-area fallback for existing test surfaces.
- `YimyWalk.Core.Simulation.PetEngine` wraps walking positions against that span while retaining the resolved display area for vertical movement and landing.
- `YimyWalk.Core.Display.WindowSurfaceCandidate` carries an `IsMaximized` flag, and `SurfaceSelector` rejects those candidates.
- `YimyWalk.App.Host.WindowSurfaceResolver` reads `IsZoomed` and compares candidate bounds with the resolved working area before publishing candidates.

## Verification

- Core tests prove the global span for multiple displays, right-to-left and left-to-right wrapping, crossing from one display range into another, and rejection of maximized candidates.
- Existing Core physics and surface-selection tests remain green.
- Windows smoke adds targeted assertions for a maximized probe not being selected as a landing surface and for a pet walking from one visible display working area into the other when the current topology exposes both displays. The existing 83/83 dual-display matrix is not repeated unless topology changes.
- Status documentation distinguishes code implementation, Core tests, build/package verification, and targeted Windows desktop evidence; unverified negative-coordinate, hot-plug, sleep/wake, long-duration, and permission scenarios remain explicit.
