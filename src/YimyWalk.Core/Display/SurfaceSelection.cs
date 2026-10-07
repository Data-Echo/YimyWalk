using System.Numerics;
using YimyWalk.Core.Simulation;

namespace YimyWalk.Core.Display;

public readonly record struct WindowSurfaceCandidate(
    string Id,
    WorldRect Bounds,
    bool IsVisible,
    bool IsMinimized,
    bool IsExcluded,
    bool IsStale,
    bool IsOccluded = false,
    int ZOrder = int.MaxValue,
    bool IsOcclusionBlocker = true,
    bool IsMaximized = false)
{
    public bool IsValid
        => !string.IsNullOrWhiteSpace(Id)
            && Bounds.Width > 0
            && Bounds.Height > 0;
}

public readonly record struct SurfaceSelection(WorldRect Area, bool IsFallback, string? SourceId);

public readonly record struct JumpTarget(
    WorldRect Area,
    string SourceId,
    float LandingTime,
    float LandingX,
    float HorizontalVelocity,
    float LaunchSpeed);

public static class BallisticMotion
{
    // Shared by prediction and integration, including a step that reaches
    // terminal velocity partway through its duration.
    public static (float Displacement, float Velocity) Advance(float velocity, float gravity, float maxFallSpeed, float seconds)
    {
        velocity = Math.Min(velocity, maxFallSpeed);
        var finalVelocity = Math.Min(maxFallSpeed, velocity + gravity * seconds);
        // Keep the existing fixed-step integration contract: each simulation
        // step advances by the velocity reached at that step. The planner
        // still uses the closed-form descending intersection for candidate
        // filtering, while SurfaceSelector remains the final authority.
        return (finalVelocity * seconds, finalVelocity);
    }

    public static bool TryGetDescendingTime(
        float startY,
        float targetY,
        float jumpSpeed,
        float gravity,
        float maxFallSpeed,
        out float seconds)
    {
        seconds = 0;
        if (!float.IsFinite(startY) || !float.IsFinite(targetY)
            || !float.IsFinite(jumpSpeed) || !float.IsFinite(gravity)
            || !float.IsFinite(maxFallSpeed)
            || jumpSpeed <= 0 || gravity <= 0 || maxFallSpeed <= 0)
            return false;

        return TryGetDescendingTimeFromVelocity(startY, targetY, -jumpSpeed, gravity, maxFallSpeed, out seconds);
    }

    public static bool TryGetDescendingTimeFromVelocity(float startY, float targetY, float velocity, float gravity, float maxFallSpeed, out float seconds)
    {
        seconds = 0;
        velocity = Math.Min(velocity, maxFallSpeed);
        var apexY = velocity < 0 ? startY - velocity * velocity / (2f * gravity) : startY;
        if (targetY < apexY - 0.001f) return false;

        var terminalTime = (maxFallSpeed - velocity) / gravity;
        var terminalY = startY + velocity * terminalTime + gravity * terminalTime * terminalTime / 2f;
        var discriminant = velocity * velocity + 2f * gravity * (targetY - startY);
        if (discriminant < -0.01f) return false;

        var uncappedTime = (-velocity + MathF.Sqrt(Math.Max(0, discriminant))) / gravity;
        seconds = uncappedTime <= terminalTime
            ? uncappedTime
            : terminalTime + (targetY - terminalY) / maxFallSpeed;
        return float.IsFinite(seconds) && seconds >= 0;
    }
}

public static class JumpTargetPlanner
{
    public static bool TrySelect(
        IEnumerable<WindowSurfaceCandidate> candidates,
        Vector2 start,
        FacingDirection facing,
        float horizontalSpeed,
        float jumpSpeed,
        float gravity,
        float maxFallSpeed,
        float landingMargin,
        out JumpTarget target,
        WorldRect? workingArea = null,
        float? rightLandingMargin = null,
        float? maxLaunchSpeed = null)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        target = default;
        if (!float.IsFinite(horizontalSpeed) || horizontalSpeed <= 0
            || !float.IsFinite(jumpSpeed) || jumpSpeed <= 0
            || !float.IsFinite(gravity) || gravity <= 0
            || !float.IsFinite(maxFallSpeed) || maxFallSpeed <= 0
            || !float.IsFinite(landingMargin))
            return false;

        var launchSpeedLimit = maxLaunchSpeed is { } requestedLimit && float.IsFinite(requestedLimit)
            ? Math.Max(jumpSpeed, requestedLimit)
            : jumpSpeed;

