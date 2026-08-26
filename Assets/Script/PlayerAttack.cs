using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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
    public float nextFireTime;

    public void Start()
    {
        if (aimCamera == null)
            aimCamera = Camera.main;

        if (attackButton == null)
        {
            Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();

            foreach (Button button in buttons)
            {
                if (button.gameObject.scene.IsValid() &&
                    button.name == "attackButton")
                {
                    attackButton = button;
                    break;
                }
            }
        }

        if (attackButton != null)
            attackButton.onClick.AddListener(Fire);
    }

    public void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Fire();
        }
    }

    public void OnDestroy()
    {
        if (attackButton != null)
            attackButton.onClick.RemoveListener(Fire);
    }

    public void Fire()
    {
        if (Time.time < nextFireTime)
            return;

        PlayerStats stats = GetComponent<PlayerStats>();

        if (stats != null && stats.isDead)
            return;

        nextFireTime = Time.time + fireCooldown;
        PlayerController playerController = GetComponent<PlayerController>();

        if (playerController != null)
            playerController.MagicHeal();

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
        magicProjectile.damage = projectileDamage;
        magicProjectile.lifeTime = projectileLifetime;
        magicProjectile.fireColor = fireColor;
        magicProjectile.BuildFireLook();
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.AfterSceneLoad
    )]
    public static void AddAttackToPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null && player.GetComponent<PlayerAttack>() == null)
            player.AddComponent<PlayerAttack>();
    }
}
