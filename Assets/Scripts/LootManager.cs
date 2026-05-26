using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LootManager : MonoBehaviour
{
    public static LootManager Instance;

    [Header("Coins")]
    public int coins = 0;
    public TextMeshProUGUI coinsText;

    [Header("Loot prefabs")]
    public GameObject healthPotionPrefab;
    public GameObject manaPotionPrefab;
    public GameObject chestPrefab;
    public GameObject lockedChestPrefab;
    public GameObject coinPrefab;

    [Header("Spawn settings")]
    public int coinsPerRoom = 3;
    public float lootSpawnChance = 0.7f;
    public int absoluteMaxLoot = 25;

    [Header("References")]
    public MapGenerator mapGenerator;

    private List<GameObject> spawnedLoot =
        new List<GameObject>();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (coinsText != null)
            coinsText.text = $"Coins: {coins}";
    }

    public GameObject SpawnLootForClass(
        Vector3 position, string className,
        AbilityData[] abilities)
    {
        if (Random.value > lootSpawnChance) return null;

        GameObject prefab = GetLootForClass(
            className, abilities);

        if (prefab == null) return null;

        return Instantiate(prefab,
            position + GetRandomOffset(),
            Quaternion.identity);
    }

    GameObject GetLootForClass(
        string className, AbilityData[] abilities)
    {
        float magicWeight = 0f;
        float meleeWeight = 0f;

        if (abilities != null && abilities.Length > 0)
            foreach (var ab in abilities)
            {
                magicWeight += ab.magicWeight;
                meleeWeight += ab.meleeWeight;
            }
        else
        {
            int roll = Random.Range(0, 3);
            if (roll == 0) return healthPotionPrefab;
            if (roll == 1) return coinPrefab;
            return chestPrefab;
        }

        if (magicWeight > meleeWeight)
        {
            int roll = Random.Range(0, 3);
            if (roll == 0) return manaPotionPrefab;
            if (roll == 1) return healthPotionPrefab;
            return coinPrefab;
        }
        else
        {
            int roll = Random.Range(0, 3);
            if (roll == 0) return healthPotionPrefab;
            if (roll == 1) return chestPrefab;
            return coinPrefab;
        }
    }

    public void SpawnLootOnMap(
        int[,] map, int width, int height)
    {
        ClearAllLoot();

        if (mapGenerator == null ||
            mapGenerator.tilemapFloor == null)
        {
            Debug.LogWarning("LootManager is missing MapGenerator or floor Tilemap.");
            return;
        }

        string className = GameManager.Instance != null
            ? GameManager.Instance.selectedClass
            : "Warrior";

        AbilityData[] abilities =
            GameManager.Instance != null
            ? GameManager.Instance
                .selectedAbilities.ToArray()
            : null;

        int maxLoot = 15;
        if (mapGenerator != null &&
            mapGenerator.currentBuild != null)
        {
            float difficultyLootModifier =
                DifficultyManager.Instance != null
                ? DifficultyManager.Instance.GetLootModifier()
                : 1f;

            maxLoot = Mathf.Max(
                1,
                Mathf.RoundToInt(
                    (8 + mapGenerator.currentBuild.lootDensity * 24) *
                    mapGenerator.adaptiveLootMultiplier *
                    difficultyLootModifier));
        }

        maxLoot = Mathf.Clamp(maxLoot, 0, absoluteMaxLoot);

        List<Vector3Int> candidates = new List<Vector3Int>();

        for (int x = 2; x < width - 2; x++)
        {
            for (int y = 2; y < height - 2; y++)
            {
                if (map[x, y] != 0) continue;

                bool hasWallNeighbour =
                    map[x + 1, y] == 1 || map[x - 1, y] == 1 ||
                    map[x, y + 1] == 1 || map[x, y - 1] == 1;

                if (hasWallNeighbour) continue;

                candidates.Add(new Vector3Int(x, y, 0));
            }
        }

        Shuffle(candidates);

        int spawned = 0;
        foreach (Vector3Int cell in candidates)
        {
            if (spawned >= maxLoot) break;

            Vector3 pos = mapGenerator.tilemapFloor
                .GetCellCenterWorld(cell);
            pos.z = 0;

            GameObject loot = SpawnLootForClass(
                pos, className, abilities);

            if (loot == null) continue;

            spawnedLoot.Add(loot);

            SpriteRenderer sr = loot.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingOrder = 2;
            spawned++;
        }

        Debug.Log($"Spawned loot: {spawned}/{maxLoot}");
    }

    public void ClearAllLoot()
    {
        foreach (var loot in spawnedLoot)
            if (loot != null)
                Destroy(loot);
        spawnedLoot.Clear();
    }

    Vector3 GetRandomOffset()
    {
        return new Vector3(
            Random.Range(-0.3f, 0.3f),
            Random.Range(-0.3f, 0.3f),
            0f);
    }

    void Shuffle(List<Vector3Int> cells)
    {
        for (int i = cells.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector3Int tmp = cells[i];
            cells[i] = cells[j];
            cells[j] = tmp;
        }
    }
}