        WindowSurfaceCandidate? best = null;
        var bestTarget = default(JumpTarget);
        var bestDistance = float.PositiveInfinity;
        foreach (var candidate in candidates)
        {
            if (!candidate.IsValid || !candidate.IsVisible || candidate.IsMinimized || candidate.IsExcluded
                || candidate.IsStale || candidate.IsOccluded || candidate.IsMaximized)
                continue;

            if (workingArea is { } visibleArea && candidate.Bounds.Top < visibleArea.Top) continue;
            var left = Math.Max(candidate.Bounds.Left, workingArea?.Left ?? candidate.Bounds.Left) + Math.Max(0, landingMargin);
            var right = Math.Min(candidate.Bounds.Right, workingArea?.Right ?? candidate.Bounds.Right) - Math.Max(0, rightLandingMargin ?? landingMargin);
            if (right <= left) continue;
            var requiredSpeed = jumpSpeed;
            if (candidate.Bounds.Top < start.Y)
            {
                var rise = start.Y - candidate.Bounds.Top;
                var apexSpeed = MathF.Sqrt(Math.Max(0, 2f * gravity * rise));
                // Leave a small descending margin above the exact apex so the
                // planner cannot select a trajectory that merely touches a
                // window and has no downward crossing.
                requiredSpeed = Math.Max(requiredSpeed, apexSpeed + 25f);
            }
            if (requiredSpeed > launchSpeedLimit + 0.001f)
                continue;
            if (!BallisticMotion.TryGetDescendingTime(start.Y, candidate.Bounds.Top, requiredSpeed, gravity, maxFallSpeed, out var time))
                continue;

            var distance = facing == FacingDirection.Right
                ? left - start.X
                : start.X - right;
            if (facing == FacingDirection.Right && right <= start.X + 0.5f) continue;
            if (facing == FacingDirection.Left && left >= start.X - 0.5f) continue;

            // Aim a small distance inside the valid interval. This absorbs a
            // single bounded simulation step at the descending crossing and
            // avoids grazing the four-pixel safety margin.
            var settle = Math.Min(horizontalSpeed * 0.1f, Math.Max(0, right - left) / 2f);
            var landingX = facing == FacingDirection.Right
                ? Math.Max(start.X, left + settle)
                : Math.Min(start.X, right - settle);
            landingX = Math.Clamp(landingX, left, right);
            var velocity = (landingX - start.X) / time;
            if (!float.IsFinite(velocity) || Math.Abs(velocity) > horizontalSpeed + 0.001f)
                continue;
            if (distance < 0) distance = 0;
            if (best is not null && distance >= bestDistance) continue;

            best = candidate;
            bestDistance = distance;
            bestTarget = new JumpTarget(
                new WorldRect(candidate.Bounds.Left, candidate.Bounds.Top, candidate.Bounds.Right, candidate.Bounds.Top),
                candidate.Id,
                time,
                landingX,
                velocity,
                requiredSpeed);
        }

        if (best is null) return false;
        target = bestTarget;
        return true;
    }
}

public static class SurfaceSelector
{
    public static SurfaceSelection Select(
        IEnumerable<WindowSurfaceCandidate> candidates,
        WorldRect fallback,
        float footLeft,
        float footRight,
        float currentY,
        float nextY)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (footRight < footLeft) (footLeft, footRight) = (footRight, footLeft);
        var descending = nextY >= currentY;
        WindowSurfaceCandidate? best = null;
        var bestDistance = float.PositiveInfinity;

        if (descending)
        {
            foreach (var candidate in candidates)
            {
                if (!candidate.IsValid || !candidate.IsVisible || candidate.IsMinimized || candidate.IsExcluded || candidate.IsStale || candidate.IsOccluded || candidate.IsMaximized)
                    continue;
                const float safetyMargin = 4f;
                if (candidate.Bounds.Right <= footLeft || candidate.Bounds.Left >= footRight
                    || footLeft < candidate.Bounds.Left + safetyMargin
                    || footRight > candidate.Bounds.Right - safetyMargin)
                    continue;
                if (candidate.Bounds.Top < currentY || candidate.Bounds.Top > nextY)
                    continue;

                var distance = candidate.Bounds.Top - currentY;
                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
        }

        if (!best.HasValue)
            return new SurfaceSelection(fallback, IsFallback: true, SourceId: null);

        var selected = best.Value;
        return new SurfaceSelection(
            new WorldRect(selected.Bounds.Left, selected.Bounds.Top, selected.Bounds.Right, selected.Bounds.Top),
            IsFallback: false,
            selected.Id);
    }
}
