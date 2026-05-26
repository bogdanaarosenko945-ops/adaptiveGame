using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [Header("Melee attack")]
    public int meleeDamage = 20;
    public float meleeRange = 1.5f;
    public float meleeCooldown = 0.5f;

    [Header("Ranged attack")]
    public GameObject projectilePrefab;
    public GameObject magicProjectilePrefab;
    public int rangedDamage = 15;
    public float rangedCooldown = 1f;

    [Header("References")]
    public PlayerTracker tracker;

    private float lastMeleeTime;
    private float lastRangedTime;
    private string playerClass = "Warrior";
    private Camera cam;
    private PlayerAbilityController abilities;

    void Start()
    {
        cam = Camera.main;
        abilities = GetComponent<PlayerAbilityController>();
        if (GameManager.Instance != null)
            playerClass =
                GameManager.Instance.selectedClass;
    }

    public void ApplyAbilityStats(PlayerAbilityController abilityController)
    {
        abilities = abilityController;
    }

    void Update()
    {
        if (GameManager.Instance != null)
            playerClass = GameManager.Instance.selectedClass;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (CanUseMelee())
                MeleeAttack();
            else
                Debug.Log("Melee attack is locked.");
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (CanUseRanged())
                RangedAttack();
            else
                Debug.Log("Ranged attack is locked.");
        }
    }

    void MeleeAttack()
    {
        if (Time.time - lastMeleeTime < meleeCooldown)
            return;
        lastMeleeTime = Time.time;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position, GetMeleeRange());

        bool hit = false;
        foreach (var col in hits)
        {
            EnemyBase enemy =
                col.GetComponent<EnemyBase>();
            if (enemy == null) continue;

            enemy.TakeDamage(GetMeleeDamage());
            hit = true;
        }

        Debug.Log(hit
            ? $"Melee attack: -{GetMeleeDamage()}"
            : "Miss.");
    }

    void RangedAttack()
    {
        if (Time.time - lastRangedTime < GetRangedCooldown())
            return;

        lastRangedTime = Time.time;

        GameObject prefabToUse = projectilePrefab;
        if (playerClass == "Mage" &&
            magicProjectilePrefab != null)
            prefabToUse = magicProjectilePrefab;

        if (prefabToUse == null)
        {
            Debug.Log("Projectile prefab is missing.");
            return;
        }

        Vector3 mousePos = cam.ScreenToWorldPoint(
            Mouse.current.position.ReadValue());
        mousePos.z = 0;
        Vector2 dir = (mousePos -
            transform.position).normalized;

        Vector3 spawnPos = transform.position +
            (Vector3)(dir * 0.8f);

        GameObject proj = Instantiate(
            prefabToUse,
            spawnPos,
            Quaternion.identity);

        SpriteRenderer sr =
            proj.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sortingOrder = 10;

        Arrow arrow = proj.GetComponent<Arrow>();
        if (arrow != null)
        {
            arrow.damage = rangedDamage;
            arrow.direction = dir;
            arrow.owner = gameObject;
            arrow.speed *= GetProjectileSpeedMultiplier();
            arrow.explosionRadius = HasFireball() ? 2.2f : 0f;
            arrow.explosionDamageMultiplier = 0.65f;
            arrow.freezeDuration = HasFreeze() ? 2.0f : 0f;
            arrow.freezeSpeedMultiplier = 0.35f;
            arrow.damage = GetRangedDamage();
        }

        Debug.Log($"Ranged attack: {dir}");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position, GetMeleeRange());
    }

    int GetMeleeDamage()
    {
        float multiplier = abilities != null
            ? abilities.meleeDamageMultiplier
            : 1f;
        return Mathf.Max(
            1,
            Mathf.RoundToInt(meleeDamage * multiplier));
    }

    float GetMeleeRange()
    {
        float bonus = abilities != null
            ? abilities.meleeRangeBonus
            : 0f;
        return meleeRange + bonus;
    }

    int GetRangedDamage()
    {
        float multiplier = abilities != null
            ? abilities.rangedDamageMultiplier
            : 1f;
        return Mathf.Max(
            1,
            Mathf.RoundToInt(rangedDamage * multiplier));
    }

    float GetRangedCooldown()
    {
        float multiplier = abilities != null
            ? abilities.rangedCooldownMultiplier
            : 1f;
        return rangedCooldown * multiplier;
    }

    float GetProjectileSpeedMultiplier()
    {
        return abilities != null
            ? abilities.projectileSpeedMultiplier
            : 1f;
    }

    bool HasFireball()
    {
        return abilities != null && abilities.hasFireball;
    }

    bool HasFreeze()
    {
        return abilities != null && abilities.hasFreeze;
    }

    bool CanUseMelee()
    {
        return playerClass == "Warrior" ||
               (abilities != null &&
                abilities.canUseMeleeAbility);
    }

    bool CanUseRanged()
    {
        return playerClass == "Mage" ||
               playerClass == "Rogue" ||
               (abilities != null &&
                abilities.canUseRangedAbility);
    }
}
