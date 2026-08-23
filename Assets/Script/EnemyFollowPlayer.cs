using UnityEngine;

public class EnemyFollowPlayer : MonoBehaviour
{
    public Transform player;

    public float moveSpeed = 2f;
    public float turnSpeed = 8f;
    public float stopDistance = 1.2f;

    public float rotateLeft = -90f;

    // 怪物之間保持距離
    public float separationRadius = 1.5f;
    public float separationStrength = 2f;

    public Rigidbody rb;

    public void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (rb != null)
        {
            rb.freezeRotation = false;

            rb.constraints =
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;

            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;
        }
    }

    public void FixedUpdate()
    {
        if (player == null || rb == null)
            return;

        Vector3 direction =
            player.position - transform.position;

        direction.y = 0;

        // =========================
        // 一直面向玩家
        // =========================

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

            rb.MoveRotation(newRotation);
        }

        // =========================
        // 怪物互相分開
        // =========================

        Vector3 separation = Vector3.zero;

        Collider[] nearbyEnemies =
            Physics.OverlapSphere(
                transform.position,
                separationRadius
            );

        foreach (Collider col in nearbyEnemies)
        {
            if (col.gameObject == gameObject)
                continue;

            EnemyFollowPlayer otherEnemy =
                col.GetComponentInParent<EnemyFollowPlayer>();

            if (otherEnemy == null)
                continue;

            if (otherEnemy == this)
                continue;

            Vector3 away =
                transform.position -
                otherEnemy.transform.position;

            away.y = 0;

            float distance = away.magnitude;

            if (distance > 0.01f)
            {
                separation +=
                    away.normalized /
                    distance;
            }
        }

        // =========================
        // 追玩家
        // =========================

        Vector3 moveDirection = Vector3.zero;

        if (direction.sqrMagnitude >
            stopDistance * stopDistance)
        {
            moveDirection = direction.normalized;
        }

        // 加入互相推開效果
        moveDirection +=
            separation * separationStrength;

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        Vector3 targetPosition =
            rb.position +
            moveDirection *
            moveSpeed *
            Time.fixedDeltaTime;

        rb.MovePosition(targetPosition);
    }
}