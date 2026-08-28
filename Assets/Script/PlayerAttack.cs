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
        isHoldingAttack = true;
        Fire();
    }

    public void OnAttackPointerUp(BaseEventData eventData)
    {
        isHoldingAttack = false;
    }

    public void OnDisable()
    {
        isHoldingAttack = false;
    }

    public void OnDestroy()
    {
        isHoldingAttack = false;

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
