using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
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

    [Header("Attack Touch Area (0 - 1 Screen)")]
    public bool useAttackTouchArea = true;

    [Range(0f, 1f)]
    [InspectorName("左邊界 (Left)")]
    public float attackAreaLeft = 0.5f;

    [Range(0f, 1f)]
    [InspectorName("右邊界 (Right)")]
    public float attackAreaRight = 1f;

    [Range(0f, 1f)]
    [InspectorName("下邊界 (Bottom)")]
    public float attackAreaBottom = 0f;

    [Range(0f, 1f)]
    [InspectorName("上邊界 (Top)")]
    public float attackAreaTop = 1f;

    public float nextFireTime;
    public EventTrigger attackEventTrigger;
    public EventTrigger.Entry attackPointerDownEntry;
    public EventTrigger.Entry attackPointerUpEntry;
    public EventTrigger.Entry attackPointerExitEntry;
    public PlayerStats playerStats;

    public void Start()
    {
        playerStats = GetComponent<PlayerStats>();

        SetupHoldAttack();
    }

    public void Update()
    {
        isHoldingAttack = buttonHoldingAttack || IsAttackAreaPressed();

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

        attackPointerExitEntry = CreateTriggerEntry(
            EventTriggerType.PointerExit,
            OnAttackPointerUp
        );
        attackEventTrigger.triggers.Add(attackPointerExitEntry);
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
        buttonHoldingAttack = true;
        Fire();
    }

    public void OnAttackPointerUp(BaseEventData eventData)
    {
        buttonHoldingAttack = false;
    }

    public bool IsAttackAreaPressed()
    {
        if (Time.timeScale <= 0f ||
            !useAttackTouchArea || attackButton == null ||
            !attackButton.gameObject.activeInHierarchy ||
            !attackButton.interactable)
            return false;

        if (Touchscreen.current != null)
        {
            foreach (TouchControl touch in Touchscreen.current.touches)
            {
                if (touch.press.isPressed &&
                    IsInsideAttackArea(touch.position.ReadValue()))
                    return true;
            }
        }

        return Mouse.current != null &&
            Mouse.current.leftButton.isPressed &&
            IsInsideAttackArea(Mouse.current.position.ReadValue());
    }

    public bool IsInsideAttackArea(Vector2 screenPosition)
    {
        if (Screen.width <= 0 || Screen.height <= 0)
            return false;

        float x = screenPosition.x / Screen.width;
        float y = screenPosition.y / Screen.height;
        float left = Mathf.Min(attackAreaLeft, attackAreaRight);
        float right = Mathf.Max(attackAreaLeft, attackAreaRight);
        float bottom = Mathf.Min(attackAreaBottom, attackAreaTop);
        float top = Mathf.Max(attackAreaBottom, attackAreaTop);

        return x >= left && x <= right && y >= bottom && y <= top;
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
        attackEventTrigger.triggers.Remove(attackPointerExitEntry);
    }

    public void Fire()
    {
        if (Time.time < nextFireTime)
            return;

        if (playerStats != null && playerStats.isDead)
            return;

        nextFireTime = Time.time + fireCooldown;

        Vector3 direction = transform.forward;

        if (aimCamera != null)
        {
            Ray aimRay = aimCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f)
            );

            direction = aimRay.direction.normalized;
        }

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

        projectileBody.useGravity = false;
        projectileBody.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;
        projectileBody.linearVelocity =
            direction * projectileSpeed;

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
                health = enemy.gameObject.AddComponent<EnemyHealth>();

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
