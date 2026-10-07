# YimyWalk charged jumps and reachable window trajectories

Date: 2026-10-07

## Goal

Make autonomous jumps visibly varied and make visible-window jumps capable of reaching higher window tops without teleporting. Ordinary jumps remain attention-seeking and probabilistic; a window jump is a planned physical trajectory selected only when its landing can be reached.

## Behavior

Each eligible autonomous jump attempt consumes its existing attempt timer once. The attempt randomly chooses one of two ordinary-jump modes:

- **Uncharged:** start the existing jump immediately with the role's base vertical jump speed.
- **Charged:** enter a new `JumpCharge` state for a random duration in the bounded 0.15–0.45 second interval. During this state horizontal position and velocity stay fixed, the renderer always selects the static `Apex` frame (frame 7), and no new movement is sampled. At the end of the charge, launch with a vertical speed derived from the duration. The launch speed is capped at 1.75 times the role's base `JumpSpeed`; horizontal speed remains within the existing walking-speed limit.

The charged/uncharged choice and duration are sampled once per attempt. A zero ordinary jump desire prevents starting either mode; it never interrupts a jump or charge already in progress. Existing activity-rate scaling, pause behavior, drag priority, Eat/airborne restrictions, and retry intervals remain unchanged.

When window landing is enabled and the independent `WindowJumpDesire` branch selects a reachable candidate, the attempt uses a planned charged trajectory. The planner computes a descending intersection time, a valid landing X interval after scaled-frame margins, and a continuous ballistic path whose apex is high enough to clear the candidate top. Horizontal velocity is clamped to the existing walk-speed limit. The planned vertical launch speed uses a separate finite `6x` role `JumpSpeed` ceiling. This target-only ceiling is explicit because the ordinary `1.75x` ceiling cannot clear a typical desktop-height difference; ordinary attention-seeking jumps keep the smaller cap. If no candidate satisfies all constraints, the attempt falls back to the ordinary charged/uncharged choice. The current supporting window is excluded so a 100% desire does not repeatedly select the surface already under the pet. If a target moves, disappears, becomes invalid, or loses its landing interval during flight, the pet keeps integrating its current trajectory and does not snap or replan in place.

The renderer does not draw a visible parabola line. Prediction and Tick integration use the same closed-form acceleration and terminal-speed split, so the selected descending intersection matches the simulated path. The phrase “draw a parabola” means the pet's physical position follows the continuously integrated arc, including the existing descending surface-selection logic.

## State and data flow

`PetState.JumpCharge` stores the charge timer and the sampled launch speed on `PetInstance`. `PetInstance.GetFrameKind()` maps it to `FrameKind.Apex`. `PetEngine` starts a charge or jump only from the existing autonomous attempt gate, integrates charge without displacement, and then transitions to `Jump`. Window planning carries the selected launch parameters only for that attempt; no target is retained as an attachment.

The Core remains platform-neutral. App and Windows layers do not gain trajectory policy or rendering dependencies. Existing settings and UI remain unchanged: no new user-facing charge parameter is added in this iteration.

## Safety and fallback rules

- All random durations and launch speeds are finite and clamped to their documented bounds.
- A target is valid only when the current horizontal distance, descending intersection, scaled frame margins, and landing interval all pass the planner checks.
- No horizontal speed exceeds `min(WalkSpeed, 1000)` and no state teleports.
- A target invalidation leaves ordinary physics in control; a later autonomous attempt may choose another target after the normal timer.
- Pause freezes charge and flight time without replaying overdue behavior. Dragging interrupts charge/flight through the existing priority path.

## Tests and acceptance

Core tests cover: the new charge state uses Apex and remains stationary; uncharged jumps launch immediately; charged duration changes vertical launch speed within the 1.75x cap; zero desire prevents new charge/jump; a window target above the base apex uses a reachable planned arc; horizontal speed and landing interval remain bounded; target invalidation continues ordinary physics; pause freezes charge; and failed attempts arm the next timer only once.

App/UI smoke keeps existing tray, drag, resource-release, settings, and preview regressions. The new behavior is accepted from Core and UI smoke evidence; real desktop timing and visual observation are reported separately. User-observed confirmation of proactive ordinary/window jumps is recorded as desktop evidence after implementation.
