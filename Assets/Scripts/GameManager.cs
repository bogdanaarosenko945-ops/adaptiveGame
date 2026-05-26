using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Налаштування")]
    public AbilityData[] allAbilities; // всі 6-9 абілок
    public int maxRuns = 3; // скільки разів вибираємо абілки

    [Header("Посилання")]
    public MapGenerator mapGenerator;
    public PlayerTracker playerTracker;
    public AbilitySelectionUI abilityUI;

    [Header("Конфіги класів")]
    public BuildConfig warriorConfig;
    public BuildConfig mageConfig;
    public BuildConfig rogueConfig;

    [Header("Player prefabs by class")]
    public GameObject warriorPlayerPrefab;
    public GameObject magePlayerPrefab;
    public GameObject roguePlayerPrefab;
    

    // Поточний стан
    public int currentRun = 0;
    public string selectedClass = "";
    public List<AbilityData> selectedAbilities
        = new List<AbilityData>();
    public List<AbilityData> usedAbilities
        = new List<AbilityData>();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        SetGameplayHudVisible(false);

        // Перший запуск — показуємо вибір класу
        if (currentRun == 0 &&
            FindAnyObjectByType<RunSetupUI>() == null)
            abilityUI.ShowClassSelection();
    }

    // Викликається коли гравець вибрав клас
    public void OnClassSelected(string className)
    {
        selectedClass = className;

        switch (className)
        {
            case "Warrior":
                mapGenerator.currentBuild = warriorConfig;
                break;
            case "Mage":
                mapGenerator.currentBuild = mageConfig;
                break;
            case "Rogue":
                mapGenerator.currentBuild = rogueConfig;
                break;
        }

        if (mapGenerator != null)
            mapGenerator.ResetAdaptiveMapParams();

        // Скидаємо абілки при новому виборі класу
        selectedAbilities.Clear();

        ShowAbilitySelection();
    }

    public GameObject GetSelectedPlayerPrefab()
    {
        switch (selectedClass)
        {
            case "Warrior":
                return warriorPlayerPrefab;
            case "Mage":
                return magePlayerPrefab;
            case "Rogue":
                return roguePlayerPrefab;
            default:
                return null;
        }
    }

    public void ConfigurePlayer(GameObject player)
    {
        if (player == null)
            return;

        PlayerMovement movement =
            player.GetComponent<PlayerMovement>();
        if (movement != null)
            movement.tracker = playerTracker;

        PlayerCombat combat =
            player.GetComponent<PlayerCombat>();
        if (combat != null)
            combat.tracker = playerTracker;

        PlayerClassIdentity identity =
            player.GetComponent<PlayerClassIdentity>();
        if (identity == null)
            identity = player.AddComponent<PlayerClassIdentity>();

        identity.className = selectedClass;

        ApplyPlayerAbilities(player);
    }


  
    // Показуємо 3 рандомні абілки на вибір
    public void ShowAbilitySelection()
    {
        // Збираємо абілки які ще не використовувались
        List<AbilityData> available =
            new List<AbilityData>();

        foreach (var ab in allAbilities)
            if (!usedAbilities.Contains(ab))
                available.Add(ab);

        // Якщо мало абілок — скидаємо список
        if (available.Count < 3)
        {
            usedAbilities.Clear();
            available = new List<AbilityData>(allAbilities);
        }

        // Вибираємо 3 рандомні
        List<AbilityData> offered =
            new List<AbilityData>();

        while (offered.Count < 3 && available.Count > 0)
        {
            int idx = Random.Range(0, available.Count);
            offered.Add(available[idx]);
            available.RemoveAt(idx);
        }

        // Показуємо UI
        abilityUI.ShowAbilityCards(offered);
    }

    // Викликається коли гравець вибрав абілку
    public void OnAbilitySelected(AbilityData ability)
    {
        selectedAbilities.Add(ability);
        usedAbilities.Add(ability);
        currentRun++;

        if (playerTracker != null)
            playerTracker.selectedAbilities =
                selectedAbilities.ToArray();

        // Застосовуємо параметри карти від абілок
        ApplyPlayerAbilities();
        ApplyAbilityMapParams();

        abilityUI.HideAll();
        SetGameplayHudVisible(true);
        mapGenerator.GenerateMap();
    }

    public void OnAbilitySkipped()
    {
        currentRun++;

        if (playerTracker != null)
            playerTracker.selectedAbilities =
                selectedAbilities.ToArray();

        ApplyPlayerAbilities();

        abilityUI.HideAll();
        SetGameplayHudVisible(true);
        mapGenerator.GenerateMap();
    }

    // Викликається після смерті або завершення рівня
    public void OnRunComplete()
    {
        if (playerTracker != null)
        {
            playerTracker.selectedAbilities =
                selectedAbilities.ToArray();
            playerTracker.RegisterRunComplete();
        }

        if (currentRun < maxRuns)
        {
            SetGameplayHudVisible(false);
            ShowAbilitySelection();
        }
        else
        {
            // Не очищаємо абілки — залишаємо для ML і луту
            Debug.Log("ML аналізує дані і адаптує карту");
            abilityUI.HideAll();
            SetGameplayHudVisible(true);
            mapGenerator.GenerateMap();
        }
    }
    void ApplyAbilityMapParams()
    {
        if (mapGenerator == null) return;
        if (mapGenerator.currentBuild == null) return;

        float meleeTotal = 0f;
        float rangedTotal = 0f;
        float magicTotal = 0f;

        foreach (var ab in selectedAbilities)
        {
            meleeTotal += ab.meleeWeight;
            rangedTotal += ab.rangedWeight;
            magicTotal += ab.magicWeight;
        }

        int fillOffset = 0;
        int smoothOffset = 0;
        int widthOffset = 0;
        int heightOffset = 0;

        // Ближній бій → трохи вузькі коридори
        if (meleeTotal > rangedTotal &&
            meleeTotal > magicTotal)
        {
            fillOffset = 5;
            smoothOffset = -1;
            widthOffset = -5;
            heightOffset = -5;
            Debug.Log("Карта: вузькі коридори (ближній бій)");
        }
        // Дальній бій → трохи відкритіше
        else if (rangedTotal > meleeTotal &&
                 rangedTotal > magicTotal)
        {
            fillOffset = -5;
            smoothOffset = 1;
            widthOffset = 5;
            heightOffset = 5;
            Debug.Log("Карта: відкриті простори (дальній бій)");
        }
        // Магія → середній розмір
        else if (magicTotal > meleeTotal)
        {
            fillOffset = -3;
            smoothOffset = 2;
            Debug.Log("Карта: відкриті зали (магія)");
        }

        mapGenerator.SetAbilityMapModifiers(
            fillOffset,
            smoothOffset,
            widthOffset,
            heightOffset);
    }

    public void ApplyPlayerAbilities()
    {
        GameObject player =
            GameObject.FindWithTag("Player");
        if (player == null) return;

        ApplyPlayerAbilities(player);
    }

    public void ApplyPlayerAbilities(GameObject player)
    {
        if (player == null) return;

        PlayerAbilityController controller =
            player.GetComponent<PlayerAbilityController>();
        if (controller == null)
            controller =
                player.AddComponent<PlayerAbilityController>();

        controller.ApplyAbilities(selectedAbilities);
    }

    void SetGameplayHudVisible(bool visible)
    {
        HPBar[] huds = FindObjectsByType<HPBar>(
            FindObjectsInactive.Include);

        foreach (HPBar hud in huds)
            hud.SetVisible(visible);
    }

}
