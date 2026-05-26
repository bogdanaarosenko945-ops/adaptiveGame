using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class PlayerProfile
{
    // Вибрані абілки (індекси)
    public int ability1Index = 0;
    public int ability2Index = 1;
    public int ability3Index = 2;

    // Стиль гравця (0-1)
    public float meleeRate = 0f;
    public float rangedRate = 0f;
    public float magicRate = 0f;

    // Статистика бою
    public int meleeKills = 0;
    public int rangedKills = 0;
    public int magicKills = 0;
    public int totalKills = 0;

    // Смерті
    public int totalDeaths = 0;
    public int runDeaths = 0;
    public int deathsInCorridors = 0;
    public int deathsInOpenAreas = 0;

    // Лут
    public int hpLootPickups = 0;
    public int manaLootPickups = 0;
    public int coinPickups = 0;

    // Забіги
    public int totalRuns = 0;

    // Оновлює rates після кожного забігу
    public void UpdateRates()
    {
        if (totalKills == 0) return;
        meleeRate = (float)meleeKills / totalKills;
        rangedRate = (float)rangedKills / totalKills;
        magicRate = (float)magicKills / totalKills;
    }

    // Зберегти в JSON
    public void Save()
    {
        string path = Application.persistentDataPath
                      + "/profile.json";
        string json = JsonUtility.ToJson(this, true);
        File.WriteAllText(path, json);
        Debug.Log("Профіль збережено: " + path);
    }

    // Завантажити з JSON
    public static PlayerProfile Load()
    {
        string path = Application.persistentDataPath
                      + "/profile.json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<PlayerProfile>(json);
        }
        return new PlayerProfile();
    }

    // Скинути статистику після забігу
    // (але не загальні дані)
    public void ResetRunStats()
    {
        meleeKills = 0;
        rangedKills = 0;
        magicKills = 0;
        totalKills = 0;
        runDeaths = 0;
        deathsInCorridors = 0;
        deathsInOpenAreas = 0;
        hpLootPickups = 0;
        manaLootPickups = 0;
        coinPickups = 0;

    }
}
