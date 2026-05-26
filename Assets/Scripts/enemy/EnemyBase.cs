using UnityEngine;

public enum EnemyType { Melee, Ranged, Tank }

public class EnemyBase : MonoBehaviour
{
    [Header("Тип ворога")]
    public EnemyType enemyType;

    [Header("Характеристики")]
    public int maxHealth = 30;
    public int currentHealth;
    public int damage = 10;
    public float moveSpeed = 2f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    public float detectionRange = 8f;

    [Header("Лут після смерті")]
    public GameObject[] lootDrops;
    [Range(0f, 1f)]
    public float lootDropChance = 0.4f;

    protected Transform player;
    protected Rigidbody2D rb;
    protected float lastAttackTime;
    protected bool isDead = false;
    protected float speedMultiplier = 1f;
    protected PlayerAbilityController playerAbilities;
    private Coroutine slowCoroutine;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Start()
    {
        GameObject p =
            GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
        if (p != null)
            playerAbilities =
                p.GetComponent<PlayerAbilityController>();

        SpriteRenderer sr =
            GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sortingOrder = 2;
    }

    public virtual void TakeDamage(int dmg)
    {
        if (isDead) return;
        currentHealth -= dmg;
        Debug.Log($"{gameObject.name} отримав " +
                  $"{dmg} шкоди. HP: {currentHealth}");
        if (currentHealth <= 0) Die();
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        if (PlayerTracker.Instance != null)
            PlayerTracker.Instance.RegisterKill(
                enemyType == EnemyType.Ranged
                ? "ranged" : "melee");

        if (Random.value < lootDropChance &&
            lootDrops != null &&
            lootDrops.Length > 0)
        {
            GameObject drop = lootDrops[
                Random.Range(0, lootDrops.Length)];
            if (drop != null)
                Instantiate(drop,
                    transform.position,
                    Quaternion.identity);
        }

        Collider2D[] nearby =
            Physics2D.OverlapCircleAll(
                transform.position, 5f);
        foreach (var col in nearby)
        {
            LootItem loot =
                col.GetComponent<LootItem>();
            if (loot != null)
                loot.OnEnemyKilledNearby();
        }

        if (EnemySpawner.Instance != null)
            EnemySpawner.Instance.NotifyEnemyKilled(gameObject);

        Destroy(gameObject);
    }

    protected float DistanceToPlayer()
    {
        if (player == null) return 999f;
        return Vector2.Distance(
            transform.position, player.position);
    }

    protected Vector2 DirectionToPlayer()
    {
        if (player == null) return Vector2.zero;
        return (player.position -
            transform.position).normalized;
    }

    public float GetEffectiveMoveSpeed()
    {
        return moveSpeed * speedMultiplier;
    }

    public void ApplySlow(
        float multiplier,
        float duration)
    {
        if (slowCoroutine != null)
            StopCoroutine(slowCoroutine);

        slowCoroutine = StartCoroutine(
            SlowRoutine(multiplier, duration));
    }

    System.Collections.IEnumerator SlowRoutine(
        float multiplier,
        float duration)
    {
        speedMultiplier = Mathf.Clamp(multiplier, 0.05f, 1f);

        SpriteRenderer sr =
            GetComponent<SpriteRenderer>();
        Color originalColor = sr != null
            ? sr.color
            : Color.white;

        if (sr != null)
            sr.color = Color.cyan;

        yield return new WaitForSeconds(duration);

        speedMultiplier = 1f;
        if (sr != null)
            sr.color = originalColor;
        slowCoroutine = null;
    }

    protected bool CanDetectPlayer()
    {
        if (player == null) return false;
        if (playerAbilities == null)
            playerAbilities =
                player.GetComponent<PlayerAbilityController>();

        if (playerAbilities != null &&
            playerAbilities.isInvisible &&
            DistanceToPlayer() > attackRange)
            return false;

        return true;
    }
}
