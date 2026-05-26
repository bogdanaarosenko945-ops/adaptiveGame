using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

public class AbilitySelectionUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject classPanel;
    public GameObject abilityPanel;
    public GameObject selectedPanel;

    [Header("Class selection")]
    public Button warriorButton;
    public Button mageButton;
    public Button rogueButton;

    [Header("Ability cards")]
    public GameObject[] abilityCards;
    public Image[] cardIcons;
    public TextMeshProUGUI[] cardNames;
    public TextMeshProUGUI[] cardDescriptions;
    public Button[] cardButtons;

    [Header("Selected abilities")]
    public Image[] selectedIcons;
    public TextMeshProUGUI selectedClassText;

    private readonly List<AbilityData> currentOffered =
        new List<AbilityData>();

    void Start()
    {
        if (warriorButton != null)
            warriorButton.onClick.AddListener(
                () => GameManager.Instance
                    .OnClassSelected("Warrior"));
        if (mageButton != null)
            mageButton.onClick.AddListener(
                () => GameManager.Instance
                    .OnClassSelected("Mage"));
        if (rogueButton != null)
            rogueButton.onClick.AddListener(
                () => GameManager.Instance
                    .OnClassSelected("Rogue"));

        for (int i = 0; i < cardButtons.Length; i++)
        {
            int idx = i;
            if (cardButtons[i] != null)
                cardButtons[i].onClick.AddListener(
                    () => OnCardClicked(idx));
        }

        HideAll();
    }

    void Update()
    {
        if (abilityPanel == null ||
            !abilityPanel.activeInHierarchy)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.hKey.wasPressedThisFrame)
            OnSkipClicked();
    }

    public void ShowClassSelection()
    {
        HideAll();
        if (classPanel != null)
            classPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ShowAbilityCards(
        List<AbilityData> abilities)
    {
        HideAll();
        if (abilityPanel != null)
            abilityPanel.SetActive(true);

        currentOffered.Clear();
        currentOffered.AddRange(abilities);
        Time.timeScale = 0f;

        for (int i = 0; i < abilityCards.Length; i++)
        {
            bool hasAbility = i < abilities.Count;

            if (abilityCards[i] != null)
                abilityCards[i].SetActive(hasAbility);

            if (!hasAbility)
                continue;

            AbilityData ability = abilities[i];

            if (cardIcons.Length > i &&
                cardIcons[i] != null &&
                ability.icon != null)
                cardIcons[i].sprite = ability.icon;

            if (cardNames.Length > i &&
                cardNames[i] != null)
                cardNames[i].text = ability.abilityName;

            if (cardButtons.Length > i &&
                cardButtons[i] != null)
            {
                TextMeshProUGUI btnText =
                    cardButtons[i]
                        .GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                    btnText.text = "Вибрати";
            }

            if (cardDescriptions.Length > i &&
                cardDescriptions[i] != null)
                cardDescriptions[i].text =
                    ability.GetDisplayDescription();
        }

        CardAnimator animator =
            abilityPanel != null
                ? abilityPanel.GetComponent<CardAnimator>()
                : null;
        if (animator != null)
            animator.AnimateCards();

        UpdateSelectedPanel();
    }

    void OnCardClicked(int index)
    {
        if (index >= currentOffered.Count)
            return;

        Time.timeScale = 1f;
        GameManager.Instance
            .OnAbilitySelected(currentOffered[index]);
    }

    void OnSkipClicked()
    {
        Time.timeScale = 1f;
        GameManager.Instance.OnAbilitySkipped();
    }

    void UpdateSelectedPanel()
    {
        if (selectedPanel != null)
            selectedPanel.SetActive(true);

        if (GameManager.Instance == null)
            return;

        List<AbilityData> selected =
            GameManager.Instance.selectedAbilities;

        if (selectedClassText != null)
            selectedClassText.text =
                $"Клас: {GameManager.Instance.selectedClass}";

        for (int i = 0; i < selectedIcons.Length; i++)
        {
            if (selectedIcons[i] == null)
                continue;

            bool hasIcon =
                i < selected.Count &&
                selected[i] != null &&
                selected[i].icon != null;

            selectedIcons[i].gameObject.SetActive(hasIcon);

            if (hasIcon)
                selectedIcons[i].sprite = selected[i].icon;
        }
    }

    public void HideAll()
    {
        if (classPanel != null)
            classPanel.SetActive(false);
        if (abilityPanel != null)
            abilityPanel.SetActive(false);
        Time.timeScale = 1f;
    }
}
