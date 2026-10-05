# Airborne Cross-Display Toss Design

## Goal

When a dragged pet is released with horizontal velocity, its airborne motion uses the same global horizontal span as autonomous walking. Crossing the span's right edge re-enters at the left edge, and crossing the left edge re-enters at the right edge.

## Behavior

- Wrapping applies only to horizontal airborne integration after drag release; vertical position and velocity continue through the existing gravity and fall-speed rules.
- The global span comes from `IWorldSurface.HorizontalTravelBounds`, which covers the valid display working areas and any coordinate gaps between them.
- The wrapped position is used when selecting the next surface, so a pet that crosses to another display can land on that display's floor or a valid window surface.
- Landing keeps the wrapped X coordinate and uses the selected surface's bottom Y. If no positive travel span exists, the existing area clamp remains the safe fallback.
- Horizontal velocity is preserved across a wrap; no teleport event or direction reversal is generated.

## Constraints

The change remains in the platform-neutral `PetEngine`, does not access the network or desktop files, and does not alter startup, privilege, or display-topology behavior. Existing walking wrap and window-surface selection remain unchanged except that airborne surface selection receives the wrapped projected position.

## Verification

Core tests cover right-to-left and left-to-right airborne wrapping, including continuous Y movement and preserved horizontal velocity. The full Core suite, build, publish/package checks, and an available targeted Windows smoke are run after implementation. Existing 83/83 dual-display matrix testing is not repeated unless the display topology changes.
