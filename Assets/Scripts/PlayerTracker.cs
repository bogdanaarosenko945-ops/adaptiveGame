using UnityEngine;
using System.Globalization;
using System.IO;

public class PlayerTracker : MonoBehaviour
{
    public static PlayerTracker Instance;

    [Header("References")]
    public MapGenerator mapGenerator;
    public AbilityData[] selectedAbilities;

    private PlayerProfile profile;
    public int TotalKills => profile != null ? profile.totalKills : 0;
    public int TotalDeaths => profile != null ? profile.totalDeaths : 0;
    public int RunDeaths => profile != null ? profile.runDeaths : 0;
    public int HpLootPickups => profile != null ? profile.hpLootPickups : 0;
    public int ManaLootPickups => profile != null ? profile.manaLootPickups : 0;
    public int CoinPickups => profile != null ? profile.coinPickups : 0;
    public float MeleeRate => profile != null ? profile.meleeRate : 0f;
    public float RangedRate => profile != null ? profile.rangedRate : 0f;
    public float MagicRate => profile != null ? profile.magicRate : 0f;
    public string LastMLStatus { get; private set; } =
        "Waiting for completed runs";
    public int LastMLRunCount { get; private set; }
    public float LastMLMAE { get; private set; }
    public float LastMLMSE { get; private set; }
    public float LastMLR2 { get; private set; }
    public string LastMLPredictionSummary { get; private set; } = "-";
    public string LastMLReportPath { get; private set; } = "-";
    public string DataFolder => Application.persistentDataPath;

    public void GenerateMLReport()
    {
        LastMLReportPath = MLReportExporter.Export(
            Application.persistentDataPath);
        LastMLStatus = "HTML report generated";
    }

