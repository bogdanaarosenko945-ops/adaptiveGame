using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum MapGenerationMethod
{
    CellularAutomata,
    BSP
}

public class MapGenerator : MonoBehaviour
{
    public MapGenerationMethod generationMethod =
        MapGenerationMethod.CellularAutomata;

    [Header("Білд")]
    public BuildConfig currentBuild;

    [Header("Tilemap і спрайти")]
    public Tilemap tilemapFloor;   // підлога
    public Tilemap tilemapWalls;   // стіни з колайдером
    public Sprite wallSprite;
    public Sprite floorSprite;

    [Header("Розмір карти")]
    public int minWidth = 60;
    public int minHeight = 60;
    public int maxWidth = 100;
    public int maxHeight = 100;

    [Header("Safety limits")]
    public int hardMaxWidth = 120;
    public int hardMaxHeight = 120;
    public int hardMaxCells = 10000;

    [Header("Visual style")]
    public Color floorTint = new Color(0.82f, 0.86f, 0.76f, 1f);
    public Color wallTint = new Color(0.58f, 0.54f, 0.62f, 1f);
    public Color waterTint = new Color(0.38f, 0.64f, 0.82f, 1f);

    private int[,] map;
    private Tile wallTile;
    private Tile floorTile;
    [HideInInspector] public int width;
    [HideInInspector] public int height;
    [HideInInspector] public int fillPercent;
    [HideInInspector] public int smoothIterations;
    [HideInInspector] public float adaptiveEnemyMultiplier = 1f;
    [HideInInspector] public float adaptiveLootMultiplier = 1f;
    [HideInInspector] public float adaptiveRangedEnemyBias = 0f;
    [HideInInspector] public float adaptiveTankEnemyBias = 0f;

    private bool hasAdaptiveMapParams;
    private int adaptiveFillPercent = 45;
    private int adaptiveSmoothIterations = 5;
    private int abilityFillOffset;
    private int abilitySmoothOffset;
    private int abilityWidthOffset;
    private int abilityHeightOffset;

    void Awake()
    {
        NormalizeMapLimits();

        if (PlayerPrefs.HasKey("MapGenerationMethod"))
            generationMethod =
                (MapGenerationMethod)PlayerPrefs.GetInt(
                    "MapGenerationMethod",
                    (int)MapGenerationMethod.CellularAutomata);
    }

    void OnValidate()
    {
        NormalizeMapLimits();
    }

    public void SetGenerationMethod(MapGenerationMethod method)
    {
        generationMethod = method;
        PlayerPrefs.SetInt(
            "MapGenerationMethod",
            (int)generationMethod);
        PlayerPrefs.Save();
        Debug.Log("Map generation method: " + generationMethod);
    }

