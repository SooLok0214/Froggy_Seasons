using UnityEngine;

// Queries only the solid root body, never the larger damage trigger.
// Kept separate from physics integration so kinematic enemies retain their
// existing height/animation, without being allowed to MovePosition through walls.
public sealed class EnemyObstacleCollision
{
    private const float ContactTolerance = 0.003f;
    private const float Skin = 0.01f;
    private readonly Collider[] overlaps = new Collider[128];
    private readonly RaycastHit[] hits = new RaycastHit[128];
    private readonly Transform owner;
    private readonly Vector3 center;
    private readonly Vector3 halfSize;
    private readonly PhysicsScene physicsScene;
    private readonly bool separateEnemies;
    public float HorizontalRadius { get; }

    public EnemyObstacleCollision(BoxCollider body, Vector3 scale, PhysicsScene scene,
        Transform ignoreOwner = null, bool useCrowdSeparation = false)
    {
        owner = ignoreOwner;
        separateEnemies = useCrowdSeparation;
        physicsScene = scene;
        center = Vector3.Scale(body.center, scale);
        Vector3 size = Vector3.Scale(body.size, scale);
        halfSize = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z)) * 0.5f;
        HorizontalRadius = new Vector2(halfSize.x, halfSize.z).magnitude + new Vector2(center.x, center.z).magnitude;
        // Allow normal floor contact / tiny floating point error, not penetration.
        halfSize = Vector3.Max(halfSize - Vector3.one * ContactTolerance, Vector3.one * 0.001f);
    }

    private bool IsObstacle(Collider other)
    {
        if (other == null || other.isTrigger ||
            (owner != null && other.transform.IsChildOf(owner))) return false;
        // Kinematic enemies use body-sized crowd separation, not an overlap
        // veto: otherwise two overlapping enemies can never move apart.
        // Spawn queries leave this OFF and still reject occupied enemy space.
        if (separateEnemies && other.GetComponentInParent<EnemyFollowPlayer>() != null) return false;
        return true;
    }

    public bool IsClear(Vector3 position, Quaternion rotation)
    {
        if (EnemyWaterArea.BlocksPosition(physicsScene, position, HorizontalRadius)) return false;
        int count = physicsScene.OverlapBox(position + rotation * center, halfSize,
            overlaps, rotation, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        // Overflow must fail closed rather than miss an unreturned obstacle.
        if (count == overlaps.Length) return false;
        for (int i = 0; i < count; i++)
            if (IsObstacle(overlaps[i])) return false;
        return true;
    }

    public Quaternion SafeRotation(Vector3 position, Quaternion from, Quaternion to)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(Quaternion.Angle(from, to) / 5f));
        Quaternion safe = from;
        for (int i = 1; i <= steps; i++)
        {
            Quaternion next = Quaternion.Slerp(from, to, (float)i / steps);
            if (!IsClear(position, next)) break;
            safe = next;
        }
        return safe;
    }

    public Vector3 Move(Vector3 position, Quaternion rotation, Vector3 displacement)
    {
        if (!IsClear(position, rotation)) return position;
        Vector3 start = position;
        for (int pass = 0; pass < 3 && displacement.sqrMagnitude > 0.00000001f; pass++)
        {
            float length = displacement.magnitude;
            Vector3 direction = displacement / length;
            int count = physicsScene.BoxCast(position + rotation * center, halfSize,
                direction, hits, rotation, length + Skin, Physics.AllLayers,
                QueryTriggerInteraction.Ignore);
            if (count == hits.Length) break;
            float nearest = length + Skin;
            Vector3 normal = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                if (!IsObstacle(hits[i].collider)) continue;
                if (Vector3.Dot(direction, hits[i].normal) >= -0.0001f) continue;
                if (hits[i].distance <= nearest)
                {
                    nearest = hits[i].distance;
                    normal = hits[i].normal;
                }
            }
            if (EnemyWaterArea.Cast(physicsScene, position, displacement, HorizontalRadius,
                out float waterFraction, out Vector3 waterNormal) && waterFraction * length <= nearest)
            {
                nearest = waterFraction * length;
                normal = waterNormal;
            }
            float travel = normal == Vector3.zero ? length : Mathf.Clamp(nearest - Skin, 0f, length);
            Vector3 next = position + direction * travel;
            if (!IsClear(next, rotation)) break;
            position = next;
            if (normal == Vector3.zero) break;
            // Slide along vertical obstacles, but don't climb walls / slopes.
            Vector3 remaining = displacement - direction * travel;
            displacement = Vector3.ProjectOnPlane(remaining, normal);
            if (Mathf.Abs(normal.y) < 0.7f) displacement.y = remaining.y;
        }
        return IsClear(position, rotation) ? position : start;
    }
}
