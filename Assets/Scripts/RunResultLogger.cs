using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public static class RunResultLogger
{
    public static void LogRun(
        string result,
        PlayerProfile profile,
        AbilityData[] abilities,
        MapGenerator map)
    {
        if (profile == null) return;

        string path = Application.persistentDataPath
                      + "/run_results.csv";

        if (!File.Exists(path))
        {
            File.WriteAllText(
                path,
                "time;run;result;difficulty;mapMethod;class;abilities;" +
                "kills;runDeaths;totalDeaths;meleeRate;rangedRate;magicRate;" +
                "hpLoot;manaLoot;coins;width;height;fill;smooth;" +
                "enemyMultiplier;lootMultiplier;rangedBias;tankBias\n");
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
            "{0};{1};{2};{3};{4};{5};\"{6}\";{7};{8};{9};{10:F2};{11:F2};{12:F2};{13};{14};{15};{16};{17};{18};{19};{20:F2};{21:F2};{22:F2};{23:F2}\n",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            profile.totalRuns,
            result,
            difficulty,
            map != null ? map.generationMethod.ToString() : "",
            className,
            BuildAbilityList(abilities),
            profile.totalKills,
            profile.runDeaths,
            profile.totalDeaths,
            profile.meleeRate,
            profile.rangedRate,
            profile.magicRate,
            profile.hpLootPickups,
            profile.manaLootPickups,
            profile.coinPickups,
            map != null ? map.width : 0,
            map != null ? map.height : 0,
            map != null ? map.fillPercent : 0,
            map != null ? map.smoothIterations : 0,
            map != null ? map.adaptiveEnemyMultiplier : 1f,
            map != null ? map.adaptiveLootMultiplier : 1f,
            map != null ? map.adaptiveRangedEnemyBias : 0f,
            map != null ? map.adaptiveTankEnemyBias : 0f);

        File.AppendAllText(path, line);
        Debug.Log("Run result saved: " + path);
    }

    static string BuildAbilityList(AbilityData[] abilities)
    {
        if (abilities == null || abilities.Length == 0)
            return "";

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < abilities.Length; i++)
        {
            if (abilities[i] == null) continue;
            if (sb.Length > 0) sb.Append(" | ");
            sb.Append(abilities[i].abilityName);
        }

        return sb.ToString().Replace("\"", "'");
    }
}
