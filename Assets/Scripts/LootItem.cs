using UnityEngine;

public enum LootType
{
    HealthPotion,
    ManaPotion,
    Chest,
    LockedChest,
    Coin
}

public class LootItem : MonoBehaviour
{
    [Header("Loot type")]
    public LootType lootType;
    public int value = 1;

    [Header("Chest")]
    public bool isLocked = false;
    public int requiredKills = 3;
    private int killsInZone = 0;

    private bool isCollected = false;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        UpdateVisual();
    }

    public void OnEnemyKilledNearby()
    {
        if (!isLocked) return;

        killsInZone++;
        if (killsInZone >= requiredKills)
        {
            isLocked = false;
            UpdateVisual();
            Debug.Log("Locked chest opened.");
        }
    }

    void UpdateVisual()
    {
        if (sr == null) return;

        switch (lootType)
        {
            case LootType.HealthPotion:
                sr.color = Color.red;
                break;
            case LootType.ManaPotion:
                sr.color = Color.blue;
                break;
            case LootType.Coin:
                sr.color = Color.yellow;
                break;
            case LootType.Chest:
                sr.color = new Color(0.6f, 0.4f, 0.2f);
                break;
            case LootType.LockedChest:
                sr.color = isLocked
                    ? Color.grey
                    : new Color(1f, 0.8f, 0f);
                break;
        }
    }

    public void TryCollect(PlayerHealth player)
    {
        if (isCollected) return;

        if (isLocked)
        {
            Debug.Log(
                "Chest is locked. Remaining nearby kills: " +
                Mathf.Max(0, requiredKills - killsInZone));
            return;
        }

        isCollected = true;
        ApplyEffect(player);
        Destroy(gameObject);
    }

    void ApplyEffect(PlayerHealth player)
    {
        switch (lootType)
        {
            case LootType.HealthPotion:
                player.Heal(20);
                Debug.Log("+20 HP");
                if (PlayerTracker.Instance != null)
                    PlayerTracker.Instance
                        .RegisterLootPickup("hp");
                break;

            case LootType.ManaPotion:
                int manaAmount = GetPotionAmount(player, 20);
                HPBar hpBar = Object.FindAnyObjectByType<HPBar>();
                if (hpBar != null)
                    hpBar.RestoreMana(manaAmount);
                Debug.Log($"+{manaAmount} Mana");
                if (PlayerTracker.Instance != null)
                    PlayerTracker.Instance
                        .RegisterLootPickup("mana");
                break;

            case LootType.Coin:
                LootManager.Instance.AddCoins(value);
                Debug.Log($"+{value} coins");
                if (PlayerTracker.Instance != null)
                    PlayerTracker.Instance
                        .RegisterLootPickup("coin");
                break;

            case LootType.Chest:
            case LootType.LockedChest:
                OpenChest(player);
                break;
        }
    }

    void OpenChest(PlayerHealth player)
    {
        int roll = Random.Range(0, 3);

        switch (roll)
        {
            case 0:
                player.Heal(30);
                Debug.Log("Chest: +30 HP.");
                break;
            case 1:
                LootManager.Instance.AddCoins(10);
                Debug.Log("Chest: +10 coins.");
                break;
            case 2:
                Debug.Log("Chest: rare item.");
                break;
        }
    }

    int GetPotionAmount(PlayerHealth player, int baseAmount)
    {
        PlayerAbilityController abilities =
            player != null
            ? player.GetComponent<PlayerAbilityController>()
            : null;

        float multiplier = abilities != null
            ? abilities.healMultiplier
            : 1f;

        return Mathf.Max(
            1,
            Mathf.RoundToInt(baseAmount * multiplier));
    }
}
