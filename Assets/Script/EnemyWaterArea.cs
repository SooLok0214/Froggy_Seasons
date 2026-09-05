using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// An enemy-only horizontal exclusion over an existing round water trigger.
// This does not add a collider, change physics layers, or affect the player.
[ExecuteAlways, DisallowMultipleComponent]
public sealed class EnemyWaterArea : MonoBehaviour
{
    [Tooltip("Existing water trigger. Its XZ bounds define the round no-go area; height is ignored for enemies.")]
    public Collider waterTrigger;
    [Min(0f), Tooltip("Extra shore clearance in world units, in addition to the enemy's full body footprint.")]
    public float shoreClearance = 0.35f;

    private static readonly List<EnemyWaterArea> areas = new List<EnemyWaterArea>();

    private void OnEnable() { if (!areas.Contains(this)) areas.Add(this); }
    private void OnDisable() { areas.Remove(this); }
    private void OnDestroy() { areas.Remove(this); }

    public bool TryGetCircle(PhysicsScene scene, float bodyRadius, out Vector2 center, out float radius)
    {
        center = default; radius = 0;
        if (!isActiveAndEnabled || waterTrigger == null || !waterTrigger.enabled ||
            !waterTrigger.gameObject.activeInHierarchy || gameObject.scene.GetPhysicsScene() != scene) return false;
        Bounds bounds = waterTrigger.bounds;
        center = new Vector2(bounds.center.x, bounds.center.z);
        // Deadplace2 is an almost round cylinder. Use the larger horizontal
        // radius so its entire water footprint stays inside the exclusion.
        radius = Mathf.Max(bounds.extents.x, bounds.extents.z) + Mathf.Max(0, shoreClearance) + Mathf.Max(0, bodyRadius);
        return radius > 0;
    }

    public static bool BlocksPosition(PhysicsScene scene, Vector3 position, float bodyRadius)
    {
        Vector2 point = Flat(position);
        foreach (var area in areas)
            if (area != null && area.TryGetCircle(scene, bodyRadius, out var center, out float radius) &&
                (point - center).sqrMagnitude < radius * radius) return true;
        return false;
    }

    public static bool Cast(PhysicsScene scene, Vector3 start, Vector3 displacement, float bodyRadius,
        out float fraction, out Vector3 normal)
    {
        fraction = 1; normal = Vector3.zero;
        Vector2 origin = Flat(start), delta = Flat(displacement);
        bool hit = false;
        foreach (var area in areas)
        {
            if (area == null || !area.TryGetCircle(scene, bodyRadius, out var center, out float radius)) continue;
            if (!CastCircle(origin, delta, center, radius, out float t) || t > fraction) continue;
            fraction = t;
            Vector2 away = (origin + delta * t - center).normalized;
            normal = new Vector3(away.x, 0, away.y);
            hit = true;
        }
        return hit;
    }

    // Persistent clockwise/counterclockwise choice prevents symmetric targets
    // and small frame-to-frame player movement from making an enemy zigzag.
    public static Vector3 SteeringTarget(PhysicsScene scene, Vector3 start, Vector3 goal, float bodyRadius,
        ref EnemyWaterArea detourArea, ref int detourSide)
    {
        Vector2 origin = Flat(start), destination = Flat(goal);
        EnemyWaterArea blocking = null;
        Vector2 center = default;
        float radius = 0, nearest = float.PositiveInfinity;
        foreach (var area in areas)
        {
            if (area == null || !area.TryGetCircle(scene, bodyRadius, out var c, out float r)) continue;
            // Visibility uses the hard boundary, not the outer steering ring.
            // Otherwise arc chords just inside that ring never regain a clear
            // line to the goal, and an enemy can orbit the lake indefinitely.
            Vector2 offset = origin - c, delta = destination - origin;
            if (offset.sqrMagnitude >= r * r && Vector2.Dot(offset, delta) >= 0) continue;
            if (!CastCircle(origin, delta, c, r + 0.05f, out float t) || t >= nearest) continue;
            nearest = t; blocking = area; center = c; radius = r + 0.5f;
        }
        if (blocking == null) { detourArea = null; detourSide = 0; return goal; }

        Vector2 fromCenter = origin - center;
        float fromDistance = fromCenter.magnitude;
        Vector2 outward = fromDistance > 0.001f ? fromCenter / fromDistance : Vector2.right;
        Vector2 toCenter = destination - center;
        float toDistance = toCenter.magnitude;
        // Never chase a player into water. Wait at the nearest safe shore.
        if (toDistance <= radius)
            return AtHeight(center + outward * (radius + 0.15f), start.y);
        if (fromDistance < radius - 0.2f)
            return AtHeight(center + outward * (radius + 1f), start.y);

        float fromAngle = Mathf.Atan2(fromCenter.y, fromCenter.x);
        float toAngle = Mathf.Atan2(toCenter.y, toCenter.x);
        float fromTangent = Mathf.Acos(Mathf.Clamp01(radius / Mathf.Max(radius, fromDistance)));
        float toTangent = Mathf.Acos(Mathf.Clamp01(radius / Mathf.Max(radius, toDistance)));
        if (detourArea != blocking || detourSide == 0)
        {
            float ccw = WrapAngle((toAngle - toTangent) - (fromAngle + fromTangent));
            float cw = WrapAngle((fromAngle - fromTangent) - (toAngle + toTangent));
            detourSide = ccw <= cw ? 1 : -1;
            detourArea = blocking;
        }
        float entryAngle = fromAngle + detourSide * fromTangent;
        Vector2 entry = center + Direction(entryAngle) * radius;
        // Far from shore: go to the tangent. Near shore: look a small angle
        // ahead around the ring. The hard sweep still prevents crossing it.
        if ((entry - origin).sqrMagnitude <= 1f || fromDistance <= radius + 0.2f)
        {
            float step = Mathf.Min(0.18f, Mathf.Sqrt(0.4f / Mathf.Max(1f, radius)));
            entry = center + Direction(fromAngle + detourSide * step) * radius;
        }
        return AtHeight(entry, start.y);
    }

    private static bool CastCircle(Vector2 origin, Vector2 delta, Vector2 center, float radius, out float t)
    {
        t = 0;
        Vector2 offset = origin - center;
        float c = offset.sqrMagnitude - radius * radius;
        if (c < 0) return true;
        float a = delta.sqrMagnitude;
        if (a < 0.00000001f) return false;
        float b = Vector2.Dot(offset, delta);
        if (b >= 0) return false;
        float discriminant = b * b - a * c;
        if (discriminant < 0) return false;
        t = (-b - Mathf.Sqrt(discriminant)) / a;
        return t >= 0 && t <= 1;
    }
    private static float WrapAngle(float angle) => Mathf.Repeat(angle, Mathf.PI * 2);
    private static Vector2 Direction(float angle) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    private static Vector2 Flat(Vector3 p) => new Vector2(p.x, p.z);
    private static Vector3 AtHeight(Vector2 p, float y) => new Vector3(p.x, y, p.y);
}
