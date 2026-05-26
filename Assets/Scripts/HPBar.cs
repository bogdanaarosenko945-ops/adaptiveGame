using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class HPBar : MonoBehaviour
{
    [Header("UI елементи")]
    public Slider hpSlider;
    public Slider manaSlider;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI manaText;
    public TextMeshProUGUI classText;
    public GameObject hudRoot;
    public GameObject[] extraHudObjects;
    public CanvasGroup hudGroup;
    public bool showOnlyDuringGameplay = true;

    [Header("Посилання")]
    public PlayerHealth playerHealth;

    private int maxMana = 100;
  public int currentMana = 100;

    void Awake()
    {
        if (hudRoot == null)
            hudRoot = FindHudRoot();

        if (hudGroup == null)
            hudGroup = hudRoot.GetComponent<CanvasGroup>();
        if (hudGroup == null)
            hudGroup = hudRoot.AddComponent<CanvasGroup>();
    }

    void Start()
    {
        if (showOnlyDuringGameplay)
            SetVisible(false);

        UpdateClassText();
        if (playerHealth != null)
            UpdateHPColor();
    }

    void Update()
    {
        if (playerHealth == null) return;

        // Оновлюємо HP бар
        hpSlider.maxValue = playerHealth.maxHealth;
        hpSlider.value = playerHealth.currentHealth;

        if (hpText != null)
            hpText.text =
                $"{playerHealth.currentHealth}" +
                $"/{playerHealth.maxHealth}";

        // Оновлюємо мана бар
        if (manaSlider != null)
        {
            manaSlider.maxValue = maxMana;
            manaSlider.value = currentMana;
        }

        if (manaText != null)
            manaText.text =
                $"{currentMana}/{maxMana}";
    }

    public void UseMana(int amount)
    {
        currentMana = Mathf.Max(0,
            currentMana - amount);
    }

    public void RestoreMana(int amount)
    {
        currentMana = Mathf.Min(maxMana,
            currentMana + amount);
    }

    void UpdateClassText()
    {
        if (classText == null) return;
        string cls = GameManager.Instance != null
            ? GameManager.Instance.selectedClass
            : "Warrior";
        classText.text = $"Клас: {cls}";
    }

    public void SetVisible(bool visible)
    {
        if (hudGroup == null)
            return;

        if (visible)
            UpdateClassText();

        hudGroup.alpha = visible ? 1f : 0f;
        hudGroup.interactable = visible;
        hudGroup.blocksRaycasts = visible;
        hudRoot.SetActive(visible);

        foreach (GameObject obj in GetExtraHudObjects())
        {
            if (obj != null)
                obj.SetActive(visible);
        }
    }

    GameObject FindHudRoot()
    {
        GameObject panel = GameObject.Find("HPPanel");
        if (panel != null)
            return panel;

        Transform commonRoot = transform;
        AddCommonRoot(ref commonRoot, hpSlider);
        AddCommonRoot(ref commonRoot, manaSlider);
        AddCommonRoot(ref commonRoot, hpText);
        AddCommonRoot(ref commonRoot, manaText);
        AddCommonRoot(ref commonRoot, classText);

        return commonRoot != null ? commonRoot.gameObject : gameObject;
    }

    IEnumerable<GameObject> GetExtraHudObjects()
    {
        if (extraHudObjects != null)
        {
            foreach (GameObject obj in extraHudObjects)
                yield return obj;
        }

        LootManager lootManager =
            FindAnyObjectByType<LootManager>(
                FindObjectsInactive.Include);
        if (lootManager != null &&
            lootManager.coinsText != null)
            yield return lootManager.coinsText.gameObject;
    }

    void AddCommonRoot(
        ref Transform commonRoot,
        Component component)
    {
        if (component == null)
            return;

        Transform candidate = component.transform;
        while (commonRoot != null &&
               !candidate.IsChildOf(commonRoot))
            commonRoot = commonRoot.parent;
    }

    void UpdateHPColor()
    {
        if (hpSlider == null) return;

        Image fill = hpSlider
            .fillRect.GetComponent<Image>();
        if (fill == null) return;

        float ratio = (float)playerHealth.currentHealth
                      / playerHealth.maxHealth;

        if (ratio > 0.6f)
            fill.color = Color.green;
        else if (ratio > 0.3f)
            fill.color = Color.yellow;
        else
            fill.color = Color.red;
    }
}