    void Start()
    {
        wallTile = ScriptableObject.CreateInstance<Tile>();
        wallTile.sprite = wallSprite;
        floorTile = ScriptableObject.CreateInstance<Tile>();
        floorTile.sprite = floorSprite;
        ApplyVisualStyle();
        //GenerateMap();
    }

    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current
                .gKey.wasPressedThisFrame)
        {
            // Повідомляємо що забіг завершено
            // це покаже вибір абілки
            if (GameManager.Instance != null)
                GameManager.Instance.OnRunComplete();
            else
                GenerateMap();
        }
    }


    public void GenerateMap()
    {
        // 1. Очищення старого контенту
        if (LootManager.Instance != null)
            LootManager.Instance.ClearAllLoot();
        if (EnemySpawner.Instance != null)
            EnemySpawner.Instance.ClearAllEnemies();

        // 2. Визначаємо параметри з білда або ML
        ApplyRuntimeMapParams();

        // 3. Генерація карти
        if (generationMethod == MapGenerationMethod.BSP)
            GenerateBSPMap();
        else
            GenerateCellularMap();
        ForceWallBorders();
        DrawMap();
        GenerateLakes();
        SaveMetricsToCSV();

        // 4. Спавн гравця першим через корутину
        StartCoroutine(SpawnPlayerDelayed());

        // 5. Лут і вороги після генерації
        if (LootManager.Instance != null)
            LootManager.Instance.SpawnLootOnMap(
                map, width, height);

        if (EnemySpawner.Instance != null)
            EnemySpawner.Instance.SpawnEnemiesOnMap(
                map, width, height, GetSpawnPoint());
        Debug.Log($"Генерація: {width}x{height}, " +
          $"fill={fillPercent}, " +
          $"smooth={smoothIterations}");
    }

    IEnumerator SpawnPlayerDelayed()
    {
        // Чекаємо довше щоб колайдер повністю збудувався
        yield return new WaitForSeconds(0.5f);

        GameObject player =
            GetOrCreateSelectedPlayer();
        if (player == null) yield break;

        Rigidbody2D rb =
            player.GetComponent<Rigidbody2D>();

        // Повністю вимикаємо фізику
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        // Телепортуємо
        player.transform.position = GetSpawnPoint();

        if (GameManager.Instance != null)
            GameManager.Instance.ConfigurePlayer(player);

        // Чекаємо ще щоб все стабілізувалось
        yield return new WaitForSeconds(0.3f);

        // Вмикаємо фізику назад
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }
    }

    GameObject GetOrCreateSelectedPlayer()
    {
        GameObject player =
            GameObject.FindWithTag("Player");

        if (GameManager.Instance == null)
            return player;

        GameObject prefab =
            GameManager.Instance.GetSelectedPlayerPrefab();

        if (prefab == null)
            return player;

        string selectedClass =
            GameManager.Instance.selectedClass;

        if (player != null)
        {
            PlayerClassIdentity identity =
                player.GetComponent<PlayerClassIdentity>();

            if (identity != null &&
                identity.className == selectedClass)
                return player;

            Destroy(player);
        }

        Vector3 spawnPoint = GetSpawnPoint();
        GameObject spawned =
            Instantiate(prefab, spawnPoint, Quaternion.identity);
        spawned.tag = "Player";

        PlayerClassIdentity spawnedIdentity =
            spawned.GetComponent<PlayerClassIdentity>();
        if (spawnedIdentity == null)
            spawnedIdentity =
                spawned.AddComponent<PlayerClassIdentity>();

        spawnedIdentity.className = selectedClass;

        return spawned;
    }

    public void ApplyMLParams(
        int newFillPercent,
        int newSmooth,
        float enemyMultiplier = 1f,
        float lootMultiplier = 1f,
        float rangedEnemyBias = 0f,
        float tankEnemyBias = 0f,
        bool regenerateMap = true)
    {
        float diffMod = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.GetFillModifier()
            : 0f;

        float smoothMod = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.GetSmoothModifier()
            : 0f;

        // ML параметри + модифікатор складності
        adaptiveFillPercent = Mathf.Clamp(
            newFillPercent + (int)diffMod, 35, 58);
        adaptiveSmoothIterations = Mathf.Clamp(
            newSmooth + (int)smoothMod, 3, 9);
        hasAdaptiveMapParams = true;

        adaptiveEnemyMultiplier = Mathf.Clamp(
            enemyMultiplier, 0.65f, 1.45f);
        adaptiveLootMultiplier = Mathf.Clamp(
            lootMultiplier, 0.70f, 1.60f);
        adaptiveRangedEnemyBias = Mathf.Clamp(
            rangedEnemyBias, 0f, 0.45f);
        adaptiveTankEnemyBias = Mathf.Clamp(
            tankEnemyBias, 0f, 0.45f);

        ApplyRuntimeMapParams();

        Debug.Log($"ML + складність: " +
                  $"fill={fillPercent}, " +
                  $"smooth={smoothIterations}, " +
                  $"difficulty={DifficultyManager.Instance?.currentDifficulty}");

        Debug.Log(
            "ML adaptive content: " +
            $"enemyMultiplier={adaptiveEnemyMultiplier:F2}, " +
            $"lootMultiplier={adaptiveLootMultiplier:F2}, " +
            $"rangedBias={adaptiveRangedEnemyBias:F2}, " +
            $"tankBias={adaptiveTankEnemyBias:F2}");

        if (regenerateMap)
            GenerateMap();
    }

    public void SetAbilityMapModifiers(
        int fillOffset,
        int smoothOffset,
        int widthOffset,
        int heightOffset)
    {
        abilityFillOffset = fillOffset;
        abilitySmoothOffset = smoothOffset;
        abilityWidthOffset = widthOffset;
        abilityHeightOffset = heightOffset;
        ApplyRuntimeMapParams();
    }

    public void ResetAdaptiveMapParams()
    {
        hasAdaptiveMapParams = false;
        adaptiveEnemyMultiplier = 1f;
        adaptiveLootMultiplier = 1f;
        adaptiveRangedEnemyBias = 0f;
        adaptiveTankEnemyBias = 0f;
        SetAbilityMapModifiers(0, 0, 0, 0);
    }

    void ApplyRuntimeMapParams()
    {
        int baseWidth = currentBuild != null
            ? currentBuild.mapWidth
            : minWidth;
        int baseHeight = currentBuild != null
            ? currentBuild.mapHeight
            : minHeight;
        int baseFill = currentBuild != null
            ? currentBuild.fillPercent
            : 45;
        int baseSmooth = currentBuild != null
            ? currentBuild.smoothIterations
            : 5;

        if (hasAdaptiveMapParams)
        {
            baseFill = adaptiveFillPercent;
            baseSmooth = adaptiveSmoothIterations;
        }

        width = Mathf.Clamp(
            baseWidth + abilityWidthOffset,
            minWidth,
            maxWidth);
        height = Mathf.Clamp(
            baseHeight + abilityHeightOffset,
            minHeight,
            maxHeight);
        ClampMapArea();
        fillPercent = Mathf.Clamp(
            baseFill + abilityFillOffset,
            35,
            58);
        smoothIterations = Mathf.Clamp(
            baseSmooth + abilitySmoothOffset,
            3,
            9);
    }

    void NormalizeMapLimits()
    {
        hardMaxWidth = Mathf.Max(40, hardMaxWidth);
        hardMaxHeight = Mathf.Max(40, hardMaxHeight);
        hardMaxCells = Mathf.Max(1600, hardMaxCells);

        minWidth = Mathf.Clamp(minWidth, 30, hardMaxWidth);
        minHeight = Mathf.Clamp(minHeight, 30, hardMaxHeight);
        maxWidth = Mathf.Clamp(maxWidth, minWidth, hardMaxWidth);
        maxHeight = Mathf.Clamp(maxHeight, minHeight, hardMaxHeight);
    }

    void ClampMapArea()
    {
        int originalWidth = width;
        int originalHeight = height;

        width = Mathf.Clamp(width, minWidth, maxWidth);
        height = Mathf.Clamp(height, minHeight, maxHeight);

        while (width * height > hardMaxCells &&
               (width > minWidth || height > minHeight))
        {
            if (width >= height && width > minWidth)
                width--;
            else if (height > minHeight)
                height--;
            else
                break;
        }

        if (width != originalWidth || height != originalHeight)
        {
            Debug.LogWarning(
                "Map size clamped by safety limits: " +
                $"{originalWidth}x{originalHeight} -> {width}x{height}");
        }
    }

    void GenerateCellularMap()
    {
        map = new int[width, height];
        FillMapRandom();

        for (int i = 0; i < smoothIterations; i++)
            SmoothMap();

        FloodFill();
    }

    void GenerateBSPMap()
    {
        map = new int[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                map[x, y] = 1;

        List<RectInt> leaves = new List<RectInt>();
        List<RectInt> rooms = new List<RectInt>();
        RectInt root = new RectInt(2, 2, width - 4, height - 4);

        int splitDepth = Mathf.Clamp(
            4 + (fillPercent - 40) / 5,
            4,
            7);
        SplitBSP(root, splitDepth, leaves);

        int openness = Mathf.Clamp(58 - fillPercent, 4, 22);
        int corridorWidth = Mathf.Clamp(
            smoothIterations / 3,
            1,
            3);

        foreach (RectInt leaf in leaves)
        {
            int margin = 2;
            int minRoomSize = 5;

            if (leaf.width < minRoomSize + margin * 2 ||
                leaf.height < minRoomSize + margin * 2)
                continue;

            int maxRoomWidth = Mathf.Max(
                minRoomSize,
                leaf.width - margin * 2);
            int maxRoomHeight = Mathf.Max(
                minRoomSize,
                leaf.height - margin * 2);
            int roomWidth = UnityEngine.Random.Range(
                minRoomSize,
                Mathf.Min(maxRoomWidth, minRoomSize + openness) + 1);
            int roomHeight = UnityEngine.Random.Range(
                minRoomSize,
                Mathf.Min(maxRoomHeight, minRoomSize + openness) + 1);
            int roomX = UnityEngine.Random.Range(
                leaf.xMin + margin,
                leaf.xMax - roomWidth - margin + 1);
            int roomY = UnityEngine.Random.Range(
                leaf.yMin + margin,
                leaf.yMax - roomHeight - margin + 1);

            RectInt room = new RectInt(
                roomX,
                roomY,
                roomWidth,
                roomHeight);
            rooms.Add(room);
            CarveRoom(room);
        }

        for (int i = 1; i < rooms.Count; i++)
            ConnectRooms(
                CenterOf(rooms[i - 1]),
                CenterOf(rooms[i]),
                corridorWidth);

        if (rooms.Count == 0)
        {
            RectInt fallback = new RectInt(
                width / 2 - 4,
                height / 2 - 4,
                8,
                8);
            CarveRoom(fallback);
        }
    }

    void SplitBSP(
        RectInt area,
        int depth,
        List<RectInt> leaves)
    {
        if (depth <= 0 || area.width < 18 || area.height < 18)
        {
            leaves.Add(area);
            return;
        }

        bool splitVertical = area.width > area.height;
        if (area.width > area.height * 1.25f)
            splitVertical = true;
        else if (area.height > area.width * 1.25f)
            splitVertical = false;
        else
            splitVertical = UnityEngine.Random.value > 0.5f;

        if (splitVertical)
        {
            int min = area.xMin + area.width / 3;
            int max = area.xMax - area.width / 3;
            if (max <= min)
            {
                leaves.Add(area);
                return;
            }

            int split = UnityEngine.Random.Range(min, max);
            SplitBSP(
                new RectInt(
                    area.xMin,
                    area.yMin,
                    split - area.xMin,
                    area.height),
                depth - 1,
                leaves);
            SplitBSP(
                new RectInt(
                    split,
                    area.yMin,
                    area.xMax - split,
                    area.height),
                depth - 1,
                leaves);
        }
        else
        {
            int min = area.yMin + area.height / 3;
            int max = area.yMax - area.height / 3;
            if (max <= min)
            {
                leaves.Add(area);
                return;
            }

            int split = UnityEngine.Random.Range(min, max);
            SplitBSP(
                new RectInt(
                    area.xMin,
                    area.yMin,
                    area.width,
                    split - area.yMin),
                depth - 1,
                leaves);
            SplitBSP(
                new RectInt(
                    area.xMin,
                    split,
                    area.width,
                    area.yMax - split),
                depth - 1,
                leaves);
        }
    }

    void CarveRoom(RectInt room)
    {
        for (int x = room.xMin; x < room.xMax; x++)
            for (int y = room.yMin; y < room.yMax; y++)
                if (x > 0 && x < width - 1 &&
                    y > 0 && y < height - 1)
                    map[x, y] = 0;
    }

    void ConnectRooms(
        Vector2Int a,
        Vector2Int b,
        int corridorWidth)
    {
        if (UnityEngine.Random.value > 0.5f)
        {
            CarveHorizontal(a.x, b.x, a.y, corridorWidth);
            CarveVertical(a.y, b.y, b.x, corridorWidth);
        }
        else
        {
            CarveVertical(a.y, b.y, a.x, corridorWidth);
            CarveHorizontal(a.x, b.x, b.y, corridorWidth);
        }
    }

    void CarveHorizontal(
        int x1,
        int x2,
        int y,
        int corridorWidth)
    {
        int from = Mathf.Min(x1, x2);
        int to = Mathf.Max(x1, x2);

        for (int x = from; x <= to; x++)
            for (int offset = -corridorWidth; offset <= corridorWidth; offset++)
            {
                int cy = y + offset;
                if (x > 0 && x < width - 1 &&
                    cy > 0 && cy < height - 1)
                    map[x, cy] = 0;
            }
    }

    void CarveVertical(
        int y1,
        int y2,
        int x,
        int corridorWidth)
    {
        int from = Mathf.Min(y1, y2);
        int to = Mathf.Max(y1, y2);

        for (int y = from; y <= to; y++)
            for (int offset = -corridorWidth; offset <= corridorWidth; offset++)
            {
                int cx = x + offset;
                if (cx > 0 && cx < width - 1 &&
                    y > 0 && y < height - 1)
                    map[cx, y] = 0;
            }
    }

    Vector2Int CenterOf(RectInt rect)
    {
        return new Vector2Int(
            rect.xMin + rect.width / 2,
            rect.yMin + rect.height / 2);
    }

    void FillMapRandom()
    {
        string seed = Time.time.ToString();
        System.Random rng =
            new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 ||
                    y == 0 || y == height - 1)
                    map[x, y] = 1;
                else
                    map[x, y] =
                        rng.Next(0, 100) < fillPercent
                        ? 1 : 0;
            }
    }

    void SmoothMap()
    {
        int[,] newMap = new int[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                int walls = CountWallNeighbours(x, y);
                if (walls > 4) newMap[x, y] = 1;
                else if (walls < 4) newMap[x, y] = 0;
                else newMap[x, y] = map[x, y];
            }
        map = newMap;
    }

    int CountWallNeighbours(int x, int y)
    {
        int count = 0;
        for (int nx = x - 1; nx <= x + 1; nx++)
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                if (nx == x && ny == y) continue;
                if (nx < 0 || nx >= width ||
                    ny < 0 || ny >= height)
                    count++;
                else
                    count += map[nx, ny];
            }
        return count;
    }

    void FloodFill()
    {
        bool[,] visited = new bool[width, height];
        int bestSize = 0;
        int bestX = -1, bestY = -1;

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (map[x, y] == 0 && !visited[x, y])
                {
                    int size = GetRegionSize(
                        x, y, visited);
                    if (size > bestSize)
                    {
                        bestSize = size;
                        bestX = x;
                        bestY = y;
                    }
                }

        if (bestX == -1) return;

        bool[,] mainRegion = new bool[width, height];
        MarkRegion(bestX, bestY, mainRegion);

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (map[x, y] == 0 && !mainRegion[x, y])
                    map[x, y] = 1;
    }

    int GetRegionSize(
        int startX, int startY, bool[,] visited)
    {
        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));
        visited[startX, startY] = true;
        int size = 0;
        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            size++;
            for (int i = 0; i < 4; i++)
            {
                int nx = cur.x + dx[i];
                int ny = cur.y + dy[i];
                if (nx >= 0 && nx < width &&
                    ny >= 0 && ny < height &&
                    !visited[nx, ny] &&
                    map[nx, ny] == 0)
                {
                    visited[nx, ny] = true;
                    queue.Enqueue(
                        new Vector2Int(nx, ny));
                }
            }
        }
        return size;
    }

    void MarkRegion(
        int startX, int startY, bool[,] region)
    {
        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));
        region[startX, startY] = true;
        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            for (int i = 0; i < 4; i++)
            {
                int nx = cur.x + dx[i];
                int ny = cur.y + dy[i];
                if (nx >= 0 && nx < width &&
                    ny >= 0 && ny < height &&
                    !region[nx, ny] &&
                    map[nx, ny] == 0)
                {
                    region[nx, ny] = true;
                    queue.Enqueue(
                        new Vector2Int(nx, ny));
                }
            }
        }
    }

    void ForceWallBorders()
    {
        for (int x = 0; x < width; x++)
        {
            map[x, 0] = 1;
            map[x, height - 1] = 1;
        }
        for (int y = 0; y < height; y++)
        {
            map[0, y] = 1;
            map[width - 1, y] = 1;
        }
    }

    public Vector3 GetSpawnPoint()
    {
        int centerX = width / 2;
        int centerY = height / 2;

        for (int r = 0; r < width / 2; r++)
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;
                    if (x >= 2 && x < width - 2 &&
                        y >= 2 && y < height - 2 &&
                        map[x, y] == 0)
                    {
                        // Просто повертаємо координати тайлу
                        // без GetCellCenterWorld
                        return new Vector3(
                            x + 0.5f, y + 0.5f, 0f);
                    }
                }

        return new Vector3(
            centerX + 0.5f, centerY + 0.5f, 0f);
    }

    void DrawMap()
    {
        ApplyVisualStyle();
        tilemapFloor.ClearAllTiles();
        tilemapWalls.ClearAllTiles();

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (map[x, y] == 1)
                    tilemapWalls.SetTile(pos, wallTile);
                else
                    tilemapFloor.SetTile(pos, floorTile);
            }
    }

    void ApplyVisualStyle()
    {
        if (tilemapFloor != null)
            tilemapFloor.color = floorTint;
        if (tilemapWalls != null)
            tilemapWalls.color = wallTint;
        if (tilemapWater != null)
            tilemapWater.color = waterTint;
    }
    void SaveMetricsToCSV()
    {
        int walkable = 0;
        int total = width * height;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (map[x, y] == 0) walkable++;

        float walkPct = (float)walkable / total * 100f;
        float entropy = CalculateEntropy();
        string build = currentBuild != null
            ? currentBuild.buildName : "Default";

        string path = Application.persistentDataPath
                      + "/adaptive_map_metrics.csv";

        if (!File.Exists(path))
            File.WriteAllText(path,
                "time;build;mapMethod;width;height;" +
                "fill;smooth;walkable;entropy;" +
                "enemyMultiplier;lootMultiplier;" +
                "rangedBias;tankBias\n");

        string line = string.Format(
            CultureInfo.CurrentCulture,
            "{0};{1};{2};{3};{4};{5};{6};{7:F1};{8:F3};{9:F2};{10:F2};{11:F2};{12:F2}\n",
            DateTime.Now.ToString("HH:mm:ss"),
            build,
            generationMethod,
            width,
            height,
            fillPercent,
            smoothIterations,
            walkPct,
            entropy,
            adaptiveEnemyMultiplier,
            adaptiveLootMultiplier,
            adaptiveRangedEnemyBias,
            adaptiveTankEnemyBias);

        File.AppendAllText(path, line);
    }
   

    float CalculateEntropy()
    {
        int total = width * height;
        int walls = 0;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (map[x, y] == 1) walls++;

        float pw = (float)walls / total;
        float pf = 1f - pw;
        if (pw <= 0 || pf <= 0) return 0f;
        return -(pw * Mathf.Log(pw, 2f)
               + pf * Mathf.Log(pf, 2f));
 
    }
    [Header("Озера")]
    public Tilemap tilemapWater;
    public TileBase waterTile;
    [Range(0f, 1f)]
    public float lakeChance = 0.08f;

    void GenerateLakes()
    {
        if (tilemapWater == null)
        {
            Debug.Log("tilemapWater не підключений!");
            return;
        }
        if (waterTile == null)
        {
            Debug.Log("waterTile не підключений!");
            return;
        }

        tilemapWater.ClearAllTiles();
        int lakesCreated = 0;
        int maxLakes = 3;

        for (int x = 6; x < width - 6 &&
             lakesCreated < maxLakes; x++)
        {
            for (int y = 6; y < height - 6 &&
                 lakesCreated < maxLakes; y++)
            {
                if (map[x, y] != 0) continue;
                if (UnityEngine.Random.value > lakeChance) continue;

                // Перевіряємо що з 3 сторін стіни
                int wallCount = 0;
                if (map[x - 1, y] == 1) wallCount++;
                if (map[x + 1, y] == 1) wallCount++;
                if (map[x, y - 1] == 1) wallCount++;
                if (map[x, y + 1] == 1) wallCount++;

                if (wallCount < 3) continue;

                // Перевіряємо що є прохідний шлях
                if (!HasWalkableAround(x, y, 3)) continue;

                SpawnLake(x, y);
                lakesCreated++;
            }
        }

        Debug.Log($"Створено {lakesCreated} озер");
    }

    bool HasWalkableAround(int cx, int cy, int radius)
    {
        int walkable = 0;
        for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                int nx = cx + dx;
                int ny = cy + dy;
                if (nx < 0 || nx >= width ||
                    ny < 0 || ny >= height) continue;
                if (map[nx, ny] == 0) walkable++;
            }
        return walkable >= 6;
    }

    void SpawnLake(int startX, int startY)
    {
        int sizeX = UnityEngine.Random.Range(2, 5);
        int sizeY = UnityEngine.Random.Range(2, 4);

        for (int dx = -sizeX / 2; dx <= sizeX / 2; dx++)
            for (int dy = -sizeY / 2; dy <= sizeY / 2; dy++)
            {
                int x = startX + dx;
                int y = startY + dy;

                if (x < 3 || x >= width - 3 ||
                    y < 3 || y >= height - 3) continue;
                if (map[x, y] != 0) continue;
                if (!HasWalkableAround(x, y, 3)) continue;

                Vector3Int pos = new Vector3Int(x, y, 0);
                tilemapWater.SetTile(pos, waterTile);
                map[x, y] = 2; // 2 = вода
            }
    }

}
