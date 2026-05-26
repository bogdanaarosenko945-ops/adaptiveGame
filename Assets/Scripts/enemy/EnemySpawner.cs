using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;

    [Header("References")]
    public MapGenerator mapGenerator;

    [Header("Enemy prefabs")]
    public GameObject meleePrefab;
    public GameObject rangedPrefab;
    public GameObject tankPrefab;

    [Header("Spawn settings")]
    public int maxEnemies = 10;
    public int absoluteMaxEnemies = 18;
    public float minDistanceFromPlayer = 5f;

    private List<GameObject> spawnedEnemies
        = new List<GameObject>();
    private bool levelCompletionPending;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ClearAllEnemies()
    {
        levelCompletionPending = false;
        foreach (var e in spawnedEnemies)
            if (e != null) Destroy(e);
        spawnedEnemies.Clear();
    }

    public void SpawnEnemiesOnMap(
        int[,] map, int width, int height,
        Vector3 playerSpawn)
    {
        ClearAllEnemies();

        if (mapGenerator == null ||
            mapGenerator.tilemapFloor == null)
        {
            Debug.LogWarning("EnemySpawner is missing MapGenerator or floor Tilemap.");
            return;
        }

        string className =
            GameManager.Instance != null
            ? GameManager.Instance.selectedClass
            : "Warrior";

        var abilities =
            GameManager.Instance != null
            ? GameManager.Instance.selectedAbilities
            : null;

        GetEnemyWeights(className, abilities,
            out float meleeW,
            out float rangedW,
            out float tankW);

        rangedW += mapGenerator.adaptiveRangedEnemyBias;
        tankW += mapGenerator.adaptiveTankEnemyBias;
        meleeW = Mathf.Max(0.05f, meleeW);
        rangedW = Mathf.Max(0.05f, rangedW);
        tankW = Mathf.Max(0.05f, tankW);

        float enemyModifier = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.GetEnemyModifier()
            : 1f;
        int targetEnemies = Mathf.Max(
            0,
            Mathf.RoundToInt(
                maxEnemies *
                enemyModifier *
                mapGenerator.adaptiveEnemyMultiplier));
        targetEnemies = Mathf.Clamp(
            targetEnemies,
            0,
            absoluteMaxEnemies);

        List<Vector3Int> candidates = new List<Vector3Int>();

        for (int x = 4; x < width - 4; x++)
        {
            for (int y = 4; y < height - 4; y++)
            {
                if (map[x, y] != 0) continue;

                Vector3 pos = mapGenerator.tilemapFloor
                    .GetCellCenterWorld(
                        new Vector3Int(x, y, 0));
                pos.z = 0;

                if (Vector3.Distance(pos, playerSpawn)
                    < minDistanceFromPlayer) continue;

                candidates.Add(new Vector3Int(x, y, 0));
            }
        }

        Shuffle(candidates);

        int spawned = 0;
        foreach (Vector3Int cell in candidates)
        {
            if (spawned >= targetEnemies) break;

            GameObject prefab = GetRandomEnemy(
                meleeW, rangedW, tankW);
            if (prefab == null) continue;

            Vector3 pos = mapGenerator.tilemapFloor
                .GetCellCenterWorld(cell);
            pos.z = 0;

            GameObject enemy = Instantiate(
                prefab, pos, Quaternion.identity);

            enemy.layer =
                LayerMask.NameToLayer("Enemy");
            enemy.tag = "Enemy";

            SpriteRenderer sr =
                enemy.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingOrder = 2;

            spawnedEnemies.Add(enemy);
            spawned++;
        }

        levelCompletionPending = false;
        Debug.Log($"Spawned enemies: {spawned}/{targetEnemies}");
    }

    public void NotifyEnemyKilled(GameObject enemy)
    {
        spawnedEnemies.Remove(enemy);
        spawnedEnemies.RemoveAll(e => e == null);

        if (levelCompletionPending)
            return;

        if (spawnedEnemies.Count > 0)
            return;

        levelCompletionPending = true;
        StartCoroutine(CompleteLevelAfterDelay());
    }

    IEnumerator CompleteLevelAfterDelay()
    {
        yield return new WaitForSeconds(0.45f);

        levelCompletionPending = false;

        if (GameManager.Instance != null)
            GameManager.Instance.OnRunComplete();
    }

    void GetEnemyWeights(
        string className,
        List<AbilityData> abilities,
        out float meleeW,
        out float rangedW,
        out float tankW)
    {
        meleeW = 0.4f;
        rangedW = 0.4f;
        tankW = 0.2f;

        if (abilities == null) return;

        float magicTotal = 0f;
        float meleeTotal = 0f;

        foreach (var ab in abilities)
        {
            magicTotal += ab.magicWeight;
            meleeTotal += ab.meleeWeight;
        }

        if (magicTotal > meleeTotal)
        {
            meleeW = 0.2f;
            rangedW = 0.6f;
            tankW = 0.2f;
        }
        else if (meleeTotal > magicTotal)
        {
            meleeW = 0.5f;
            rangedW = 0.2f;
            tankW = 0.3f;
        }
    }

    GameObject GetRandomEnemy(
        float meleeW, float rangedW, float tankW)
    {
        float total = meleeW + rangedW + tankW;
        float roll = Random.value * total;

        if (roll < meleeW)
            return meleePrefab;
        else if (roll < meleeW + rangedW)
            return rangedPrefab;
        else
            return tankPrefab;
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
