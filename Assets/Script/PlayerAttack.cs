using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerAttack : MonoBehaviour
{
    public Button attackButton;
    public Camera aimCamera;

    public float projectileSpeed = 18f;
    public float projectileDamage = 25f;
    public float projectileSize = 0.28f;
    public float projectileLifetime = 4f;
    public float fireCooldown = 0.35f;
    public Color fireColor = new Color(1f, 0.24f, 0.02f, 1f);

    [Header("Fireball Spawn Position")]
    [InspectorName("左右位置 (X)")]
    public float fireballLeftRight = 0f;

    [InspectorName("上下位置 (Y)")]
    public float fireballUpDown = 0.65f;

    [InspectorName("前後位置 (Z)")]
    public float fireballForwardBack = 0.45f;

    [Header("Hold Attack")]
    public bool isHoldingAttack;
    public bool buttonHoldingAttack;

    public float nextFireTime;
    public EventTrigger attackEventTrigger;
    public EventTrigger.Entry attackPointerDownEntry;
    public EventTrigger.Entry attackPointerUpEntry;
    public PlayerStats playerStats;

    public void Start()
    {
        playerStats = GetComponent<PlayerStats>();

        SetupHoldAttack();
    }

    public void Update()
    {
        if (Time.timeScale <= 0f)
        {
            isHoldingAttack = false;
            buttonHoldingAttack = false;
            return;
        }

        isHoldingAttack = buttonHoldingAttack;

        if (isHoldingAttack)
            Fire();

        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Fire();
        }
    }

    public void SetupHoldAttack()
    {
        if (attackButton == null)
            return;

        attackEventTrigger = attackButton.GetComponent<EventTrigger>();

        if (attackEventTrigger == null)
            attackEventTrigger = attackButton.gameObject.AddComponent<EventTrigger>();

        if (attackEventTrigger.triggers == null)
            attackEventTrigger.triggers = new List<EventTrigger.Entry>();

        attackPointerDownEntry = CreateTriggerEntry(
            EventTriggerType.PointerDown,
            OnAttackPointerDown
        );
        attackEventTrigger.triggers.Add(attackPointerDownEntry);

        attackPointerUpEntry = CreateTriggerEntry(
            EventTriggerType.PointerUp,
            OnAttackPointerUp
        );
        attackEventTrigger.triggers.Add(attackPointerUpEntry);
    }

    public EventTrigger.Entry CreateTriggerEntry(
        EventTriggerType eventType,
        UnityEngine.Events.UnityAction<BaseEventData> action
    )
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = eventType;
        entry.callback.AddListener(action);
        return entry;
    }

    public void OnAttackPointerDown(BaseEventData eventData)
    {
        if (Time.timeScale <= 0f)
            return;

        buttonHoldingAttack = true;
        Fire();
    }

    public void OnAttackPointerUp(BaseEventData eventData)
    {
        buttonHoldingAttack = false;
    }

    public void OnDisable()
    {
        isHoldingAttack = false;
        buttonHoldingAttack = false;
    }

    public void OnDestroy()
    {
        isHoldingAttack = false;
        buttonHoldingAttack = false;

        if (attackEventTrigger == null || attackEventTrigger.triggers == null)
            return;

        attackEventTrigger.triggers.Remove(attackPointerDownEntry);
        attackEventTrigger.triggers.Remove(attackPointerUpEntry);
    }

    public void Fire()
    {
        if (Time.time < nextFireTime)
            return;

        if (playerStats != null && playerStats.isDead)
            return;

        nextFireTime = Time.time + fireCooldown;

        Vector3 spawnPosition =
            transform.position +
            transform.TransformDirection(
                new Vector3(
                    fireballLeftRight,
                    fireballUpDown,
                    fireballForwardBack
                )
            );

        GameObject projectile = GameObject.CreatePrimitive(
            PrimitiveType.Sphere
        );

        projectile.name = "FireMagicBall";
        projectile.transform.position = spawnPosition;
        projectile.transform.localScale =
            Vector3.one * projectileSize;

        SphereCollider projectileCollider =
            projectile.GetComponent<SphereCollider>();

        projectileCollider.isTrigger = true;

        Rigidbody projectileBody =
            projectile.AddComponent<Rigidbody>();

        LaunchProjectile(projectileBody);

        MagicProjectile magicProjectile =
            projectile.AddComponent<MagicProjectile>();

        magicProjectile.owner = gameObject;
        magicProjectile.damage = playerStats != null
            ? playerStats.attack
            : projectileDamage;
        magicProjectile.lifeTime = projectileLifetime;
        magicProjectile.fireColor = fireColor;
        magicProjectile.BuildFireLook();

        if (MusicManager.instance != null)
            MusicManager.instance.PlayFireSfx();
    }

    public Vector3 GetFireDirection()
    {
        Vector3 direction = aimCamera != null
            ? aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)).direction
            : transform.forward;
        direction.y = 0f;
        // Looking straight up/down has no horizontal aim; use player facing.
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.forward;
        return direction.normalized;
    }

    public void LaunchProjectile(Rigidbody projectileBody)
    {
        projectileBody.useGravity = false;
        projectileBody.constraints |= RigidbodyConstraints.FreezePositionY;
        projectileBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        projectileBody.linearVelocity = GetFireDirection() * projectileSpeed;
    }

}

// Fireballs are created only by PlayerAttack at runtime, so keeping their
// short-lived behaviour beside the firing code avoids a separate script asset.
public class MagicProjectile : MonoBehaviour
{
    public GameObject owner;
    public float damage = 25f;
    public float lifeTime = 4f;
    public Color fireColor = new Color(1f, 0.24f, 0.02f, 1f);
    public bool hasHit;

    public void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void BuildFireLook()
    {
        Renderer projectileRenderer = GetComponent<Renderer>();
        if (projectileRenderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material material = new Material(shader);
            material.color = fireColor;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", fireColor * 3f);
            }
            projectileRenderer.material = material;
        }

        Light fireLight = gameObject.AddComponent<Light>();
        fireLight.color = fireColor;
        fireLight.range = 2.5f;
        fireLight.intensity = 2f;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (hasHit || other.gameObject == owner ||
            other.transform.IsChildOf(owner.transform))
            return;

        EnemyFollowPlayer enemy = other.GetComponentInParent<EnemyFollowPlayer>();
        if (enemy != null)
        {
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health == null)
                return;

            hasHit = true;
            health.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }
}