    public void AddDemoMLData()
    {
        MLReportExporter.AppendDemoData(Application.persistentDataPath);
        GenerateMLReport();
        LastMLStatus = "Demo ML data added";
    }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        profile = PlayerProfile.Load();
    }

    public void RegisterKill(string weaponType)
    {
        profile.totalKills++;
        switch (weaponType)
        {
            case "melee": profile.meleeKills++; break;
            case "ranged": profile.rangedKills++; break;
            case "magic": profile.magicKills++; break;
        }
    }

    public void RegisterDeath(Vector2 position)
    {
        profile.totalDeaths++;
        profile.runDeaths++;
        string zone = GetZoneType(position);
        if (zone == "corridor")
            profile.deathsInCorridors++;
        else
            profile.deathsInOpenAreas++;

        EndRun(true, "Death");
    }

    public void RegisterRunComplete()
    {
        EndRun(false, "Complete");
    }

    public void RegisterLootPickup(string lootType)
    {
        if (lootType == "hp")
            profile.hpLootPickups++;
        else if (lootType == "mana")
            profile.manaLootPickups++;
        else if (lootType == "coin")
            profile.coinPickups++;
    }

    void EndRun(bool regenerateMap, string result)
    {
        profile.totalRuns++;
        profile.UpdateRates();
        SaveRunToCSV(result);
        RunResultLogger.LogRun(
            result,
            profile,
            selectedAbilities,
            mapGenerator);
        RunMLAdaptation(regenerateMap, result);
        profile.ResetRunStats();
        profile.Save();
    }

    void RunMLAdaptation(bool regenerateMap, string result)
    {
        LinearRegression ml =
            new LinearRegression(15, 6);

        string csvPath =
            Application.persistentDataPath
            + "/player_data.csv";
        string modelPath =
            Application.persistentDataPath
            + "/adaptive_model_v2.txt";

        if (!File.Exists(csvPath) ||
            File.ReadAllLines(csvPath).Length < 3)
        {
            LastMLRunCount = CountDataRows(csvPath);
            LastMLStatus = "Skipped: need at least 2 completed runs";
            LastMLPredictionSummary = "-";
            Debug.Log("ML skipped: not enough completed runs yet.");
            return;
        }

        LastMLRunCount = CountDataRows(csvPath);
        LastMLStatus = "Training from CSV";
        ml.LoadModel(modelPath);
        ml.TrainFromCSV(csvPath);
        LastMLMAE = ml.LastMAE;
        LastMLMSE = ml.LastMSE;
        LastMLR2 = ml.LastR2;

        float[] input = GetMLInput(result);
        float[] prediction = ml.PredictSingle(input);

        float newFill = prediction[0];
        float newSmooth = prediction[1];
        float enemyMultiplier = prediction[2];
        float lootMultiplier = prediction[3];
        float rangedEnemyBias = prediction[4];
        float tankEnemyBias = prediction[5];
        int baseWidth = mapGenerator != null ? mapGenerator.width : 0;
        int baseHeight = mapGenerator != null ? mapGenerator.height : 0;
        int baseFill = mapGenerator != null ? mapGenerator.fillPercent : 45;
        int baseSmooth = mapGenerator != null ? mapGenerator.smoothIterations : 5;
        LastMLPredictionSummary =
            $"fill={newFill:F1}, smooth={newSmooth:F1}, " +
            $"enemy=x{enemyMultiplier:F2}, loot=x{lootMultiplier:F2}, " +
            $"ranged={rangedEnemyBias:F2}, tank={tankEnemyBias:F2}";

        Debug.Log(
            "ML prediction: " +
            $"fill={newFill:F1}, " +
            $"smooth={newSmooth:F1}, " +
            $"enemies=x{enemyMultiplier:F2}, " +
            $"loot=x{lootMultiplier:F2}, " +
            $"rangedBias={rangedEnemyBias:F2}, " +
            $"tankBias={tankEnemyBias:F2}");

        if (mapGenerator != null)
        {
            mapGenerator.ApplyMLParams(
                Mathf.RoundToInt(newFill),
                Mathf.RoundToInt(newSmooth),
                enemyMultiplier,
                lootMultiplier,
                rangedEnemyBias,
                tankEnemyBias,
                regenerateMap);

            SaveMLPredictionToCSV(
                ml,
                input,
                prediction,
                mapGenerator.fillPercent,
                mapGenerator.smoothIterations,
                mapGenerator.adaptiveEnemyMultiplier,
                mapGenerator.adaptiveLootMultiplier,
                mapGenerator.adaptiveRangedEnemyBias,
                mapGenerator.adaptiveTankEnemyBias,
                baseFill,
                baseSmooth,
                baseWidth,
                baseHeight,
                mapGenerator.width,
                mapGenerator.height);
            LastMLStatus = regenerateMap
                ? "Applied and regenerated map"
                : "Applied to next map";
        }
        else
        {
            SaveMLPredictionToCSV(
                ml,
                input,
                prediction,
                Mathf.Clamp(Mathf.RoundToInt(newFill), 35, 58),
                Mathf.Clamp(Mathf.RoundToInt(newSmooth), 3, 9),
                Mathf.Clamp(enemyMultiplier, 0.65f, 1.45f),
                Mathf.Clamp(lootMultiplier, 0.70f, 1.60f),
                Mathf.Clamp(rangedEnemyBias, 0f, 0.45f),
                Mathf.Clamp(tankEnemyBias, 0f, 0.45f),
                baseFill,
                baseSmooth,
                baseWidth,
                baseHeight,
                baseWidth,
                baseHeight);
            LastMLStatus = "Saved prediction without map reference";
        }

        LastMLReportPath = MLReportExporter.Export(
            Application.persistentDataPath);
    }

    int CountDataRows(string path)
    {
        if (!File.Exists(path)) return 0;

        int lines = File.ReadAllLines(path).Length;
        return Mathf.Max(0, lines - 1);
    }

    float[] GetMLInput(string result)
    {
        GetAbilityWeights(
            out float meleeAbW,
            out float rangedAbW,
            out float magicAbW);

        return LinearRegression.BuildInput(
            GetClassCode(),
            meleeAbW,
            rangedAbW,
            magicAbW,
            profile.meleeRate,
            profile.rangedRate,
            profile.magicRate,
            profile.runDeaths,
            profile.hpLootPickups,
            profile.manaLootPickups,
            profile.coinPickups,
            result == "Complete" ? 1f : 0f,
            CalculatePerformanceScore(result));
    }

    float GetClassCode()
    {
        if (GameManager.Instance == null) return 0f;

        switch (GameManager.Instance.selectedClass)
        {
            case "Warrior": return 1f;
            case "Mage": return 2f;
            case "Rogue": return 3f;
            default: return 0f;
        }
    }

    void GetAbilityWeights(
        out float meleeAbW,
        out float rangedAbW,
        out float magicAbW)
    {
        meleeAbW = 0f;
        rangedAbW = 0f;
        magicAbW = 0f;

        if (selectedAbilities == null) return;

        foreach (var ab in selectedAbilities)
        {
            if (ab == null) continue;
            meleeAbW += ab.meleeWeight;
            rangedAbW += ab.rangedWeight;
            magicAbW += ab.magicWeight;
        }
    }

    float CalculatePerformanceScore(string result)
    {
        float score = 0f;
        score += profile.totalKills * 1.5f;
        score += profile.coinPickups * 0.4f;
        score += profile.hpLootPickups * 0.25f;
        score += profile.manaLootPickups * 0.25f;
        score -= profile.runDeaths * 4f;

        if (result == "Complete")
            score += 5f;

        return Mathf.Max(0f, score);
    }

    string GetZoneType(Vector2 position)
    {
        int openNeighbours = 0;
        for (int dx = -3; dx <= 3; dx++)
            for (int dy = -3; dy <= 3; dy++)
            {
                Vector2Int check = new Vector2Int(
                    Mathf.RoundToInt(position.x + dx),
                    Mathf.RoundToInt(position.y + dy));
                if (!Physics2D.OverlapPoint(
                    new Vector2(check.x, check.y)))
                    openNeighbours++;
            }
        return openNeighbours > 20 ? "open" : "corridor";
    }

    void EnsureCsvExists(string path)
    {
        if (!File.Exists(path))
            File.WriteAllText(path,
                "run;result;performanceScore;class;meleeAbW;rangedAbW;" +
                "magicAbW;meleeRate;rangedRate;" +
                "magicRate;deaths;hpLoot;" +
                "manaLoot;coinLoot\n");
    }

    void SaveRunToCSV(string result)
    {
        string path =
            Application.persistentDataPath
            + "/player_data.csv";

        EnsureCsvExists(path);

        GetAbilityWeights(
            out float mW,
            out float rW,
            out float mgW);

        string line = string.Format(
            CultureInfo.CurrentCulture,
            "{0};{1};{2:F2};{3};{4:F2};{5:F2};{6:F2};{7:F2};{8:F2};{9:F2};{10};{11};{12};{13}\n",
            profile.totalRuns,
            result,
            CalculatePerformanceScore(result),
            GetClassCode(),
            mW,
            rW,
            mgW,
            profile.meleeRate,
            profile.rangedRate,
            profile.magicRate,
            profile.runDeaths,
            profile.hpLootPickups,
            profile.manaLootPickups,
            profile.coinPickups);

        File.AppendAllText(path, line);
        Debug.Log($"Player data saved: {path}");
    }

    void SaveMLPredictionToCSV(
        LinearRegression ml,
        float[] input,
        float[] prediction,
        int appliedFill,
        int appliedSmooth,
        float appliedEnemyMultiplier,
        float appliedLootMultiplier,
        float appliedRangedBias,
        float appliedTankBias,
        int baseFill,
        int baseSmooth,
        int baseWidth,
        int baseHeight,
        int appliedWidth,
        int appliedHeight)
    {
        string path =
            Application.persistentDataPath
            + "/ml_predictions.csv";

        if (!File.Exists(path))
        {
            File.WriteAllText(path,
                MLReportExporter.PredictionHeader());
        }

        string difficulty =
            DifficultyManager.Instance != null
            ? DifficultyManager.Instance.currentDifficulty.ToString()
            : "Easy";

        string className =
            GameManager.Instance != null
            ? GameManager.Instance.selectedClass
            : "";

        string line = string.Format(
            CultureInfo.CurrentCulture,
            "{0};{1};{2};{3};" +
            "{4:F3};{5:F3};{6:F3};" +
            "{7:F3};{8:F3};{9:F3};" +
            "{10:F3};{11:F3};{12:F3};" +
            "{13:F3};{14:F3};{15:F3};{16:F3};" +
            "{17:F3};{18:F3};" +
            "{19:F3};{20:F3};{21:F3};" +
            "{22:F3};{23:F3};{24:F3};" +
            "{25};{26};{27:F3};" +
            "{28:F3};{29:F3};{30:F3};" +
            "{31:F4};{32:F4};{33:F4};" +
            "{34};{35};{36};{37};{38};{39}\n",
            System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            profile.totalRuns,
            className,
            difficulty,
            input[0],
            input[1],
            input[2],
            input[3],
            input[4],
            input[5],
            input[6],
            input[7],
            input[8],
            input[9],
            input[10],
            input[11],
            input[12],
            input[13],
            input[14],
            prediction[0],
            prediction[1],
            prediction[2],
            prediction[3],
            prediction[4],
            prediction[5],
            appliedFill,
            appliedSmooth,
            appliedEnemyMultiplier,
            appliedLootMultiplier,
            appliedRangedBias,
            appliedTankBias,
            ml.LastMAE,
            ml.LastMSE,
            ml.LastR2,
            baseFill,
            baseSmooth,
            baseWidth,
            baseHeight,
            appliedWidth,
            appliedHeight);

        File.AppendAllText(path, line);
        Debug.Log("ML prediction saved: " + path);
    }
}
