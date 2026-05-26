using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AdaptiveRuntimeUI : MonoBehaviour
{
    private Canvas canvas;
    private GameObject debugPanel;
    private TextMeshProUGUI debugText;
    private bool debugVisible = false;
    private float nextDebugUpdate;
    private string reportOpenStatus = "-";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        TryCreateForScene(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreateForScene(scene);
    }

    static void TryCreateForScene(Scene scene)
    {
        if (scene.name == "MainMenu")
            return;

        if (FindAnyObjectByType<AdaptiveRuntimeUI>() != null)
            return;

        GameObject ui = new GameObject("Adaptive Runtime UI");
        DontDestroyOnLoad(ui);
        ui.AddComponent<AdaptiveRuntimeUI>();
    }

    void Awake()
    {
        EnsureDifficultyManager();
        EnsureEventSystem();
        CreateCanvas();
        CreateDebugPanel();
    }

    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.f1Key.wasPressedThisFrame)
        {
            debugVisible = !debugVisible;
            debugPanel.SetActive(debugVisible);
        }

        if (Keyboard.current != null &&
            Keyboard.current.f2Key.wasPressedThisFrame)
        {
            PlayerTracker.Instance?.AddDemoMLData();
            debugVisible = true;
            debugPanel.SetActive(true);
            UpdateDebugText();
        }

        if (Keyboard.current != null &&
            Keyboard.current.f3Key.wasPressedThisFrame)
        {
            OpenReport();
        }

        if (!debugVisible) return;
        if (Time.unscaledTime < nextDebugUpdate) return;

        nextDebugUpdate = Time.unscaledTime + 0.25f;
        debugPanel.SetActive(debugVisible);
        UpdateDebugText();
    }

    void EnsureDifficultyManager()
    {
        if (DifficultyManager.Instance != null ||
            FindAnyObjectByType<DifficultyManager>() != null)
            return;

        GameObject obj = new GameObject("DifficultyManager");
        DontDestroyOnLoad(obj);
        obj.AddComponent<DifficultyManager>();
    }

    void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    void CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Adaptive UI Canvas");
        canvasObject.transform.SetParent(transform);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler =
            canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObject.AddComponent<GraphicRaycaster>();
    }

    void OnReportButtonClicked()
    {
        PlayerTracker.Instance?.GenerateMLReport();
        debugVisible = true;
        debugPanel.SetActive(true);
        UpdateDebugText();
    }

    void OnOpenReportButtonClicked()
    {
        OpenReport();
    }

    void OpenReport()
    {
        PlayerTracker tracker = PlayerTracker.Instance;
        string path = "";

        if (tracker != null)
        {
            if (string.IsNullOrEmpty(tracker.LastMLReportPath) ||
                tracker.LastMLReportPath == "-")
            {
                tracker.GenerateMLReport();
            }

            path = tracker.LastMLReportPath;
        }
        else
        {
            path = MLReportExporter.Export(
                Application.persistentDataPath);
        }

        if (string.IsNullOrEmpty(path) || path == "-")
        {
            reportOpenStatus = "Report path is empty";
            return;
        }

        reportOpenStatus = TryOpenReport(path)
            ? "Open request sent: " + path
            : "Could not open: " + path;
        debugVisible = true;
        debugPanel.SetActive(true);
        UpdateDebugText();
    }

    bool TryOpenReport(string path)
    {
        try
        {
            System.Diagnostics.ProcessStartInfo info =
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                };
            System.Diagnostics.Process.Start(info);
            Debug.Log("Opening ML report through shell: " + path);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "Shell open failed, trying Application.OpenURL: " +
                ex.Message);
        }

        try
        {
            string url = new Uri(path).AbsoluteUri;
            Application.OpenURL(url);
            Debug.Log("Opening ML report through URL: " + url);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("Could not open ML report: " + ex.Message);
            return false;
        }
    }

    void CreateDebugPanel()
    {
        debugPanel = CreatePanel(
            "ML Debug Panel",
            canvas.transform,
            new Vector2(0f, 1f),
            new Vector2(860f, 900f),
            new Color(0.02f, 0.025f, 0.03f, 0.88f));

        SetRect(
            debugPanel.GetComponent<RectTransform>(),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(24f, -24f),
            new Vector2(860f, 900f));

        debugText = CreateText(
            "Debug Text",
            debugPanel.transform,
            "",
            22,
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft);
        debugText.color = new Color(0.88f, 0.94f, 1f, 1f);
        SetRect(
            debugText.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero);
        debugText.rectTransform.offsetMin =
            new Vector2(28f, 112f);
        debugText.rectTransform.offsetMax =
            new Vector2(-28f, -24f);

        CreateDebugPanelButton(
            "Generate Report Button",
            "Generate Report",
            new Vector2(28f, 28f),
            OnReportButtonClicked);

        CreateDebugPanelButton(
            "Open HTML Report Button",
            "Open HTML Report",
            new Vector2(294f, 28f),
            OnOpenReportButtonClicked);

        UpdateDebugText();
        debugPanel.SetActive(debugVisible);
    }

    void CreateDebugPanelButton(
        string objectName,
        string labelText,
        Vector2 position,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(objectName);
        buttonObj.transform.SetParent(debugPanel.transform, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        SetRect(
            rect,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            position,
            new Vector2(240f, 56f));

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.10f, 0.18f, 0.28f, 0.96f);

        Button button = buttonObj.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        TextMeshProUGUI label = CreateText(
            "Label",
            buttonObj.transform,
            labelText,
            20,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        label.color = Color.white;
        SetRect(
            label.rectTransform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero);
    }

    void UpdateDebugText()
    {
        StringBuilder sb = new StringBuilder();

        GameManager gm = GameManager.Instance;
        MapGenerator map = FindAnyObjectByType<MapGenerator>();
        PlayerTracker tracker = PlayerTracker.Instance;
        Difficulty difficulty =
            DifficultyManager.Instance != null
            ? DifficultyManager.Instance.currentDifficulty
            : Difficulty.Easy;

        sb.AppendLine("Adaptive ML Debug  [F1]");
        sb.AppendLine("F2: add demo ML data");
        sb.AppendLine("F3: open HTML report");
        sb.AppendLine($"Difficulty: {difficulty}");
        sb.AppendLine(
            $"Map method: {(map != null ? map.generationMethod : MapGenerationMethod.CellularAutomata)}");
        sb.AppendLine($"Class: {(gm != null ? gm.selectedClass : "-")}");

        sb.Append("Abilities: ");
        if (gm != null && gm.selectedAbilities.Count > 0)
        {
            for (int i = 0; i < gm.selectedAbilities.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(gm.selectedAbilities[i].abilityName);
            }
        }
        else
        {
            sb.Append("-");
        }
        sb.AppendLine();

        sb.AppendLine();
        sb.AppendLine("Player data");
        sb.AppendLine(
            $"Run deaths: {(tracker != null ? tracker.RunDeaths : 0)}");
        sb.AppendLine(
            $"Total deaths: {(tracker != null ? tracker.TotalDeaths : 0)}");
        sb.AppendLine(
            $"Kills: {(tracker != null ? tracker.TotalKills : 0)}");
        sb.AppendLine(
            "Style: " +
            $"M {GetRate(tracker?.MeleeRate):F2} / " +
            $"R {GetRate(tracker?.RangedRate):F2} / " +
            $"G {GetRate(tracker?.MagicRate):F2}");
        sb.AppendLine(
            "Loot: " +
            $"HP {tracker?.HpLootPickups ?? 0}, " +
            $"Mana {tracker?.ManaLootPickups ?? 0}, " +
            $"Coins {tracker?.CoinPickups ?? 0}");

        sb.AppendLine();
        sb.AppendLine("Generated content");
        sb.AppendLine(
            $"Map: {(map != null ? map.width : 0)}x{(map != null ? map.height : 0)}");
        sb.AppendLine(
            $"Fill: {(map != null ? map.fillPercent : 0)}");
        sb.AppendLine(
            $"Smooth: {(map != null ? map.smoothIterations : 0)}");
        sb.AppendLine(
            $"Enemies: x{(map != null ? map.adaptiveEnemyMultiplier : 1f):F2}");
        sb.AppendLine(
            $"Loot: x{(map != null ? map.adaptiveLootMultiplier : 1f):F2}");
        sb.AppendLine(
            "Bias: " +
            $"Ranged {(map != null ? map.adaptiveRangedEnemyBias : 0f):F2}, " +
            $"Tank {(map != null ? map.adaptiveTankEnemyBias : 0f):F2}");

        sb.AppendLine();
        sb.AppendLine("ML model");
        sb.AppendLine(
            $"Status: {(tracker != null ? tracker.LastMLStatus : "-")}");
        sb.AppendLine(
            $"CSV runs: {(tracker != null ? tracker.LastMLRunCount : 0)}");
        sb.AppendLine(
            "Metrics: " +
            $"MAE {GetRate(tracker?.LastMLMAE):F3}, " +
            $"MSE {GetRate(tracker?.LastMLMSE):F3}, " +
            $"R2 {GetRate(tracker?.LastMLR2):F3}");
        sb.AppendLine(
            "Last prediction: " +
            $"{(tracker != null ? tracker.LastMLPredictionSummary : "-")}");
        sb.AppendLine(
            "Data folder: " +
            $"{(tracker != null ? tracker.DataFolder : "-")}");
        sb.AppendLine(
            "HTML report: " +
            $"{(tracker != null ? tracker.LastMLReportPath : "-")}");
        sb.AppendLine("Open status: " + reportOpenStatus);

        sb.AppendLine();
        sb.AppendLine("ML-підказка");
        sb.AppendLine(
            "Після кожного забігу ML читає CSV-статистику " +
            "і прогнозує параметри наступної карти.");
        sb.AppendLine(
            "Змінюються fill, smooth, вороги, лут і типи ворогів.");

        debugText.text = sb.ToString();
    }

    float GetRate(float? value)
    {
        return value ?? 0f;
    }

    GameObject CreatePanel(
        string name,
        Transform parent,
        Vector2 pivot,
        Vector2 size,
        Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect =
            panel.AddComponent<RectTransform>();
        rect.pivot = pivot;
        rect.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = color;

        return panel;
    }

    TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string text,
        int size,
        FontStyles style,
        TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp =
            obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.textWrappingMode = TextWrappingModes.Normal;

        return tmp;
    }

    void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }
}
