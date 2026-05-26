using UnityEngine;
using UnityEngine.UI;

public class RunSetupUI : MonoBehaviour
{
    [SerializeField] private GameObject setupPanel;
    [SerializeField] private Button easyButton;
    [SerializeField] private Button hardButton;
    [SerializeField] private Button cellularButton;
    [SerializeField] private Button bspButton;
    [SerializeField] private Button startButton;
    [SerializeField] private AbilitySelectionUI abilitySelectionUI;
    [SerializeField] private GameObject easySelectedIndicator;
    [SerializeField] private GameObject hardSelectedIndicator;
    [SerializeField] private GameObject cellularSelectedIndicator;
    [SerializeField] private GameObject bspSelectedIndicator;
    [SerializeField] private bool centerStartButtonAtBottom = true;
    [SerializeField] private Vector2 startButtonBottomOffset =
        new Vector2(0f, 24f);

    private Difficulty selectedDifficulty = Difficulty.Easy;
    private MapGenerationMethod selectedGenerationMethod =
        MapGenerationMethod.CellularAutomata;

    void Awake()
    {
        if (setupPanel == null)
            setupPanel = gameObject;

        if (abilitySelectionUI == null)
            abilitySelectionUI = FindAnyObjectByType<AbilitySelectionUI>();

        AddListeners();
        PositionStartButton();
        LoadCurrentSelections();
        RefreshSelectionVisuals();
    }

    void Start()
    {
        if (abilitySelectionUI != null)
            abilitySelectionUI.HideAll();

        setupPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    void OnDestroy()
    {
        RemoveListeners();
    }

    void AddListeners()
    {
        if (easyButton != null)
            easyButton.onClick.AddListener(SelectEasy);
        if (hardButton != null)
            hardButton.onClick.AddListener(SelectHard);
        if (cellularButton != null)
            cellularButton.onClick.AddListener(SelectCellular);
        if (bspButton != null)
            bspButton.onClick.AddListener(SelectBSP);
        if (startButton != null)
            startButton.onClick.AddListener(StartRunSetup);
    }

    void RemoveListeners()
    {
        if (easyButton != null)
            easyButton.onClick.RemoveListener(SelectEasy);
        if (hardButton != null)
            hardButton.onClick.RemoveListener(SelectHard);
        if (cellularButton != null)
            cellularButton.onClick.RemoveListener(SelectCellular);
        if (bspButton != null)
            bspButton.onClick.RemoveListener(SelectBSP);
        if (startButton != null)
            startButton.onClick.RemoveListener(StartRunSetup);
    }

    public void SelectEasy()
    {
        selectedDifficulty = Difficulty.Easy;
        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.SetDifficulty(Difficulty.Easy);
        RefreshSelectionVisuals();
    }

    public void SelectHard()
    {
        selectedDifficulty = Difficulty.Hard;
        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.SetDifficulty(Difficulty.Hard);
        RefreshSelectionVisuals();
    }

    public void SelectCellular()
    {
        selectedGenerationMethod =
            MapGenerationMethod.CellularAutomata;
        MapGenerator map = FindAnyObjectByType<MapGenerator>();
        if (map != null)
            map.SetGenerationMethod(MapGenerationMethod.CellularAutomata);
        RefreshSelectionVisuals();
    }

    public void SelectBSP()
    {
        selectedGenerationMethod = MapGenerationMethod.BSP;
        MapGenerator map = FindAnyObjectByType<MapGenerator>();
        if (map != null)
            map.SetGenerationMethod(MapGenerationMethod.BSP);
        RefreshSelectionVisuals();
    }

    public void StartRunSetup()
    {
        setupPanel.SetActive(false);

        if (abilitySelectionUI != null)
            abilitySelectionUI.ShowClassSelection();
        else
            Time.timeScale = 1f;
    }

    void PositionStartButton()
    {
        if (!centerStartButtonAtBottom || startButton == null)
            return;

        RectTransform rect =
            startButton.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = startButtonBottomOffset;
    }

    void LoadCurrentSelections()
    {
        if (DifficultyManager.Instance != null)
            selectedDifficulty =
                DifficultyManager.Instance.currentDifficulty;

        MapGenerator map = FindAnyObjectByType<MapGenerator>();
        if (map != null)
            selectedGenerationMethod = map.generationMethod;
    }

    void RefreshSelectionVisuals()
    {
        SetIndicator(
            easySelectedIndicator,
            selectedDifficulty == Difficulty.Easy);
        SetIndicator(
            hardSelectedIndicator,
            selectedDifficulty == Difficulty.Hard);
        SetIndicator(
            cellularSelectedIndicator,
            selectedGenerationMethod ==
            MapGenerationMethod.CellularAutomata);
        SetIndicator(
            bspSelectedIndicator,
            selectedGenerationMethod == MapGenerationMethod.BSP);
    }

    void SetIndicator(GameObject indicator, bool selected)
    {
        if (indicator == null)
            return;

        indicator.SetActive(selected);
    }
}
