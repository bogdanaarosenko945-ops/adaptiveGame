using UnityEngine;

[CreateAssetMenu(fileName = "BuildConfig", menuName = "Palimpsest/Build Config")]
public class BuildConfig : ScriptableObject
{
    [Header("Назва білда")]
    public string buildName = "Warrior";

    [Header("Параметри карти")]
    public int mapWidth = 50;
    public int mapHeight = 50;
    [Range(0, 100)]
    public int fillPercent = 45;
    public int smoothIterations = 5;

    [Header("Щільність кімнат")]
    [Range(0f, 1f)]
    public float enemyDensity = 0.5f;
    [Range(0f, 1f)]
    public float lootDensity = 0.3f;
    [Range(0f, 1f)]
    public float trapDensity = 0.1f;

    [Header("Префаби")]
    public GameObject enemyPrefab;
    public GameObject lootPrefab;
    public GameObject trapPrefab;
}
    