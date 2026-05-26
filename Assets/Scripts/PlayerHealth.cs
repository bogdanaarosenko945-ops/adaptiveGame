using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    private PlayerMovement movement;
    private PlayerAbilityController abilities;

    void Start()
    {
        currentHealth = maxHealth;
        movement = GetComponent<PlayerMovement>();
        abilities = GetComponent<PlayerAbilityController>();
    }

    public void ApplyAbilityStats(PlayerAbilityController abilityController)
    {
        abilities = abilityController;
    }

    public void TakeDamage(int damage)
    {
        float damageMultiplier = abilities != null
            ? abilities.damageTakenMultiplier
            : 1f;
        int finalDamage = Mathf.Max(
            1,
            Mathf.RoundToInt(damage * damageMultiplier));

        currentHealth -= finalDamage;
        Debug.Log($"HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        currentHealth = maxHealth;
        bool handledByTracker = false;

        if (movement != null)
        {
            movement.OnDeath();
            handledByTracker = movement.tracker != null;
        }

        if (!handledByTracker &&
            GameManager.Instance != null)
            GameManager.Instance.OnRunComplete();
    }

    public void Heal(int amount)
    {
        float healMultiplier = abilities != null
            ? abilities.healMultiplier
            : 1f;
        int finalAmount = Mathf.Max(
            1,
            Mathf.RoundToInt(amount * healMultiplier));

        currentHealth = Mathf.Min(
            currentHealth + finalAmount, maxHealth);
        Debug.Log($"HP: {currentHealth}/{maxHealth}");
    }
}
