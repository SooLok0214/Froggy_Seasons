using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class EnemyFollowPlayer : MonoBehaviour
{
    public const float MaximumMoveSpeed = 9f;

    public enum MovementAnimationType
    {
        None,
        BobUpAndDown,
        SquashY
    }

    public Transform player;

    public float moveSpeed = 2f;
    public float turnSpeed = 8f;
    public float stopDistance = 1.2f;

    public float rotateLeft = -90f;

    [Header("Chase / Patrol")]
    [Min(0f), Tooltip("Horizontal world-space distance at which a patrolling enemy starts chasing.")]
    public float chaseStartDistance = 22f;
    [Min(0.1f), Tooltip("Stop chasing beyond this horizontal distance. Keep larger than Chase Start Distance.")]
    public float chaseStopDistance = 30f;
    [Min(0f), Tooltip("Patrol radius around the spawn point or the position where the chase ended.")]
    public float patrolRadius = 6f;
    [Range(0f, 1f), Tooltip("Patrol speed relative to Move Speed. Chase speed is unchanged.")]
    public float patrolSpeedMultiplier = 0.45f;
    [Min(0f)] public float patrolWaitTime = 1.5f;

    public bool IsChasing { get; private set; }
    private Vector3 patrolCenter;
    private Vector3 patrolTarget;
    private bool behaviourInitialized;
    private bool hasPatrolTarget;
    private float patrolWaitRemaining;
    private float patrolBlockedTime;
    private BoxCollider bodyCollider;
    private readonly RaycastHit[] patrolGroundHits = new RaycastHit[16];

    // 怪物之間保持距離
    public float separationRadius = 1.5f;
    public float separationStrength = 2f;

    [Range(0.4f, 1f)]
    [Tooltip("Monster-to-monster spacing only. Lower values allow a tighter crowd; environment colliders and attack ranges are unchanged.")]
    public float crowdSpacingMultiplier = 0.7f;

    public Rigidbody rb;

    [Header("Stationary Cleanup")]
    [Min(0f), Tooltip("Remove after this many gameplay seconds without horizontal movement, without score or XP. 0 disables cleanup.")]
    public float stationaryDespawnTime = 5f;
    [Min(0.001f), Tooltip("Horizontal movement in world units needed to reset the timer. Vertical bobbing and tiny collision jitter do not count.")]
    public float stationaryPositionTolerance = 0.05f;

    private Vector3 stationaryAnchor;
    private float stationaryElapsed;
    private bool stationaryInitialized;
    [System.NonSerialized] public Collider[] nearbyColliders = new Collider[128];
    private readonly HashSet<EnemyFollowPlayer> nearbyEnemies = new HashSet<EnemyFollowPlayer>();
    private float crowdBodyRadius;

    public float CrowdBodyRadius
    {
        get
        {
            if (crowdBodyRadius <= 0f)
            {
                BoxCollider body = GetComponent<BoxCollider>();
                if (body == null) return 0.1f;
                Vector3 size = Vector3.Scale(body.size, transform.lossyScale) * 0.5f;
                Vector3 center = Vector3.Scale(body.center, transform.lossyScale);
                // Cache the unscaled footprint so the Inspector multiplier can
                // change immediately without changing the real collision shape.
                crowdBodyRadius = new Vector2(size.x, size.z).magnitude + new Vector2(center.x, center.z).magnitude;
            }
            return Mathf.Max(0.1f, crowdBodyRadius * Mathf.Clamp(crowdSpacingMultiplier, 0.4f, 1f));
        }
    }

    [Header("Movement Animation")]
    [Tooltip("Bee / Ice use Bob Up And Down; Grass / Musroom use Squash Y.")]
    public MovementAnimationType movementAnimationType;

    [Tooltip("Optional visual child to animate. Leave empty to animate this monster root.")]
    public Transform animationTarget;

    [Min(0.01f)]
    public float movementAnimationSpeed = 5f;

    [Min(0f)]
    public float bobHeight = 0.12f;

    [Range(0.1f, 1f)]
    public float minimumYScale = 0.8f;

    [Min(0.01f)]
    public float animationReturnSpeed = 8f;

    private Vector3 originalAnimationLocalPosition;
    private Vector3 originalAnimationLocalScale;
    private float animationTime;
    private float currentBobOffset;
    private bool isMoving;
    private bool animationInitialized;
    private EnemyObstacleCollision obstacleCollision;
    private EnemyWaterArea waterDetourArea;
    private int waterDetourSide;

    public float CurrentMoveSpeed => Mathf.Clamp(moveSpeed, 0f, MaximumMoveSpeed);

    public void OnValidate()
    {
        moveSpeed = CurrentMoveSpeed;
    }

    public void IncreaseMoveSpeed(float amount)
    {
        if (amount <= 0f)
            return;

        moveSpeed = Mathf.Min(MaximumMoveSpeed, CurrentMoveSpeed + amount);
    }

    public void Start()
    {
        moveSpeed = CurrentMoveSpeed;
        rb = GetComponent<Rigidbody>();
        ResetStationaryTimer(rb != null ? rb.position : transform.position);
        BoxCollider body = GetComponent<BoxCollider>();
        bodyCollider = body;
        BeginPatrol(transform.position);
        if (body != null && body.enabled && !body.isTrigger)
            obstacleCollision = new EnemyObstacleCollision(body, transform.lossyScale,
                gameObject.scene.GetPhysicsScene(), transform, true);
        else
            Debug.LogError("Enemy needs an enabled, non-trigger root BoxCollider for safe movement.", this);

        if (animationTarget == null)
        {
            animationTarget = transform;
        }

        originalAnimationLocalPosition = animationTarget.localPosition;
        originalAnimationLocalScale = animationTarget.localScale;
        animationInitialized = true;

        if (rb != null)
        {
            rb.freezeRotation = false;

            rb.constraints =
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;

            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode =
                rb.isKinematic ? CollisionDetectionMode.ContinuousSpeculative :
                CollisionDetectionMode.ContinuousDynamic;
        }
    }

    public void FixedUpdate()
    {
        if (Time.timeScale <= 0f)
            return;

        // Measure actual physics positions before requesting the next move.
        // Patrol target changes and blocked-movement retries must not reset it.
        if (UpdateStationaryTimer(rb != null ? rb.position : transform.position, Time.fixedDeltaTime))
        {
            DespawnWithoutReward();
            return;
        }

        if (rb == null || obstacleCollision == null)
            return;

        if (!behaviourInitialized) BeginPatrol(rb.position);
        UpdateChaseState(rb.position);
        Vector3 direction;
        if (IsChasing)
        {
            direction = player.position - rb.position;
            direction.y = 0;
        }
        else
        {
            direction = GetPatrolDirection(rb.position);
        }

        if (direction.sqrMagnitude > 0.0001f)
        {
            Vector3 destination = rb.position + direction;
            direction = EnemyWaterArea.SteeringTarget(gameObject.scene.GetPhysicsScene(), rb.position,
                destination, obstacleCollision.HorizontalRadius, ref waterDetourArea, ref waterDetourSide) - rb.position;
            direction.y = 0;
        }

        // =========================
        // Chase faces the player; patrol faces its local waypoint.
        // =========================

        Quaternion movementRotation = rb.rotation;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction)
                * Quaternion.Euler(0, rotateLeft, 0);

            Quaternion newRotation =
                Quaternion.Slerp(
                    rb.rotation,
                    targetRotation,
                    turnSpeed * Time.fixedDeltaTime
                );

            movementRotation = obstacleCollision.SafeRotation(rb.position, rb.rotation, newRotation);
            rb.MoveRotation(movementRotation);
        }

        // =========================
        // 怪物互相分開
        // =========================

        Vector3 separation = Vector3.zero;

        nearbyEnemies.Clear();
        float ownRadius = CrowdBodyRadius;
        // Ray/overlap calls must query this scene (also supports isolated tests).
        int nearbyCount = gameObject.scene.GetPhysicsScene().OverlapSphere(
            rb.position, Mathf.Max(separationRadius, ownRadius * 2f + 0.5f),
            nearbyColliders, Physics.AllLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < nearbyCount; i++)
        {
            Collider col = nearbyColliders[i];

            if (col.gameObject == gameObject)
                continue;

            EnemyFollowPlayer otherEnemy =
                col.GetComponentInParent<EnemyFollowPlayer>();

            if (otherEnemy == null)
                continue;

            if (otherEnemy == this)
                continue;

            if (!nearbyEnemies.Add(otherEnemy))
                continue;

            Vector3 away =
                rb.position -
                (otherEnemy.rb != null ? otherEnemy.rb.position : otherEnemy.transform.position);

            away.y = 0;

            float distance = away.magnitude;

            float safeDistance = ownRadius + otherEnemy.CrowdBodyRadius + 0.08f;
            float influenceDistance = Mathf.Max(separationRadius, safeDistance + 0.5f);
            if (distance >= influenceDistance) continue;

            // A stable, opposite direction for each pair even at identical pivots.
            Vector3 awayDirection = distance > 0.001f ? away / distance : CoincidentSeparation(otherEnemy);
            float weight = distance < safeDistance
                ? 2f + 2f * (1f - distance / safeDistance)
                : Mathf.Max(0f, separationStrength) * (influenceDistance - distance) / (influenceDistance - safeDistance);
            separation += awayDirection * weight;
        }

        // =========================
        // Follow the active chase target or patrol waypoint.
        // =========================

        Vector3 moveDirection = Vector3.zero;

        if (direction.sqrMagnitude > (IsChasing ? stopDistance * stopDistance : 0.0001f))
        {
            moveDirection = direction.normalized;
        }

        // 加入互相推開效果
        moveDirection += separation;

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        isMoving = moveDirection.sqrMagnitude > 0.0001f;

        Vector3 targetPosition =
            rb.position +
            moveDirection *
            CurrentMoveSpeed * (IsChasing ? 1f : Mathf.Clamp01(patrolSpeedMultiplier)) *
            Time.fixedDeltaTime;

        if (!IsChasing)
        {
            // Keep local patrol bounded even when crowd separation pushes it.
            Vector3 fromCenter = targetPosition - patrolCenter;
            fromCenter.y = 0;
            float radius = Mathf.Max(0f, patrolRadius);
            if (fromCenter.sqrMagnitude > radius * radius)
            {
                Vector3 edge = patrolCenter + Vector3.ClampMagnitude(fromCenter, radius);
                // A radius edited at runtime must not teleport the enemy.
                Vector2 limited = Vector2.MoveTowards(new Vector2(rb.position.x, rb.position.z),
                    new Vector2(edge.x, edge.z), CurrentMoveSpeed * Mathf.Clamp01(patrolSpeedMultiplier) * Time.fixedDeltaTime);
                targetPosition.x = limited.x;
                targetPosition.z = limited.y;
            }
        }

        float previousBobOffset = currentBobOffset;
        UpdateMovementAnimation(ref targetPosition);

        targetPosition = obstacleCollision.Move(rb.position, movementRotation, targetPosition - rb.position);
        if (!IsChasing)
        {
            // Do not patrol off an edge or onto unsupported space. Never add or
            // change map colliders to create patrol paths.
            if (!HasPatrolGround(targetPosition))
            {
                targetPosition.x = rb.position.x;
                targetPosition.z = rb.position.z;
            }
            Vector3 moved = targetPosition - rb.position;
            moved.y = 0;
            patrolBlockedTime = isMoving && moved.sqrMagnitude < 0.000001f
                ? patrolBlockedTime + Time.fixedDeltaTime : 0f;
            if (patrolBlockedTime >= 1f)
            {
                hasPatrolTarget = false;
                patrolWaitRemaining = Mathf.Max(0f, patrolWaitTime);
                patrolBlockedTime = 0f;
            }
        }
        if (animationTarget == transform && movementAnimationType == MovementAnimationType.BobUpAndDown)
            currentBobOffset = previousBobOffset + targetPosition.y - rb.position.y;

        rb.MovePosition(targetPosition);
    }

    private void ResetStationaryTimer(Vector3 position)
    {
        stationaryAnchor = position;
        stationaryElapsed = 0f;
        stationaryInitialized = true;
    }

    private bool UpdateStationaryTimer(Vector3 position, float deltaTime)
    {
        if (!stationaryInitialized || stationaryDespawnTime <= 0f)
        {
            ResetStationaryTimer(position);
            return false;
        }
        if (deltaTime <= 0f) return false;

        Vector3 offset = position - stationaryAnchor;
        offset.y = 0f;
        float tolerance = Mathf.Max(0.001f, stationaryPositionTolerance);
        if (offset.sqrMagnitude >= tolerance * tolerance)
        {
            ResetStationaryTimer(position);
            return false;
        }

        stationaryElapsed += deltaTime;
        return stationaryElapsed + 0.00001f >= stationaryDespawnTime;
    }

    private void DespawnWithoutReward()
    {
        // Do not call Die/TakeDamage: normal deaths award kills and XP.
        // Block a projectile arriving later in this same frame from scoring.
        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health != null) health.isDead = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void BeginPatrol(Vector3 position)
    {
        IsChasing = false;
        patrolCenter = position;
        hasPatrolTarget = false;
        patrolWaitRemaining = 0f;
        patrolBlockedTime = 0f;
        behaviourInitialized = true;
    }

    private void UpdateChaseState(Vector3 position)
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            if (IsChasing) BeginPatrol(position);
            return;
        }

        Vector3 toPlayer = player.position - position;
        toPlayer.y = 0; // Flying/bobbing models and terrain height must not affect aggro.
        float stop = Mathf.Max(0.1f, chaseStopDistance);
        float start = Mathf.Clamp(chaseStartDistance, 0f, stop);
        if (IsChasing)
        {
            if (toPlayer.sqrMagnitude > stop * stop) BeginPatrol(position);
        }
        else if (toPlayer.sqrMagnitude <= start * start)
        {
            IsChasing = true;
            hasPatrolTarget = false;
            patrolBlockedTime = 0f;
        }
    }

    private Vector3 GetPatrolDirection(Vector3 position)
    {
        if (patrolRadius <= 0f || patrolSpeedMultiplier <= 0f) return Vector3.zero;
        if (patrolWaitRemaining > 0f)
        {
            patrolWaitRemaining = Mathf.Max(0f, patrolWaitRemaining - Time.fixedDeltaTime);
            return Vector3.zero;
        }

        if (!hasPatrolTarget)
        {
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * Mathf.Max(0f, patrolRadius);
                Vector3 candidate = new Vector3(patrolCenter.x + offset.x, position.y, patrolCenter.z + offset.y);
                Vector3 delta = candidate - position;
                delta.y = 0;
                if (delta.sqrMagnitude <= 0.09f ||
                    !obstacleCollision.IsClear(candidate, rb.rotation) || !HasPatrolGround(candidate)) continue;
                patrolTarget = candidate;
                hasPatrolTarget = true;
                break;
            }
            if (!hasPatrolTarget)
            {
                patrolWaitRemaining = Mathf.Max(0.25f, patrolWaitTime);
                return Vector3.zero;
            }
        }

        Vector3 direction = patrolTarget - position;
        direction.y = 0;
        if (direction.sqrMagnitude <= 0.09f)
        {
            hasPatrolTarget = false;
            patrolWaitRemaining = Mathf.Max(0f, patrolWaitTime);
            return Vector3.zero;
        }
        return direction;
    }

    private bool HasPatrolGround(Vector3 position)
    {
        if (bodyCollider == null) return false;
        float bottomOffset = bodyCollider.bounds.min.y - rb.position.y;
        Vector3 origin = position + Vector3.up * (bottomOffset + 0.5f);
        int count = gameObject.scene.GetPhysicsScene().Raycast(origin, Vector3.down, patrolGroundHits,
            1.5f + Mathf.Max(0f, bobHeight), Physics.AllLayers, QueryTriggerInteraction.Ignore);
        if (count == patrolGroundHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            var hit = patrolGroundHits[i];
            if (hit.normal.y < 0.6f || hit.collider.attachedRigidbody != null ||
                hit.collider.transform.IsChildOf(transform)) continue;
            return true;
        }
        return false;
    }

    private Vector3 CoincidentSeparation(EnemyFollowPlayer other)
    {
        EntityId a = GetEntityId(), b = other.GetEntityId();
        bool first = a.CompareTo(b) < 0;
        uint hash = unchecked((uint)(first ? a : b).GetHashCode() * 73856093u ^
                              (uint)(first ? b : a).GetHashCode() * 19349663u);
        float angle = (hash % 360u) * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        return first ? direction : -direction;
    }

    private void UpdateMovementAnimation(ref Vector3 targetPosition)
    {
        if (movementAnimationType == MovementAnimationType.None || animationTarget == null)
            return;

        if (isMoving)
        {
            animationTime += Time.fixedDeltaTime * movementAnimationSpeed;
        }

        // Starts at the original pose, rises / shrinks, then returns to it.
        float wave01 = (1f - Mathf.Cos(animationTime)) * 0.5f;

        if (movementAnimationType == MovementAnimationType.BobUpAndDown)
        {
            float targetBobOffset = isMoving ? wave01 * bobHeight : 0f;
            float previousBobOffset = currentBobOffset;
            currentBobOffset = Mathf.MoveTowards(
                currentBobOffset,
                targetBobOffset,
                animationReturnSpeed * Time.fixedDeltaTime
            );

            if (animationTarget == transform)
            {
                // Remove the previous offset before applying the next one so the
                // monster never drifts upward over time.
                targetPosition.y = rb.position.y - previousBobOffset + currentBobOffset;
            }
            else
            {
                Vector3 localPosition = originalAnimationLocalPosition;
                localPosition.y += currentBobOffset;
                animationTarget.localPosition = localPosition;
            }
        }
        else if (movementAnimationType == MovementAnimationType.SquashY)
        {
            float targetYScale = isMoving
                ? Mathf.Lerp(originalAnimationLocalScale.y, originalAnimationLocalScale.y * minimumYScale, wave01)
                : originalAnimationLocalScale.y;

            Vector3 scale = animationTarget.localScale;
            scale.x = originalAnimationLocalScale.x;
            scale.y = Mathf.MoveTowards(
                scale.y,
                targetYScale,
                animationReturnSpeed * Time.fixedDeltaTime
            );
            scale.z = originalAnimationLocalScale.z;
            animationTarget.localScale = scale;
        }

        if (!isMoving && currentBobOffset <= 0.0001f)
        {
            animationTime = 0f;
        }
    }

    private void OnDisable()
    {
        stationaryInitialized = false;
        stationaryElapsed = 0f;
        waterDetourArea = null;
        waterDetourSide = 0;
        behaviourInitialized = false;
        IsChasing = false;
        hasPatrolTarget = false;
        if (!animationInitialized || animationTarget == null)
            return;

        if (movementAnimationType == MovementAnimationType.BobUpAndDown)
        {
            if (animationTarget == transform && rb != null && currentBobOffset > 0f)
            {
                Vector3 restoredPosition = rb.position;
                restoredPosition.y -= currentBobOffset;
                rb.position = restoredPosition;
            }
            else if (animationTarget != transform)
            {
                animationTarget.localPosition = originalAnimationLocalPosition;
            }
        }

        animationTarget.localScale = originalAnimationLocalScale;
        currentBobOffset = 0f;
        animationTime = 0f;
        isMoving = false;
    }
}
