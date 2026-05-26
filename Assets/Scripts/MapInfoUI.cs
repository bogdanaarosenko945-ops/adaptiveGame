using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapInfoUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject infoPanel;
    public TextMeshProUGUI infoText;
    public Button infoButton;
    public Button closeButton;

    [Header("References")]
    public MapGenerator mapGenerator;

    private bool isVisible;

    void Start()
    {
        if (infoButton != null)
            infoButton.onClick.AddListener(ToggleInfo);

        if (closeButton != null)
            closeButton.onClick.AddListener(HideInfo);

        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (infoButton != null)
            infoButton.onClick.RemoveListener(ToggleInfo);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(HideInfo);
    }

    void ToggleInfo()
    {
        if (infoPanel == null)
            return;

        isVisible = !isVisible;
        infoPanel.SetActive(isVisible);

        if (isVisible)
            UpdateInfo();
    }

    void HideInfo()
    {
        isVisible = false;

        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    void UpdateInfo()
    {
        if (infoText == null)
            return;

        if (mapGenerator == null)
        {
            infoText.text = "MapGenerator не знайдено.";
            return;
        }

        string className = GameManager.Instance != null
            ? GameManager.Instance.selectedClass
            : "невідомо";
        if (string.IsNullOrEmpty(className))
            className = "не вибрано";

        string difficulty = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.currentDifficulty.ToString()
            : "Easy";

        string mlStatus = PlayerTracker.Instance != null
            ? PlayerTracker.Instance.LastMLStatus
            : "очікування даних";

        int csvRuns = CountCsvRows("player_data.csv");
        string abilities = BuildAbilityList();

        infoText.text =
            "=== ПОТОЧНА КАРТА ===\n\n" +
            $"Клас: {className}\n" +
            $"Складність: {difficulty}\n" +
            $"Здібності:\n{abilities}\n" +
            "=== ПАРАМЕТРИ ГЕНЕРАЦІЇ ===\n\n" +
            $"Метод: {mapGenerator.generationMethod}\n" +
            $"Розмір: {mapGenerator.width} x {mapGenerator.height}\n" +
            $"Заповнення стінами: {mapGenerator.fillPercent}%\n" +
            $"Ітерації згладжування: {mapGenerator.smoothIterations}\n" +
            $"Вороги: x{mapGenerator.adaptiveEnemyMultiplier:F2}\n" +
            $"Лут: x{mapGenerator.adaptiveLootMultiplier:F2}\n" +
            "Зміщення ворогів: " +
            $"дальні {mapGenerator.adaptiveRangedEnemyBias:F2}, " +
            $"танки {mapGenerator.adaptiveTankEnemyBias:F2}\n\n" +
            "=== ML-АДАПТАЦІЯ ===\n\n" +
            $"CSV-забігів зібрано: {csvRuns}\n" +
            $"Статус: {mlStatus}\n" +
            "Модель: багатовихідна лінійна регресія\n" +
            "Входи: клас, здібності, стиль бою, смерті, лут, результат забігу\n" +
            "Виходи: fill, smooth, множник ворогів, множник луту, типи ворогів\n\n" +
            "Пояснення: після завершення забігу ML читає CSV-статистику, " +
            "навчається і змінює параметри наступної карти.";
    }

    string BuildAbilityList()
    {
        if (GameManager.Instance == null ||
            GameManager.Instance.selectedAbilities == null ||
            GameManager.Instance.selectedAbilities.Count == 0)
            return "  - немає\n";

        StringBuilder sb = new StringBuilder();
        foreach (AbilityData ability in GameManager.Instance.selectedAbilities)
        {
            if (ability == null) continue;
            sb.AppendLine("  - " + ability.abilityName);
        }

        return sb.Length > 0 ? sb.ToString() : "  - немає\n";
    }

    int CountCsvRows(string fileName)
    {
        string path = Path.Combine(
            Application.persistentDataPath,
            fileName);

        if (!File.Exists(path))
            return 0;

        return Mathf.Max(0, File.ReadAllLines(path).Length - 1);
    }
}
