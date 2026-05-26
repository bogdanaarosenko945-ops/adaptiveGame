using UnityEngine;

public class Arrow : MonoBehaviour
{
    public int damage = 10;
    public Vector2 direction;
    public float speed = 8f;
    public float lifetime = 3f;
    public float explosionRadius = 0f;
    public float explosionDamageMultiplier = 0.65f;
    public float freezeDuration = 0f;
    public float freezeSpeedMultiplier = 0.35f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction * speed;
        Destroy(gameObject, lifetime);

        float angle = Mathf.Atan2(
            direction.y, direction.x)
            * Mathf.Rad2Deg;
        transform.rotation =
            Quaternion.Euler(0, 0, angle);

        SpriteRenderer sr =
            GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 10;
        }
    }
    public GameObject owner; // хто випустив стрілу

    void OnTriggerEnter2D(Collider2D col)
    {
        // Ігноруємо власника стріли
        if (owner != null &&
            col.gameObject == owner) return;

        if (col.CompareTag("Player"))
        {
            PlayerHealth ph =
                col.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (col.CompareTag("Enemy"))
        {
            EnemyBase enemy =
                col.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                ApplyStatusAndAreaDamage(enemy);
            }
            Destroy(gameObject);
        }
        else if (!col.isTrigger)
        {
            Destroy(gameObject);
        }
    }

    void ApplyStatusAndAreaDamage(EnemyBase directHit)
    {
        if (freezeDuration > 0f && directHit != null)
            directHit.ApplySlow(
                freezeSpeedMultiplier,
                freezeDuration);

        if (explosionRadius <= 0f) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position, 
            explosionRadius);

        foreach (Collider2D hit in hits)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy == null || enemy == directHit) continue;

            int explosionDamage = Mathf.Max(
                1,
                Mathf.RoundToInt(
                    damage * explosionDamageMultiplier));
            enemy.TakeDamage(explosionDamage);

            if (freezeDuration > 0f)
                enemy.ApplySlow(
                    freezeSpeedMultiplier,
                    freezeDuration);
        }
    }
}
